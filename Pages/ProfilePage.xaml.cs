
using RoyalPlay.Controls;
using RoyalPlay.Services;

namespace RoyalPlay.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly RoyalWebSession session = new("profile");

    public ProfilePage()
    {
        InitializeComponent();

        Shell.SetTabBarIsVisible(this, false);

        Content = session.View;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await session.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private async void SettingsButton_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(SettingsPage));
    }

    private async void LogoutButton_Clicked(
        object sender, EventArgs e)
    {
        AccountService.Logout();

        await Shell.Current.GoToAsync("//login");
    }
}
