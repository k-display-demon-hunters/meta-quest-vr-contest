using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Networking.Tests
{
    public sealed class WebSocketConnectionTests
    {
        [Test]
        public void RemoteCloseDiscardsQueuedResponsesAndNotifiesOnce()
        {
            using (LoopbackServer server = new LoopbackServer())
            using (WebSocketConnection connection = new WebSocketConnection())
            {
                int received = 0;
                int disconnected = 0;
                connection.JsonReceived += json => received++;
                connection.Disconnected += () => disconnected++;
                Task<WebSocket> accept = server.AcceptAsync();
                Wait(connection.ConnectAsync(server.Url));
                using (WebSocket peer = Wait(accept))
                {
                    Wait(peer.SendAsync(Bytes("{\"type\":\"training_completed\"}"), WebSocketMessageType.Text, true, server.Token));
                    Wait(peer.CloseAsync(WebSocketCloseStatus.NormalClosure, "", server.Token));
                    WaitUntil(() => !connection.IsConnected);
                    connection.DispatchPendingEvents();
                    connection.DispatchPendingEvents();
                    Assert.That(received, Is.Zero);
                    Assert.That(disconnected, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void FragmentedJsonIsDeliveredAsOneMessageAndClientCanSend()
        {
            using (LoopbackServer server = new LoopbackServer())
            using (WebSocketConnection connection = new WebSocketConnection())
            {
                string received = null;
                connection.JsonReceived += json => received = json;
                Task<WebSocket> accept = server.AcceptAsync();
                Wait(connection.ConnectAsync(server.Url));
                using (WebSocket peer = Wait(accept))
                {
                    Wait(peer.SendAsync(Bytes("{\"type\":"), WebSocketMessageType.Text, false, server.Token));
                    connection.DispatchPendingEvents();
                    Assert.That(received, Is.Null);
                    Wait(peer.SendAsync(Bytes("\"training_cancelled\"}"), WebSocketMessageType.Text, true, server.Token));
                    WaitUntil(() =>
                    {
                        connection.DispatchPendingEvents();
                        return received != null;
                    });
                    Assert.That(received, Is.EqualTo("{\"type\":\"training_cancelled\"}"));
                    Wait(connection.SendJsonAsync("{\"type\":\"cancel_training\"}"));
                    byte[] buffer = new byte[256];
                    WebSocketReceiveResult result = Wait(peer.ReceiveAsync(new ArraySegment<byte>(buffer), server.Token));
                    Assert.That(Encoding.UTF8.GetString(buffer, 0, result.Count), Is.EqualTo("{\"type\":\"cancel_training\"}"));
                }
            }
        }

        private static ArraySegment<byte> Bytes(string value)
        {
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(value));
        }

        private static void Wait(Task task)
        {
            if (!task.Wait(TimeSpan.FromSeconds(10)))
            {
                Assert.Fail("Loopback WebSocket operation timed out.");
            }
            task.GetAwaiter().GetResult();
        }

        private static T Wait<T>(Task<T> task)
        {
            Wait((Task)task);
            return task.GetAwaiter().GetResult();
        }

        private static void WaitUntil(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                {
                    Assert.Fail("Loopback WebSocket state timed out.");
                }
                Thread.Sleep(5);
            }
        }

        // 서버 학습 구현 없이 실제 WebSocket 전송과 종료 동작만 검증한다.
        private sealed class LoopbackServer : IDisposable
        {
            private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly CancellationTokenSource _timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            private TcpClient _peer;

            public string Url { get; }
            public CancellationToken Token => _timeout.Token;

            public LoopbackServer()
            {
                _listener.Start();
                Url = $"ws://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
            }

            public async Task<WebSocket> AcceptAsync()
            {
                _peer = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                NetworkStream stream = _peer.GetStream();
                StringBuilder request = new StringBuilder();
                byte[] buffer = new byte[1];
                while (!request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    int count = await stream.ReadAsync(buffer, 0, 1, Token).ConfigureAwait(false);
                    if (count == 0 || request.Length >= 8192)
                    {
                        throw new InvalidDataException("Invalid loopback upgrade request.");
                    }
                    request.Append((char)buffer[0]);
                }
                string key = null;
                using (StringReader reader = new StringReader(request.ToString()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                        {
                            key = line.Substring("Sec-WebSocket-Key:".Length).Trim();
                        }
                    }
                }
                if (key == null)
                {
                    throw new InvalidDataException("Missing WebSocket key.");
                }
                string accept;
                using (SHA1 hash = SHA1.Create())
                {
                    accept = Convert.ToBase64String(hash.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                }
                byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n");
                await stream.WriteAsync(response, 0, response.Length, Token).ConfigureAwait(false);
                return WebSocket.CreateFromStream(stream, true, null, TimeSpan.Zero);
            }

            public void Dispose()
            {
                _timeout.Cancel();
                _peer?.Dispose();
                _listener.Stop();
                _timeout.Dispose();
            }
        }
    }
}
