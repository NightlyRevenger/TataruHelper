// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Squirrel;
using System;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Updater.EventArguments;


namespace Updater
{
    public class SquirrelUpdater
    {
        #region **Events.

        public event AsyncEventHandler<UpdatingState> UpdatingStateChanged
        {
            add { this._UpdatingStateChanged.Register(value); }
            remove { this._UpdatingStateChanged.Unregister(value); }
        }
        private AsyncEvent<UpdatingState> _UpdatingStateChanged;

        #endregion

        public bool IsUpdating { get; private set; }

        string UpdatePath = @"https://github.com/NightlyRevenger/TataruHelper";

        ILog _Logger;

        bool _CheckPrerelease = false;

        UpdateManager _UpdateManager = null;

        List<Thread> _UpdaterThreads = new List<Thread>();

        private object lockObj = new object();

        private bool _RestartScheduled = false;

        public SquirrelUpdater(ILog logger)
        {
            _Logger = logger;

            _UpdatingStateChanged = new AsyncEvent<UpdatingState>(EventErrorHandler, "OnNewUpdateFound");
        }

        public void CheckAndInstallUpdates(bool checkPrerelease)
        {
            _CheckPrerelease = checkPrerelease;

            StartUpdate();
        }

        public void RestartApp()
        {
            lock (lockObj)
            {
                if (!_RestartScheduled)
                {
                    _RestartScheduled = true;
                    Task.Run(() => UpdateManager.RestartApp());
                }
            }

        }

        public void StopUpdate()
        {
            Task.Run(() =>
            {
                try
                {
                    if (_UpdateManager != null)
                        _UpdateManager?.Dispose();

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e?.ToString() ?? "StopUpdate exception");
                }
            });
        }

        public void KillAll()
        {
            try
            {
                try
                {
                    if (_UpdateManager != null)
                        _UpdateManager?.KillAllExecutablesBelongingToPackage();
                }
                catch (Exception ex)
                {
                    _Logger?.WriteLog(ex?.ToString() ?? "StopUpdate exception");
                }

                for (int i = 0; i < _UpdaterThreads.Count; i++)
                {
                    try
                    {
                        var th = _UpdaterThreads[i];
                        th.Abort();
                    }
                    catch { }
                }

                lock (_UpdaterThreads)
                {
                    _UpdaterThreads.Clear();
                }
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(e?.ToString() ?? "StopUpdate exception");
            }
        }

        private void StartUpdate()
        {
            Thread updaterThread = updaterThread = new Thread(this.EntryPoint);

            lock (_UpdaterThreads)
            {
                _UpdaterThreads.Add(updaterThread);
            }

            updaterThread.IsBackground = false;

            updaterThread.Start();
        }

        private void EntryPoint()
        {
            lock (lockObj)
            {
                try
                {
                    IsUpdating = true;
                    UpdateLogic();
                    RemoveFinishedThreads();
                    IsUpdating = false;
                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(Convert.ToString(e));
                }
            }
        }

        private void UpdateLogic()
        {
            _UpdateManager = null;

            SquirrelAwareApp.HandleEvents(
                onInitialInstall: v => OnInitialInstall(ref _UpdateManager),
                onAppUpdate: v => OnAppUpdate(ref _UpdateManager),
                onAppUninstall: v => OnAppUninstall(ref _UpdateManager));

            try
            {
                Squirrel.ReleaseEntry releaseEntry = null;
                bool newUpdateExist = false;

                Task.Run(async () =>
                {
                    try
                    {
                        _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                        {
                            Text = String.Empty,
                            UpdateSate = UpdateSate.InitializingUpdater
                        }).Forget();

                        using (_UpdateManager = await UpdateManager.GitHubUpdateManager(UpdatePath, null, null, null, _CheckPrerelease))
                        {
                            try
                            {
                                _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                {
                                    Text = String.Empty,
                                    UpdateSate = UpdateSate.LookingForUpdates
                                }).Forget(_Logger);

                                var updateInfo = await _UpdateManager.CheckForUpdate();

                                if (updateInfo.ReleasesToApply.Count > 0)
                                    newUpdateExist = true;

                                if (newUpdateExist)
                                    _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                    {
                                        Text = String.Empty,
                                        UpdateSate = UpdateSate.UpdateFound
                                    }).Forget(_Logger);
                                else
                                    _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                    {
                                        Text = String.Empty,
                                        UpdateSate = UpdateSate.NoUpdatesFound
                                    }).Forget(_Logger);

                                if (newUpdateExist)
                                    _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                    {
                                        Text = String.Empty,
                                        UpdateSate = UpdateSate.DownloadingUpdate
                                    }).Forget(_Logger);

                                releaseEntry = await _UpdateManager.UpdateApp();

                                if (newUpdateExist)
                                    _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                    {
                                        Text = String.Empty,
                                        UpdateSate = UpdateSate.UpdateDownloaded
                                    }).Forget(_Logger);
                            }
                            catch (Exception e)
                            {
                                _Logger?.WriteLog(e.ToString());
                            }

