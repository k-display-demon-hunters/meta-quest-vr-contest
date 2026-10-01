using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking
{
    public sealed class WebSocketClient : MonoBehaviour
    {
        [SerializeField] private string _serverUrl = "";

        private WebSocketConnection _connection;

        public ModelProtocolClient ModelProtocol { get; private set; }
        public bool IsConnected => _connection != null && _connection.IsConnected;

        public event Action<string> JsonReceived;
        public event Action<Exception> ConnectionError;
        public event Action Disconnected;

        private void OnEnable()
        {
            _connection = new WebSocketConnection();
            ModelProtocol = new ModelProtocolClient(SendModelJsonAsync);
            _connection.JsonReceived += HandleJsonReceived;
            _connection.ConnectionError += HandleConnectionError;
            _connection.Disconnected += HandleDisconnected;
        }

        private void Update()
        {
            _connection?.DispatchPendingEvents();
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            WebSocketConnection connection = GetConnection();
            // 이전 연결의 종료 이벤트를 새 연결의 학습 요청보다 먼저 처리한다.
            connection.DispatchPendingEvents();
            return connection.ConnectAsync(_serverUrl, cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            ModelProtocol?.ResetSession();
            await GetConnection().DisconnectAsync();
        }

        public Task TrainAsync(string modelJson, int? epoch = null, CancellationToken cancellationToken = default)
        {
            GetConnection();
            return ModelProtocol.TrainAsync(modelJson, epoch, cancellationToken);
        }

        public Task CancelTrainingAsync(CancellationToken cancellationToken = default)
        {
            GetConnection();
            return ModelProtocol.CancelTrainingAsync(cancellationToken);
        }

        public Task InferAsync(string imageId, CancellationToken cancellationToken = default)
        {
            GetConnection();
            return ModelProtocol.InferAsync(imageId, cancellationToken);
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

        private async Task SendModelJsonAsync(string json, CancellationToken cancellationToken)
        {
            WebSocketConnection connection = GetConnection();
            try
            {
                await connection.SendJsonAsync(json, cancellationToken);
            }
            catch
            {
                // 송신 실패 시 서버의 접수 여부가 불명확하므로 연결을 닫아 작업도 폐기한다.
                await connection.DisconnectAsync();
                throw;
            }
        }

        private void HandleJsonReceived(string json)
        {
            ModelProtocol.HandleJson(json);
            JsonReceived?.Invoke(json);
        }

        private void HandleDisconnected()
        {
            ModelProtocol.ResetSession();
            Disconnected?.Invoke();
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
            _connection.Disconnected -= HandleDisconnected;
            ModelProtocol.ResetSession();
            _connection.Dispose();
            _connection = null;
        }
    }
}
