using CommunityToolkit.Mvvm.ComponentModel;
using SensorStream.MAUI.Models;
using SensorStream.MAUI.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace SensorStream.MAUI.ViewModels;
[QueryProperty("LobbyParams", "LobbyParams")]
public partial class LobbyViewModel : ObservableObject
{
    [ObservableProperty]
    LobbyParams lobbyParams;
    [ObservableProperty]
    string socketId = string.Empty;

    [ObservableProperty]
    AccelerometerData accelerometerData = new();
    [ObservableProperty]
    GyroscopeData gyroscopeData = new();
    [ObservableProperty]
    OrientationSensorData orientationData = new();

    IWebSocketService _webSocketService;

    public LobbyViewModel()
    {
        StartListeningForSensorData();
    }

    partial void OnLobbyParamsChanged(LobbyParams value)
    {
        if (value.Server is null || string.IsNullOrWhiteSpace(value.Username))
        {
            Shell.Current.DisplayAlert("Missing server or username", "Server or username is empty.", "OK");
            Shell.Current.GoToAsync("..");
            return;
        }
        var serverUri = $"ws://{value.Server.IP}:{value.Server.WS_Port}?username={value.Username}";
        _webSocketService = new WebSocketService(serverUri);
        _webSocketService.MessageReceived += OnMessageReceived;
        _webSocketService.ErrorOccurred += OnErrorOccurred;
        try
        {
            _webSocketService.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebSocket connection error: {ex.Message}");
            Shell.Current.DisplayAlert("WebSocket connection error", ex.Message, "OK");
        }
    }

    private void OnMessageReceived(string message)
    {
        if (message.StartsWith("SocketId:"))
        {
            SocketId = message.Replace("SocketId:", "");
        }
        if (message == "GameStart")
        {
            StartSendingSensorData();
        }
    }

    private void OnErrorOccurred(Exception ex)
    {
        Debug.WriteLine($"WebSocket error: {ex.Message}");
    }

    private void StartSendingSensorData()
    {
        Accelerometer.ReadingChanged += async (_, _) =>
        {
            var sensorData = new
            {
                Id = SocketId,
                acc = new
                {
                    x = AccelerometerData.Acceleration.X,
                    y = AccelerometerData.Acceleration.Y,
                    z = AccelerometerData.Acceleration.Z
                },
                gyro = new
                {
                    x = GyroscopeData.AngularVelocity.X,
                    y = GyroscopeData.AngularVelocity.Y,
                    z = GyroscopeData.AngularVelocity.Z
                },
                ori = new
                {
                    x = OrientationData.Orientation.X,
                    y = OrientationData.Orientation.Y,
                    z = OrientationData.Orientation.Z,
                    w = OrientationData.Orientation.W
                }
            };
            var json = JsonSerializer.Serialize(sensorData);
            await _webSocketService.SendAsync(json);
        };
    }

    private void StartListeningForSensorData()
    {
        Accelerometer.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            AccelerometerData = reading;
        };
        Gyroscope.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            GyroscopeData = reading;
        };
        OrientationSensor.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            OrientationData = reading;
        };
        if (!Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Start(SensorSpeed.Game);
        }
        if (!Gyroscope.Default.IsMonitoring)
        {
            Gyroscope.Start(SensorSpeed.Game);
        }
        if (!OrientationSensor.Default.IsMonitoring)
        {
            OrientationSensor.Start(SensorSpeed.Game);
        }
    }

    public void StopSensorData()
    {
        if (Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Stop();
        }
        if (Gyroscope.Default.IsMonitoring)
        {
            Gyroscope.Stop();
        }
        if (OrientationSensor.Default.IsMonitoring)
        {
            OrientationSensor.Stop();
        }
    }

    public async Task DisconnectWebSocketAsync()
    {
        if (_webSocketService != null)
        {
            await _webSocketService.DisconnectAsync();
        }
    }
}

public class LobbyParams
{
    public LobbyParams(BroadcastMessageModel server, string username)
    {
        Server = server;
        Username = username;
    }

    public BroadcastMessageModel Server { get; set; }
    public string Username { get; set; }
    
}