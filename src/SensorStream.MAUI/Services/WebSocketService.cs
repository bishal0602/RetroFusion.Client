using System.Net.WebSockets;
using System.Text;

namespace SensorStream.MAUI.Services;
public class WebSocketService : IDisposable, IWebSocketService
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receivingTask;
    private const int ConnectionTimeout = 5000; // Timeout for connection attempt in milliseconds

    public event Action<string>? MessageReceived;
    public event Action<Exception>? ErrorOccurred;
    public event Action? ServerConnected;
    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    public WebSocketService()
    {
        _webSocket = new ClientWebSocket();
    }

    public async Task ConnectAsync(Uri serverUri)
    {
        try
        {
            // Ensure WebSocket is in a valid state
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                throw new InvalidOperationException("WebSocket is already connected or connecting.");
            }

            // Needed if WebSocket is reconnected after a disconnection
            if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
            {
                _webSocket.Dispose();
                _webSocket = new ClientWebSocket();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var connectTask = _webSocket.ConnectAsync(serverUri, _cancellationTokenSource.Token);

            // Apply timeout for the connection attempt
            if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeout)) == connectTask)
            {
                await connectTask; // Await to catch any exceptions
                _receivingTask = Task.Run(ReceiveAsync, _cancellationTokenSource.Token);
                ServerConnected?.Invoke();
            }
            else
            {
                throw new TimeoutException("Timed out. Couldn't establish web socket connection.");
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            // Clean up WebSocket state
            await CleanupWebSocket();
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
    private async Task CleanupWebSocket()
    {
        try
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cleaning up", CancellationToken.None);
            }
        }
        catch
        {
            // Ignore exceptions during cleanup
        }
        finally
        {
            _webSocket.Dispose();
            _webSocket = new ClientWebSocket();
        }
    }

    public void Dispose()
    {
        _webSocket.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
