using SensorStream.MAUI.ViewModels;

namespace SensorStream.MAUI;

public partial class LobbyPage : ContentPage
{
	public LobbyPage(LobbyViewModel lobbyViewModel)
	{
		InitializeComponent();
		BindingContext = lobbyViewModel;
	}
    protected override bool OnBackButtonPressed()
    {
        ShowExitConfirmation();
        return true; // Prevent default back button behavior
    }

    private async void ShowExitConfirmation()
    {
        bool confirm = await DisplayAlert("Confirm Exit", "Are you sure you want to exit? This will disconnect you from the server.", "Yes", "No");
        if (confirm)
        {
            var viewModel = BindingContext as LobbyViewModel;
            if (viewModel != null)
            {
                viewModel.StopSensorData();
                await viewModel.DisconnectWebSocketAsync();
            }
            await Shell.Current.GoToAsync("..");
        }
    }
}