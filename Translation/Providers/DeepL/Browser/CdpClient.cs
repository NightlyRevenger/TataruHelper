using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Translation.Providers.DeepL.Browser
{
    /// <summary>
    /// The least of the Chrome DevTools protocol that is needed to drive one
    /// page: send a command, get its answer. Events are read and dropped.
    /// </summary>
    internal sealed class CdpClient : IDisposable
    {
        private readonly ClientWebSocket _socket;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JToken>> _waiting =
            new ConcurrentDictionary<int, TaskCompletionSource<JToken>>();

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private int _nextId;

        private CdpClient(ClientWebSocket socket)
        {
            _socket = socket;
        }

        public bool IsOpen => _socket.State == WebSocketState.Open;

        public static async Task<CdpClient> ConnectAsync(string webSocketUrl, CancellationToken cancellationToken)
        {
            var socket = new ClientWebSocket();
            try
            {
                await socket.ConnectAsync(new Uri(webSocketUrl), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                socket.Dispose();
                throw;
            }

            var client = new CdpClient(socket);
            _ = client.ReadLoopAsync();
            return client;
        }

        public async Task<JToken> SendAsync(string method, JObject parameters, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var id = Interlocked.Increment(ref _nextId);
            var answer = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiting[id] = answer;

            try
            {
                var message = new JObject
                {
                    ["id"] = id,
                    ["method"] = method,
                    ["params"] = parameters ?? new JObject(),
                };

                var bytes = Encoding.UTF8.GetBytes(message.ToString(Newtonsoft.Json.Formatting.None));
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    cancellationToken).ConfigureAwait(false);

                return await answer.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _waiting.TryRemove(id, out _);
            }
        }

        /// <summary>Runs a script in the page and returns its value (null for undefined).</summary>
        public async Task<JToken> EvaluateAsync(string script, CancellationToken cancellationToken)
        {
            var result = await SendAsync("Runtime.evaluate", new JObject
            {
                ["expression"] = script,
                ["returnByValue"] = true,
            }, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);

            if (result?["exceptionDetails"] != null)
            {
                throw new InvalidOperationException(
                    "The page rejected the script: " + result["exceptionDetails"]?["text"]);
            }

            return result?["result"]?["value"];
        }

        private async Task ReadLoopAsync()
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (!_stop.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    using (var message = new MemoryStream())
                    {
                        WebSocketReceiveResult received;
                        do
                        {
                            received = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _stop.Token)
                                .ConfigureAwait(false);
                            if (received.MessageType == WebSocketMessageType.Close)
                            {
                                return;
                            }

                            message.Write(buffer, 0, received.Count);
                        } while (!received.EndOfMessage);

                        Dispatch(Encoding.UTF8.GetString(message.ToArray()));
                    }
                }
            }
            catch (Exception)
            {
                // The page or the browser went away; whoever is waiting is told below.
            }
            finally
            {
                foreach (var pair in _waiting)
                {
                    pair.Value.TrySetException(new IOException("The browser closed the connection."));
                }
            }
        }

        private void Dispatch(string text)
        {
            JObject message;
            try
            {
                message = JObject.Parse(text);
            }
            catch (Exception)
            {
                return;
            }

            var id = (int?)message["id"];
            if (id == null || !_waiting.TryGetValue(id.Value, out var answer))
            {
                return;
            }

            if (message["error"] != null)
            {
                answer.TrySetException(new InvalidOperationException((string)message["error"]["message"]));
            }
            else
            {
                answer.TrySetResult(message["result"]);
            }
        }

        public void Dispose()
        {
            try
            {
                _stop.Cancel();
                _socket.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
