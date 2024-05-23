using System.Net.WebSockets;
using System.Text;

namespace SensorStream.MAUI.Services;
public class WebSocketService : IDisposable, IWebSocketService
{
    private ClientWebSocket _webSocket;
    private readonly Uri _serverUri;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _receivingTask;

    public event Action<string> MessageReceived;
    public event Action<Exception> ErrorOccurred;

    public WebSocketService(string serverUri)
    {
        _webSocket = new ClientWebSocket();
        _serverUri = new Uri(serverUri);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task ConnectAsync()
    {
        try
        {
            await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
            _receivingTask = Task.Run(ReceiveAsync, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    public async Task SendAsync(string message)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket connection is not open.");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveAsync()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected exception when the operation is canceled.
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
        {
            try
            {
                _cancellationTokenSource.Cancel();
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }
        Dispose();
    }

    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
