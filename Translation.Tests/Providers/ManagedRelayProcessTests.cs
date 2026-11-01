using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Translation.Providers.DeepL;

namespace Translation.Tests.Providers
{
    public class ManagedRelayProcessTests
    {
        [TestCase("{\"service\":\"tataruhelper-deepl-relay\"}", true)]
        [TestCase("{\"service\":\"something else\"}", false)]
        [TestCase("<html>other server</html>", false)]
        public void HealthChecksIdentifyOurHelper(string body, bool expected)
            => Assert.That(ManagedRelayProcess.IsRelayHealth(body), Is.EqualTo(expected));

        [Test]
        public async Task BorrowingAnExistingServiceDoesNotStopIt()
        {
            using var server = new HealthServer("{\"service\":\"tataruhelper-deepl-relay\"}");
            using (var relay = new ManagedRelayProcess(endpoint: server.Endpoint))
                Assert.That(await relay.TryStartAsync("missing-helper.exe"), Is.EqualTo(server.Endpoint));
            using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false });
            Assert.That(await client.GetStringAsync(server.Endpoint + "/health"), Does.Contain("tataruhelper-deepl-relay"));
        }

        [Test]
        public async Task AnUnrelatedServerIsNotTreatedAsTheRelay()
        {
            using var server = new HealthServer("{}");
            using var relay = new ManagedRelayProcess(endpoint: server.Endpoint);
            Assert.That(await relay.TryStartAsync("missing-helper.exe"), Is.Null);
        }

        private sealed class HealthServer : IDisposable
        {
            private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly CancellationTokenSource _stop = new CancellationTokenSource();
            private readonly Task _run;
            public string Endpoint { get; }
            public HealthServer(string body)
            {
                _listener.Start();
                Endpoint = "http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port;
                _run = Task.Run(async () =>
                {
                    while (!_stop.IsCancellationRequested)
                    {
                        using var connection = await _listener.AcceptTcpClientAsync(_stop.Token);
                        var stream = connection.GetStream();
                        using var reader = new System.IO.StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(_stop.Token))) { }
                        var bytes = Encoding.UTF8.GetBytes(body);
                        var headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(headers, _stop.Token);
                        await stream.WriteAsync(bytes, _stop.Token);
                    }
                });
            }
            public void Dispose()
            {
                _stop.Cancel(); _listener.Stop();
                try { _run.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
                catch (SocketException) when (_stop.IsCancellationRequested) { }
                _stop.Dispose();
            }
        }
    }

    public class ManagedRelayProcessLiveTests
    {
        [Test, Explicit("Starts the real bundled relay on a private test port")]
        public async Task TheOwnerStopsTheHelperButABorrowerDoesNot()
        {
            var binary = Environment.GetEnvironmentVariable("DEEPL_RELAY_PROCESS_PROBE");
            Assert.That(binary, Is.Not.Null.And.Not.Empty);
            var reservation = new TcpListener(IPAddress.Loopback, 0);
            reservation.Start();
            var endpoint = "http://127.0.0.1:" + ((IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false });
            using (var owner = new ManagedRelayProcess(endpoint: endpoint))
            {
                Assert.That(await owner.TryStartAsync(binary), Is.EqualTo(endpoint));
                using (var borrower = new ManagedRelayProcess(endpoint: endpoint))
                    Assert.That(await borrower.TryStartAsync(binary), Is.EqualTo(endpoint));
                Assert.That(await client.GetStringAsync(endpoint + "/health"), Does.Contain("tataruhelper-deepl-relay"));
            }
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetStringAsync(endpoint + "/health"));
        }
    }
}
