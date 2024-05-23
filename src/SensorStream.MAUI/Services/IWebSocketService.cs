

namespace SensorStream.MAUI.Services;

public interface IWebSocketService
{
    bool IsConnected { get; }

    event Action<string> MessageReceived;
    event Action<Exception> ErrorOccurred;

    Task ConnectAsync();
    Task DisconnectAsync();
    void Dispose();
    Task SendAsync(string message);
}
