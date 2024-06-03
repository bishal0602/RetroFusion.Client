using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SensorStream.MAUI.Helpers;
using SensorStream.MAUI.Models;
using SensorStream.MAUI.Services;
using System.Diagnostics;
using System.Numerics;
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
    Vector3 accelerometerData = new();
    [ObservableProperty]
    Vector3 gyroscopeData = new();
    [ObservableProperty]
    Quaternion orientationData = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSendingSensorData))]
    bool isSendingSensorData = false;

    public bool IsNotSendingSensorData => !IsSendingSensorData;


    private readonly IWebSocketService _webSocketService;
    private readonly IUiNotificationHelper _uiNotificationHelper;
    private readonly ISensorService _sensorService;

    private EventHandler<AccelerometerChangedEventArgs>? _accelerometerHandler;
    public LobbyViewModel(IWebSocketService webSocketService, ISensorService sensorService, IUiNotificationHelper uiNotificationHelper)
    {
        _webSocketService = webSocketService;
        _uiNotificationHelper = uiNotificationHelper;
        _sensorService = sensorService;

        _sensorService.StartIfNotStarted();
        StartListeningForSensorData();
    }
    [RelayCommand]
    public void Start()
    {
        StartSendingSensorData();
        IsSendingSensorData = true;
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
        _webSocketService.ErrorOccurred += async (e)=> await OnSocketErrorOccurredAsync(e);
        _webSocketService.ServerConnected += async () => await _uiNotificationHelper.DisplayToastAsync("Connected to server");

        _webSocketService.ConnectAsync(serverUri);
    }

    private void OnMessageReceived(string message)
    {
        // Just for testing, needs refactoring
        if (message.StartsWith("SocketId:"))
        {
            SocketId = message.Replace("SocketId:", "");
        }
    }

    private async Task OnSocketErrorOccurredAsync(Exception ex)
    {
        await TerminateConnection();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("..");
            await _uiNotificationHelper.DisplayToastAsync($"Socket error:{ex.Message}");
        });
    }

    private void StartSendingSensorData()
    {
        // Make it timer based instead of accelerometer subscriber???
        _accelerometerHandler = async (_, _) =>
        {
            var sensorData = new
            {
                id = SocketId,
                acc = new
                {
                    x = AccelerometerData.X,
                    y = AccelerometerData.Y,
                    z = AccelerometerData.Z
                },
                gyro = new
                {
                    x = GyroscopeData.X,
                    y = GyroscopeData.Y,
                    z = GyroscopeData.Z
                },
                ori = new
                {
                    x = OrientationData.X,
                    y = OrientationData.Y,
                    z = OrientationData.Z,
                    w = OrientationData.W
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
            AccelerometerData = reading.Acceleration;
        };
        _sensorService.Gyroscope.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            GyroscopeData = reading.AngularVelocity;
        };
        _sensorService.OrientationSensor.ReadingChanged += (sender, args) =>
        {
            var reading = args.Reading;
            OrientationData = reading.Orientation;
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
