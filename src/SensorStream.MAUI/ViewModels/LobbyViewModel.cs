using CommunityToolkit.Mvvm.ComponentModel;
using SensorStream.MAUI.Helpers;
using SensorStream.MAUI.Models;
using SensorStream.MAUI.Services;
using System.Diagnostics;
using System.Text.Json;

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
    private readonly IUiNotificationHelper _uiNotificationHelper;
    private readonly SensorService _sensorService;

    private EventHandler<AccelerometerChangedEventArgs>? _accelerometerHandler;
    public LobbyViewModel(IWebSocketService webSocketService, IUiNotificationHelper uiNotificationHelper)
    {
        _webSocketService = webSocketService;
        _uiNotificationHelper = uiNotificationHelper;

        _sensorService = new SensorService();
        _sensorService.StartIfNotStarted();

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
        var serverUri = new Uri($"ws://{value.Server.IP}:{value.Server.WS_Port}?username={value.Username}");

        _webSocketService.MessageReceived += OnMessageReceived;
        _webSocketService.ErrorOccurred += OnSocketErrorOccurred;

        _webSocketService.ConnectAsync(serverUri);
    }

    private void OnMessageReceived(string message)
    {
        // Just for testing, needs refactoring
        if (message.StartsWith("SocketId:"))
        {
            SocketId = message.Replace("SocketId:", "");
        }
        if (message == "GameStart")
        {
            StartSendingSensorData();
        }
    }

    private void OnSocketErrorOccurred(Exception ex)
    {
        _uiNotificationHelper.DisplayAlertAsync("Web Socket Error", ex.Message);
    }

    private void StartSendingSensorData()
    {
        // Make it timer based instead of accelerometer subscriber???
        _accelerometerHandler = async (_, _) =>
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

         _sensorService.Accelerometer.ReadingChanged += _accelerometerHandler;
    }
    private void StartListeningForSensorData()
    {
        _sensorService.StartIfNotStarted();

        _sensorService.Accelerometer.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            AccelerometerData = reading;
        };
        _sensorService.Gyroscope.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            GyroscopeData = reading;
        };
        _sensorService.OrientationSensor.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            OrientationData = reading;
        };
    }

    public async Task TerminateConnection()
    {
        if (_webSocketService != null)
        {
            await _webSocketService.DisconnectAsync();
        }
        _sensorService.StopIfStarted();
        if (_accelerometerHandler != null)
        {
            _sensorService.Accelerometer.ReadingChanged -= _accelerometerHandler;
            _accelerometerHandler = null;
        }
    }
}
