
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class LoginPage : ContentPage
{
    private readonly RoyalWebSession session = new("login");

    public LoginPage()
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

    // The HTML screen handles sign-in.
    // These methods preserve compatibility
    // with the existing XAML events.

    private void LoginButton_Clicked(
        object sender, EventArgs e)
    {
    }

    private async void RegisterButton_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(RegisterPage));
    }

    private void PreviewButton_Clicked(
        object sender, EventArgs e)
    {
    }
}
