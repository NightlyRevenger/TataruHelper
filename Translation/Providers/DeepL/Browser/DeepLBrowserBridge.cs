using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Translation.Providers.DeepL.Browser
{
    public enum DeepLBrowserState
    {
        Off,
        Starting,
        Connected,
        NoBrowser,
        Failed,
    }

    /// <summary>
    /// Translates through the DeepL web page opened in the player's own
    /// browser, which gets a profile of its own so nothing of theirs is touched.
    /// DeepL lets a real browser in where it turns a bare HTTP client away.
    /// </summary>
    public sealed class DeepLBrowserBridge : IDisposable
    {
        private const string TranslatorUrl = "https://www.deepl.com/en/translator";

        // The page's own markers; kept together because they are what breaks when DeepL changes the page.
        private const string SourceInput = "[data-testid=translator-source-input]";
        private const string TargetInput = "[data-testid=translator-target-input]";
        private const string SourceLangButton = "[data-testid=translator-source-lang-btn]";
        private const string TargetLangButton = "[data-testid=translator-target-lang-btn]";

        // DeepL names some languages with a region where TataruHelper uses the bare code.
        private static readonly Dictionary<string, string[]> LanguageCodes =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new[] { "en", "en-US", "en-GB" },
                ["pt"] = new[] { "pt-BR", "pt-PT" },
                ["zh"] = new[] { "zh", "zh-Hans", "zh-Hant" },
            };

        private static readonly HttpClient Local = new HttpClient(new SocketsHttpHandler { UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        private readonly SemaphoreSlim _turn = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _starting = new SemaphoreSlim(1, 1);
        private CdpClient _page;
        private int _port;
        private Process _process;
        private string _pair;

        public static DeepLBrowserBridge Shared { get; } = new DeepLBrowserBridge();

        public DeepLBrowserState State { get; private set; } = DeepLBrowserState.Off;

        public string LastError { get; private set; }

        public event EventHandler Changed;

        /// <summary>Uses a browser that is already listening, instead of starting one (for tests).</summary>
        internal void UsePort(int port) => _port = port;

        public bool IsConnected => State == DeepLBrowserState.Connected && _page != null && _page.IsOpen;

        private void SetState(DeepLBrowserState state, string error = null)
        {
            State = state;
            LastError = error;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                return true;
            }

            await _starting.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsConnected)
                {
                    return true;
                }

                SetState(DeepLBrowserState.Starting);
                _pair = null;

                // A browser left from an earlier run of the program is reused.
                var page = await TryAttachAsync(cancellationToken).ConfigureAwait(false);
                if (page == null)
                {
                    var browser = DeepLBrowserLocator.Find();
                    if (browser == null)
                    {
                        SetState(DeepLBrowserState.NoBrowser);
                        return false;
                    }

                    Launch(browser);
                    page = await WaitForPageAsync(cancellationToken).ConfigureAwait(false);
                }

                if (page == null)
                {
                    SetState(DeepLBrowserState.Failed, "The browser did not open the DeepL page.");
                    return false;
                }

                _page?.Dispose();
                _page = page;
                SetState(DeepLBrowserState.Connected);
                return true;
            }
            catch (OperationCanceledException)
            {
                SetState(DeepLBrowserState.Off);
                throw;
            }
            catch (Exception ex)
            {
                SetState(DeepLBrowserState.Failed, ex.Message);
                return false;
            }
            finally
            {
                _starting.Release();
            }
        }

        public Task DisconnectAsync()
        {
            _page?.Dispose();
            _page = null;
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (Exception)
            {
            }

            // Under Wine the process is only the launcher; the browser is closed through its own port.
            _ = CloseBrowserAsync();
            _process = null;
            SetState(DeepLBrowserState.Off);
            return Task.CompletedTask;
        }

        private async Task CloseBrowserAsync()
        {
            if (_port == 0)
            {
                return;
            }

            try
            {
                var version = JObject.Parse(await Local.GetStringAsync($"http://127.0.0.1:{_port}/json/version")
                    .ConfigureAwait(false));
                using (var browser = await CdpClient.ConnectAsync((string)version["webSocketDebuggerUrl"],
                    CancellationToken.None).ConfigureAwait(false))
                {
                    await browser.SendAsync("Browser.close", null, TimeSpan.FromSeconds(3), CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <exception cref="InvalidOperationException">The page did not give a translation; the message says why.</exception>
        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage,
            CancellationToken cancellationToken)
        {
            if (!await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(State == DeepLBrowserState.NoBrowser
                    ? "No Chrome, Edge or Brave was found for DeepL."
                    : "The browser for DeepL is not available. " + LastError);
            }

            await _turn.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var page = _page;
                await SelectLanguagesAsync(page, sourceLanguage, targetLanguage, cancellationToken)
                    .ConfigureAwait(false);

                // Clear the previous text so the page does not keep showing the old translation.
                await page.EvaluateAsync(
                    $"(()=>{{const h=document.querySelector('{SourceInput}');" +
                    "const s=h.querySelector('[contenteditable]')||h;s.focus();" +
                    "document.execCommand('selectAll');document.execCommand('delete');return true;})()",
                    cancellationToken).ConfigureAwait(false);
                // The old translation stays on the page until DeepL reacts to the empty field;
                // reading before that would hand back the previous line.
                for (var i = 0; i < 15; i++)
                {
                    var left = (string)await page.EvaluateAsync(
                        $"(document.querySelector('{TargetInput}')?.innerText||'').trim()", cancellationToken)
                        .ConfigureAwait(false);
                    if (string.IsNullOrEmpty(left))
                    {
                        break;
                    }

                    await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                }

                await page.SendAsync("Input.insertText", new JObject { ["text"] = text },
                    TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                return await WaitForResultAsync(page, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException || ex is System.Net.WebSockets.WebSocketException)
            {
                _page?.Dispose();
                _page = null;
                SetState(DeepLBrowserState.Failed, "The DeepL browser was closed.");
                throw new InvalidOperationException("The DeepL browser was closed.", ex);
            }
            finally
            {
                _turn.Release();
            }
        }

        private async Task<string> WaitForResultAsync(CdpClient page, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddSeconds(12);
            string last = null;
            var stable = 0;

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                var current = (string)await page.EvaluateAsync(
                    $"(document.querySelector('{TargetInput}')?.innerText||'').trim()", cancellationToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(current))
                {
                    stable = 0;
                    last = null;
                    continue;
                }

                // The text grows while DeepL writes it, so it is taken once it stopped changing.
                stable = current == last ? stable + 1 : 1;
                last = current;
                if (stable >= 2)
                {
                    return current;
                }
            }

            throw new InvalidOperationException("DeepL did not answer in the browser.");
        }

        private async Task SelectLanguagesAsync(CdpClient page, string source, string target,
            CancellationToken cancellationToken)
        {
            var pair = source + ">" + target;
            if (pair == _pair)
            {
                return;
            }

            if (!string.IsNullOrEmpty(source) && !source.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                await PickAsync(page, SourceLangButton, source, cancellationToken).ConfigureAwait(false);
            }

            await PickAsync(page, TargetLangButton, target, cancellationToken).ConfigureAwait(false);
            _pair = pair;
        }

        private static async Task PickAsync(CdpClient page, string button, string language,
            CancellationToken cancellationToken)
        {
            var candidates = LanguageCodes.TryGetValue(language, out var codes) ? codes : new[] { language };

            await page.EvaluateAsync($"document.querySelector('{button}').click()", cancellationToken)
                .ConfigureAwait(false);
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);

            foreach (var code in candidates)
            {
                // The exact code first; otherwise the first regional form (de -> de-DE).
                var clicked = await page.EvaluateAsync(
                    $"(()=>{{const e=document.querySelector('[data-testid=\"translator-lang-option-{code}\"]')" +
                    $"||document.querySelector('[data-testid^=\"translator-lang-option-{code}-\"]:not([data-testid$=\"-pin\"])');" +
                    "if(!e)return false;e.click();return true;})()", cancellationToken).ConfigureAwait(false);
                if (clicked?.Value<bool>() == true)
                {
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            // Close the list left open so the next click starts clean.
            await page.EvaluateAsync("document.body.click()", cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"DeepL has no language \"{language}\" in the browser.");
        }

        private async Task<CdpClient> TryAttachAsync(CancellationToken cancellationToken)
        {
            if (_port == 0)
            {
                return null;
            }

            return await FindPageAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<CdpClient> WaitForPageAsync(CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                var page = await FindPageAsync(cancellationToken).ConfigureAwait(false);
                if (page != null)
                {
                    // The page may first show Cloudflare's check or a cookie banner. The cookies
                    // are accepted here; the check is the player's one click, in the window that is
                    // open, and the profile remembers it afterwards.
                    var waitingForPlayer = false;
                    for (var i = 0; i < 480; i++)
                    {
                        var state = await page.EvaluateAsync(
                            "(()=>{const b=document.querySelector('#onetrust-accept-btn-handler');if(b)b.click();" +
                            $"if(document.querySelector('{SourceInput}'))return 'ready';" +
                            "return /moment|just a|verify|проверк/i.test(document.title)||" +
                            "document.querySelector('iframe[src*=challenges],.cf-turnstile')?'check':'loading';})()",
                            cancellationToken).ConfigureAwait(false);

                        if ((string)state == "ready")
                        {
                            if (waitingForPlayer)
                            {
                                SetState(DeepLBrowserState.Starting);
                            }

                            return page;
                        }

                        if ((string)state == "check" && !waitingForPlayer)
                        {
                            waitingForPlayer = true;
                            SetState(DeepLBrowserState.Starting,
                                "Tick the check box in the DeepL window once; it will not ask again.");
                        }

                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    page.Dispose();
                }
            }

            return null;
        }

        private async Task<CdpClient> FindPageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var list = JArray.Parse(await Local.GetStringAsync($"http://127.0.0.1:{_port}/json/list",
                    cancellationToken).ConfigureAwait(false));
                foreach (var tab in list)
                {
                    if ((string)tab["type"] == "page" && ((string)tab["url"] ?? string.Empty).Contains("deepl.com"))
                    {
                        return await CdpClient.ConnectAsync((string)tab["webSocketDebuggerUrl"], cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
            }

            return null;
        }

        private void Launch(FoundBrowser browser)
        {
            _port = FreePort();

            var wine = browser.IsHostProgram;
            var profile = wine
                ? "/tmp/tataruhelper-deepl-browser"
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TataruHelper", "deepl-browser");

            var flags = new List<string>
            {
                $"--remote-debugging-port={_port}",
                "--remote-allow-origins=*",
                $"--user-data-dir={profile}",
                "--no-first-run",
                "--no-default-browser-check",
                "--disable-background-timer-throttling",
                "--disable-renderer-backgrounding",
                "--disable-backgrounding-occluded-windows",
                "--app=" + TranslatorUrl,
            };

            ProcessStartInfo info;
            if (wine)
            {
                // Wine hands the program to the host system through `start /unix`.
                info = new ProcessStartInfo("cmd.exe") { CreateNoWindow = true, UseShellExecute = false };
                info.ArgumentList.Add("/c");
                info.ArgumentList.Add("start");
                info.ArgumentList.Add("/unix");
                info.ArgumentList.Add(browser.Path);
                foreach (var flag in flags)
                {
                    info.ArgumentList.Add(flag);
                }
            }
            else
            {
                info = new ProcessStartInfo(browser.Path) { UseShellExecute = false };
                foreach (var flag in flags)
                {
                    info.ArgumentList.Add(flag);
                }
            }

            _process = Process.Start(info);
        }

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}
