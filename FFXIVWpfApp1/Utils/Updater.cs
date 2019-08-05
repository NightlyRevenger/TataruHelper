// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Squirrel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIITataruHelper
{
    class Updater : IDisposable
    {
        Thread _UpdaterThread;

        UpdateManager _UpdateManager;

        string UpdatePath = @"https://github.com/NightlyRevenger/TataruHelper";

        public Updater()
        {
            _UpdaterThread = new Thread(this.EntryPoint);
        }

        public void StartUpdate()
        {
            _UpdaterThread.Start();
        }

        private void UpdateLogic()
        {
            SquirrelAwareApp.HandleEvents(
                onInitialInstall: v => OnInitialInstall(ref _UpdateManager),
                onAppUpdate: v => OnAppUpdate(ref _UpdateManager),
                onAppUninstall: v => OnAppUninstall(ref _UpdateManager));

            try
            {
                Squirrel.ReleaseEntry releaseEntry = null;
                Task.Run(async () =>
                {
                    try
                    {
                        _UpdateManager = await UpdateManager.GitHubUpdateManager(UpdatePath,null,null,null,CmdArgsStatus.PreRelease);

                        //var updates = await _UpdateManager.CheckForUpdate();
                        //var lastVersion = updates?.ReleasesToApply?.OrderBy(x => x.Version).LastOrDefault();

                        releaseEntry = await _UpdateManager.UpdateApp();
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(e);

                        try
                        {
                            if (_UpdateManager != null)
                                _UpdateManager.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(ex);
                        }
                    }
                }).Wait();

                string updateInfo = String.Empty;

                try
                {
                    if (releaseEntry != null)
                    {
                        updateInfo = releaseEntry.BaseUrl + Environment.NewLine;
                        updateInfo += releaseEntry.EntryAsString + Environment.NewLine;
                        updateInfo += releaseEntry.PackageName + Environment.NewLine;
                        updateInfo += releaseEntry.Version + Environment.NewLine;
                        updateInfo += "IsDelta: " + releaseEntry.IsDelta + Environment.NewLine;
                    }
                    else
                    {
                        Logger.WriteLog("No Updates Found");
                    }

                    if (updateInfo.Length > 0)
                        Logger.WriteLog(updateInfo);

                }
                catch (Exception ex3)
                {
                    Logger.WriteLog(ex3);
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void EntryPoint()
        {
            try
            {
                UpdateLogic();
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
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
                Logger.WriteLog("CreateUninstallerRegistryEntry");

            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUpdate(ref UpdateManager mgr)
        {
            try
            {
                mgr.RemoveUninstallerRegistryEntry();
                Logger.WriteLog("RemoveUninstallerRegistryEntry");
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            try
            {
                mgr.CreateUninstallerRegistryEntry();
                Logger.WriteLog("RemoveUninstallerRegistryEntry");
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUninstall(ref UpdateManager mgr)
        {
            try
            {
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.Desktop);
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.StartMenu);
                mgr.RemoveUninstallerRegistryEntry();
                Logger.WriteLog("RemoveUninstallerRegistryEntry");
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void Dispose()
        {
            if (_UpdateManager != null)
            {
                _UpdateManager.Dispose();
            }
        }
    }
}

