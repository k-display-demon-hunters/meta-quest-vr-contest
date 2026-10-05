using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class WebSocketConnection : IDisposable
    {
        // 실제 서버 주소는 이 계층에서만 설정한다.
        private string _serverUrl = "";

        private const int ReceiveBufferSize = 8192;
        private const int MaxMessageSize = 1024 * 1024;
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(3);

        private enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Disconnecting
        }

        private readonly object _sync = new object();
        private readonly ConcurrentQueue<Action> _pendingEvents = new ConcurrentQueue<Action>();
        private ConnectionSession _session;
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _isDisposed;

        public event Action<string> JsonReceived;
        public event Action<Exception> ConnectionError;
        public event Action Disconnected;

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _state == ConnectionState.Connected;
                }
            }
        }

        public void DispatchPendingEvents()
        {
            while (_pendingEvents.TryDequeue(out Action action))
            {
                action();
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            Uri uri = new Uri(_serverUrl);

            ConnectionSession session = new ConnectionSession();
            lock (_sync)
            {
                if (_isDisposed)
                {
                    session.Dispose();
                    throw new ObjectDisposedException(nameof(WebSocketConnection));
                }

                if (_session != null)
                {
                    session.Dispose();
                    throw new InvalidOperationException("WebSocket is already connecting or connected.");
                }

                _session = session;
                _state = ConnectionState.Connecting;
            }

            using (CancellationTokenSource connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken, session.Cancellation.Token))
            {
                connectTimeout.CancelAfter(TimeSpan.FromSeconds(10));

                try
                {
                    await session.Socket.ConnectAsync(uri, connectTimeout.Token).ConfigureAwait(false);

                    lock (_sync)
                    {
                        if (_session != session || session.Cancellation.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }

                        _state = ConnectionState.Connected;
                    }

                    session.ReceiveTask = ReceiveMessagesAsync(session);
                }
                catch (Exception exception)
                {
                    if (!(exception is OperationCanceledException && session.Cancellation.IsCancellationRequested))
                    {
                        QueueError(exception);
                    }

                    FinishSession(session);
                    throw;
                }
            }
        }

        public async Task DisconnectAsync()
        {
            ConnectionSession session;
            lock (_sync)
            {
                session = _session;
                if (session == null)
                {
                    return;
                }

                if (_state == ConnectionState.Disconnecting)
                {
                    return;
                }

                _state = ConnectionState.Disconnecting;
            }

            try
            {
                if (session.Socket.State == WebSocketState.Open)
                {
                    using (CancellationTokenSource timeout = new CancellationTokenSource(CloseTimeout))
                    {
                        await session.SendGate.WaitAsync(timeout.Token).ConfigureAwait(false);
                        try
                        {
                            await session.Socket.CloseOutputAsync(
                                WebSocketCloseStatus.NormalClosure, string.Empty, timeout.Token)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            session.SendGate.Release();
                        }
                    }

                    await Task.WhenAny(session.ReceiveTask, Task.Delay(CloseTimeout)).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is WebSocketException
                                              || exception is OperationCanceledException
                                              || exception is ObjectDisposedException)
            {
                // 종료 중 소켓이 먼저 닫히거나 응답 시간이 초과되면 아래에서 정리한다.
            }
            finally
            {
                FinishSession(session);
            }
        }

        public Task SendJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return SendAsync(Encoding.UTF8.GetBytes(json), cancellationToken);
        }

        private async Task SendAsync(byte[] message, CancellationToken cancellationToken)
        {
            ConnectionSession session;
            lock (_sync)
            {
                session = _session;
                if (_state != ConnectionState.Connected || session == null)
                {
                    throw new InvalidOperationException("WebSocket is not connected.");
                }
            }

            // ClientWebSocket은 같은 소켓에서 동시에 여러 SendAsync 호출을 지원하지 않는다.
            using (CancellationTokenSource sendCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken, session.Cancellation.Token))
            {
                await session.SendGate.WaitAsync(sendCancellation.Token).ConfigureAwait(false);
                try
                {
                    if (session.Socket.State != WebSocketState.Open)
                    {
                        throw new InvalidOperationException("WebSocket is not connected.");
                    }

                    await session.Socket.SendAsync(
                        new ArraySegment<byte>(message), WebSocketMessageType.Text, true, sendCancellation.Token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    session.SendGate.Release();
                }
            }
        }

        private async Task ReceiveMessagesAsync(ConnectionSession session)
        {
            byte[] buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!session.Cancellation.IsCancellationRequested)
                {
                    using (MemoryStream message = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await session.Socket.ReceiveAsync(
                                new ArraySegment<byte>(buffer), session.Cancellation.Token).ConfigureAwait(false);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                if (session.Socket.State == WebSocketState.CloseReceived)
                                {
                                    using (CancellationTokenSource timeout = new CancellationTokenSource(CloseTimeout))
                                    {
                                        await session.Socket.CloseOutputAsync(
                                            WebSocketCloseStatus.NormalClosure, string.Empty, timeout.Token)
                                            .ConfigureAwait(false);
                                    }
                                }

                                return;
                            }

                            if (result.MessageType != WebSocketMessageType.Text)
                            {
                                throw new InvalidDataException("Expected a text WebSocket message containing JSON.");
                            }

                            if (message.Length + result.Count > MaxMessageSize)
                            {
                                throw new InvalidDataException("WebSocket message exceeds the 1 MiB limit.");
                            }

                            message.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        string json = Encoding.UTF8.GetString(message.ToArray());
                        _pendingEvents.Enqueue(() =>
                        {
                            // 연결이 끝난 뒤 남아 있던 응답으로 삭제된 모델을 복원하지 않는다.
                            bool isCurrent;
                            lock (_sync)
                            {
                                isCurrent = _session == session && _state == ConnectionState.Connected;
                            }
                            if (isCurrent)
                            {
                                JsonReceived?.Invoke(json);
                            }
                        });
                    }
                }
            }
            catch (Exception exception) when (exception is OperationCanceledException
                                              || exception is ObjectDisposedException)
            {
                if (!session.Cancellation.IsCancellationRequested)
                {
                    QueueError(exception);
                }
            }
            catch (Exception exception)
            {
                QueueError(exception);
            }
            finally
            {
                bool isDisconnecting;
                lock (_sync)
                {
                    isDisconnecting = _session == session
                                      && _state == ConnectionState.Disconnecting;
                }

                if (!isDisconnecting)
                {
                    FinishSession(session);
                }
            }
        }

        private void QueueError(Exception exception)
        {
            _pendingEvents.Enqueue(() => ConnectionError?.Invoke(exception));
        }

        private void FinishSession(ConnectionSession session)
        {
            lock (_sync)
            {
                if (_session == session)
                {
                    _session = null;
                    _state = ConnectionState.Disconnected;
                    _pendingEvents.Enqueue(() => Disconnected?.Invoke());
                }
            }

            session.Dispose();
        }

        public void Dispose()
        {
            ConnectionSession session;
            lock (_sync)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                session = _session;
                _session = null;
                _state = ConnectionState.Disconnected;
            }

            session?.Dispose();

            while (_pendingEvents.TryDequeue(out _))
            {
            }
        }

        private sealed class ConnectionSession : IDisposable
        {
            private int _disposed;

            public readonly ClientWebSocket Socket = new ClientWebSocket();
            public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            public readonly SemaphoreSlim SendGate = new SemaphoreSlim(1, 1);
            public Task ReceiveTask = Task.CompletedTask;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                Cancellation.Cancel();
                Socket.Abort();
                Socket.Dispose();
                // 진행 중인 송신이 Release할 수 있으므로 SendGate는 여기서 Dispose하지 않는다.
            }
        }
    }
}
