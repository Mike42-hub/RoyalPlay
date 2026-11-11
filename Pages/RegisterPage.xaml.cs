
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly RoyalWebSession session = new("register");

    public RegisterPage()
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

    // The HTML interface handles registration.

    private void CreateAccount_Clicked(
        object sender, EventArgs e)
    {
    }

    private async void BackToLogin_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
