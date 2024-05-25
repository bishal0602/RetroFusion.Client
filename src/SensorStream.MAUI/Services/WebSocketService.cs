using System.Net.WebSockets;
using System.Text;

namespace SensorStream.MAUI.Services;
public class WebSocketService : IDisposable, IWebSocketService
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task _receivingTask;

    public event Action<string>? MessageReceived;
    public event Action<Exception>? ErrorOccurred;

    public WebSocketService()
    {
        _webSocket = new ClientWebSocket();
    }

    public async Task ConnectAsync(Uri serverUri)
    {
        try
        {
            // Needed if web socket is reconnected after a disconnection
            if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
            {
                _webSocket = new ClientWebSocket(); 
            }
            _cancellationTokenSource = new CancellationTokenSource();
            await _webSocket.ConnectAsync(serverUri, _cancellationTokenSource.Token);
            _receivingTask = Task.Run(ReceiveAsync, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    public async Task SendAsync(string message)
    {
        try { 
            if (_webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket connection is not open.");

            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    private async Task ReceiveAsync()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource!.Token);
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
                _cancellationTokenSource?.Cancel();
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.InvalidState)
            {
                // Ignore InvalidState error since it's expected in certain scenarios
            }
            catch (WebSocketException ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }
    }

    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    public void Dispose()
    {
        _webSocket.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