                            string updateTextInfo = String.Empty;

                            try
                            {
                                if (releaseEntry != null)
                                {
                                    updateTextInfo = releaseEntry.BaseUrl + Environment.NewLine;
                                    updateTextInfo += releaseEntry.EntryAsString + Environment.NewLine;
                                    updateTextInfo += releaseEntry.PackageName + Environment.NewLine;
                                    updateTextInfo += releaseEntry.Version + Environment.NewLine;
                                    updateTextInfo += "IsDelta: " + releaseEntry.IsDelta + Environment.NewLine;
                                }
                                else
                                {
                                    _Logger?.WriteLog("No Updates Found");
                                }

                                if (updateTextInfo.Length > 0)
                                    _Logger?.WriteLog(updateTextInfo);

                                _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                {
                                    Text = String.Empty,
                                    UpdateSate = UpdateSate.UpdatingFinished
                                }).Forget(_Logger);


                                if (newUpdateExist)
                                    _UpdatingStateChanged.InvokeAsync(new UpdatingState(this)
                                    {
                                        Text = String.Empty,
                                        UpdateSate = UpdateSate.ReadyToRestart
                                    }).Forget(_Logger);

                            }
                            catch (Exception ex3)
                            {
                                _Logger?.WriteLog(ex3.ToString());
                            }
                        }//using

                    }
                    catch (Exception ex0)
                    {
                        _Logger?.WriteLog(ex0.ToString());
                    }
                }).Wait();

                _UpdateManager = null;
            }
            catch (Exception ex)
            {
                _Logger?.WriteLog(ex.ToString());
            }
        }

        private void OnInitialInstall(ref UpdateManager mgr)
        {
            try
            {
                //mgr.CreateShortcutForThisExe();
                //mgr.CreateShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.Desktop, false);
                //mgr.CreateShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.StartMenu, false);
                mgr.CreateUninstallerRegistryEntry();
                _Logger?.WriteLog("CreateUninstallerRegistryEntry");

            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUpdate(ref UpdateManager mgr)
        {
            try
            {
                mgr.RemoveUninstallerRegistryEntry();
                _Logger?.WriteLog("RemoveUninstallerRegistryEntry");
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }

            try
            {
                mgr.CreateUninstallerRegistryEntry();
                _Logger?.WriteLog("RemoveUninstallerRegistryEntry");
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUninstall(ref UpdateManager mgr)
        {
            try
            {
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.Desktop);
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.StartMenu);
                mgr.RemoveUninstallerRegistryEntry();
                _Logger?.WriteLog("RemoveUninstallerRegistryEntry");

            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            _Logger?.WriteLog(text);
        }

        private void RemoveFinishedThreads()
        {
            lock (_UpdaterThreads)
            {
                for (int i = 0; i < _UpdaterThreads.Count; i++)
                {
                    Thread th = _UpdaterThreads[i];
                    if (th.ThreadState == ThreadState.Stopped || th.ThreadState == ThreadState.Aborted)
                    {
                        _UpdaterThreads.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

    }
}
