
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class BlackjackPage : ContentPage
{
    private readonly RoyalWebSession session = new("blackjack");

    public BlackjackPage()
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

    // Compatibility with existing BlackjackPage.xaml.
    // The new interface runs the game through
    // RoyalWebSession.

    private async void BackButton_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void DealButton_Clicked(
        object sender, EventArgs e)
    {
    }

    private void HitButton_Clicked(
        object sender, EventArgs e)
    {
    }

    private void StandButton_Clicked(
        object sender, EventArgs e)
    {
    }

    private void DoubleButton_Clicked(
        object sender, EventArgs e)
    {
    }
}
