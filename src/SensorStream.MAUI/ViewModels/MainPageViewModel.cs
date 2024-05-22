using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SensorStream.MAUI.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SensorStream.MAUI.ViewModels;

public partial class MainViewModel: ObservableObject
{
    private readonly IConnectivity _connectivity;

    private const int UDP_BROADCAST_PORT = 9752; //FLAG:TODO Make it configurable
    private readonly UdpClient _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    public MainViewModel(IConnectivity connectivity)
    {
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _udpClient = new UdpClient();
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_BROADCAST_PORT));
        IsSearchRunning = false;
    }

    [ObservableProperty]
    ObservableCollection<BroadcastMessageModel> broadcastMessages = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchStopped))]
    bool isSearchRunning = false;

    public bool IsSearchStopped => !IsSearchRunning;
    [RelayCommand]
    async Task SearchHost()
    {
        if(_connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert("No Internet", "No internet connection detected. You need to be connected to a network first!", "OK");
            return;
        }
     
        IsSearchRunning = true;

        if(_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
        _cancellationTokenSource = new CancellationTokenSource();
        StartListening(_cancellationTokenSource.Token);
    }
    [RelayCommand]
    void StopSearch()
    {
        _cancellationTokenSource?.Cancel();
        IsSearchRunning = false;
    }
    private async void StartListening(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                string message = Encoding.ASCII.GetString(result.Buffer);
                var serverIP = result.RemoteEndPoint.Address.MapToIPv4().ToString();
                var uDPBroadcastMessage = ParseMessage(message, serverIP);
                if (uDPBroadcastMessage != null && !BroadcastMessages.Contains(uDPBroadcastMessage))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        BroadcastMessages.Add(uDPBroadcastMessage);
                    });
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
        {
            // Expected exception when the operation is canceled or UDPClient is disposed
        }
        catch (Exception ex) 
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            });
        }
    }
    public void Dispose()
    {
        _udpClient.Dispose();
        _cancellationTokenSource?.Dispose();
    }
    /// <summary>
    /// Returns a BroadcastMessage if the message is in the correct format else returns null
    /// </summary>
    /// <param name="message">Message must be in format [Name];;[Port]</param>
    /// <param name="ip">Server's IP Address</param>
    /// <returns></returns>
    private BroadcastMessageModel? ParseMessage(string message, string ip)
    {
        var parts = message.Split(new[] { ";;" }, StringSplitOptions.None);
        if (parts.Length == 2 && int.TryParse(parts[1], out var port))
        {
            return new BroadcastMessageModel(parts[0], ip, port);
        }
        return null;
    }
}
