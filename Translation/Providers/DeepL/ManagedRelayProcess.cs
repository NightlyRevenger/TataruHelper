using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Translation.Providers.DeepL
{
    /// <summary>Starts the bundled helper and owns only the process it started.</summary>
    public sealed class ManagedRelayProcess : IDisposable
    {
        public const string DefaultEndpoint = "http://127.0.0.1:2285";
        private readonly string _endpoint;
        private readonly HttpClient _client;
        private readonly Action<string> _log;
        private Process _owned;

        public ManagedRelayProcess(Action<string> log = null, string endpoint = DefaultEndpoint)
        {
            var uri = new Uri(endpoint);
            if (uri.Scheme != "http" || !uri.IsLoopback) throw new ArgumentException("Relay must use loopback HTTP.");
            _endpoint = endpoint.TrimEnd('/');
            _log = log;
            _client = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { Timeout = TimeSpan.FromMilliseconds(500) };
        }

        public async Task<string> TryStartAsync(string executable, CancellationToken token = default)
        {
            if (await IsReadyAsync(token).ConfigureAwait(false)) return _endpoint;
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable)) return null;
            StopOwned();
            try
            {
                var info = new ProcessStartInfo(executable)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executable))
                };
                // The configurable endpoint also makes isolated process-lifecycle
                // tests possible without taking the player's normal relay port.
                info.ArgumentList.Add("-addr");
                info.ArgumentList.Add(new Uri(_endpoint).Authority);
                _owned = Process.Start(info);
                if (_owned == null) return null;
                _owned.OutputDataReceived += (_, e) => Log(e.Data);
                _owned.ErrorDataReceived += (_, e) => Log(e.Data);
                _owned.BeginOutputReadLine();
                _owned.BeginErrorReadLine();
                var watch = Stopwatch.StartNew();
                while (watch.Elapsed < TimeSpan.FromSeconds(5) && !_owned.HasExited)
                {
                    if (await IsReadyAsync(token).ConfigureAwait(false)) return _endpoint;
                    await Task.Delay(100, token).ConfigureAwait(false);
                }
                StopOwned();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { StopOwned(); throw; }
            catch (Exception ex) { Log("Could not start helper: " + ex.GetType().Name); StopOwned(); }
            return null;
        }

        private async Task<bool> IsReadyAsync(CancellationToken token)
        {
            try
            {
                using var response = await _client.GetAsync(_endpoint + "/health", token).ConfigureAwait(false);
                return response.IsSuccessStatusCode && IsRelayHealth(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch { return false; }
        }

        internal static bool IsRelayHealth(string body)
        {
            try { return (string)JObject.Parse(body)["service"] == "tataruhelper-deepl-relay"; }
            catch { return false; }
        }

        private void Log(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            try { _log?.Invoke("[DEEPL_RELAY] " + line); } catch { }
        }
        private void StopOwned()
        {
            var process = _owned;
            _owned = null;
            if (process == null) return;
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(1000); } }
            catch (Exception ex) { Log("Could not stop helper: " + ex.GetType().Name); }
            finally { process.Dispose(); }
        }
        public void Dispose() { StopOwned(); _client.Dispose(); }
    }
}
