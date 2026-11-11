
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class HomePage : ContentPage
{
    private readonly RoyalWebSession session = new("home");

    public HomePage()
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

    private async void PokerButton_Clicked(
        object sender, EventArgs e)
    {
        await session.OpenSectionAsync("poker");
    }

    private async void BlackjackButton_Clicked(
        object sender, EventArgs e)
    {
        await session.OpenSectionAsync("blackjack");
    }

    private async void SportsButton_Clicked(
        object sender, EventArgs e)
    {
        await session.OpenSectionAsync("sports");
    }
}
