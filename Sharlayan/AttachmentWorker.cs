// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttachmentWorker.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   AttachmentWorker.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    using NLog;

    using Sharlayan.Models;

    internal class AttachmentWorker : IDisposable {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Timer _scanTimer;

        private bool _isScanning;

        private ProcessModel _processModel;

        public AttachmentWorker() {
            this._scanTimer = new Timer(1000);
            this._scanTimer.Elapsed += this.ScanTimerElapsed;
        }

        public void Dispose() {
            this._scanTimer.Elapsed -= this.ScanTimerElapsed;
        }

        /// <summary>
        /// </summary>
        public void StartScanning(ProcessModel processModel) {
            this._processModel = processModel;
            this._scanTimer.Enabled = true;
        }

        /// <summary>
        /// </summary>
        public void StopScanning() {
            this._scanTimer.Enabled = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="e"> </param>
        private void ScanTimerElapsed(object sender, ElapsedEventArgs e) {
            if (this._isScanning || !MemoryHandler.Instance.IsAttached) {
                return;
            }

            this._isScanning = true;

            Func<bool> scanner = delegate {
                Process[] processes = Process.GetProcesses();
                if (!processes.Any(process => process.Id == this._processModel.ProcessID && process.ProcessName == this._processModel.ProcessName)) {
                    MemoryHandler.Instance.IsAttached = false;
                    MemoryHandler.Instance.UnsetProcess();
                }

                this._isScanning = false;
                return true;
            };

            Task.Run(() => scanner.Invoke());
        }
    }
}