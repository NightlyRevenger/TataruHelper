using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Translation.Exceptions;
using Translation.Models;

namespace Translation.Providers.Web
{
    /// <summary>A private web session with bounded latency and a refusal pause.</summary>
    internal abstract class WebSessionTranslator : ITranslationProvider
    {
        protected HttpClient Client { get; }
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Func<DateTime> _now;
        private DateTime _refusedUntil;
        public abstract TranslationEngineName EngineName { get; }

        protected WebSessionTranslator(ILogger logger, HttpClient client = null, Func<DateTime> now = null)
        {
            _logger = logger;
            _now = now ?? (() => DateTime.UtcNow);
            Client = client ?? new HttpClient(new SocketsHttpHandler
            {
                UseCookies = true, CookieContainer = new CookieContainer(), AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All, PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            });
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        }

        public async Task<string> TranslateAsync(string text, string source, string target, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var acquired = false;
            try
            {
                await _gate.WaitAsync(timeout.Token).ConfigureAwait(false);
                acquired = true;
                if (_now() < _refusedUntil)
                    throw new QuotaExceededException(EngineName, EngineName + " is temporarily paused.");
                var answer = await TranslateCoreAsync(text, NormalizeSource(source),
                    string.IsNullOrWhiteSpace(target) ? "en" : target.ToLowerInvariant(), timeout.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(answer)) throw new InvalidOperationException("No translation returned.");
                return answer;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (QuotaExceededException)
            {
                if (_refusedUntil <= _now()) _refusedUntil = _now().AddMinutes(1);
                throw;
            }
            catch (Exception ex)
            {
                if (acquired) { _refusedUntil = _now().AddMinutes(1); ResetSession(); }
                // URLs can contain text and session tokens. Log neither the URL
                // nor the exception message; status codes are logged in SendAsync.
                _logger?.LogInformation("{Message}", "[" + EngineName + "] " + ex.GetType().Name);
                return string.Empty;
            }
            finally { if (acquired) _gate.Release(); }
        }

        protected abstract Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token);
        protected virtual void ResetSession() { }

        protected async Task<string> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using (request)
            using (var response = await Client.SendAsync(request, token).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("{Message}", "[" + EngineName + "] HTTP " + (int)response.StatusCode);
                    var minimum = _now().AddMinutes(1);
                    var requested = response.Headers.RetryAfter?.Date?.UtcDateTime
                        ?? _now().Add(response.Headers.RetryAfter?.Delta ?? TimeSpan.Zero);
                    _refusedUntil = requested > minimum ? requested : minimum;
                    ResetSession();
                    throw new QuotaExceededException(EngineName, EngineName + " refused a request; trying another translator.");
                }
                return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            }
        }

        protected static string NormalizeSource(string source)
            => string.IsNullOrWhiteSpace(source) || source.Equals("auto-detect", StringComparison.OrdinalIgnoreCase)
                ? "auto" : source.ToLowerInvariant();
    }
}
