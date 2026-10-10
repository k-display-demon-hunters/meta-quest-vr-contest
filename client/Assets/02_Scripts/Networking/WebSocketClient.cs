using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking
{
    public sealed class WebSocketClient : MonoBehaviour
    {
        // 서버 주소가 확정되면 이 값을 설정한다.
        private const string ServerUrl = "";

        private WebSocketConnection _connection;

        public event Action<string> JsonReceived;
        public event Action<Exception> ConnectionError;

        private void OnEnable()
        {
            _connection = new WebSocketConnection();
            _connection.JsonReceived += HandleJsonReceived;
            _connection.ConnectionError += HandleConnectionError;
        }

        private void Update()
        {
            _connection?.DispatchPendingEvents();
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return GetConnection().ConnectAsync(ServerUrl, cancellationToken);
        }

        public Task DisconnectAsync()
        {
            return GetConnection().DisconnectAsync();
        }

        public Task SendJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            return GetConnection().SendJsonAsync(json, cancellationToken);
        }

        private WebSocketConnection GetConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("WebSocketClient is not enabled.");
            }

            return _connection;
        }

        private void HandleJsonReceived(string json)
        {
            JsonReceived?.Invoke(json);
        }

        private void HandleConnectionError(Exception exception)
        {
            ConnectionError?.Invoke(exception);
        }

        private void OnDisable()
        {
            if (_connection == null)
            {
                return;
            }

            _connection.JsonReceived -= HandleJsonReceived;
            _connection.ConnectionError -= HandleConnectionError;
            _connection.Dispose();
            _connection = null;
        }
    }
}
