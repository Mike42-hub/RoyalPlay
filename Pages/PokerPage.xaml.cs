
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class PokerPage : ContentPage
{
    private readonly RoyalWebSession session =
        new("poker");

    public PokerPage()
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

    private async void BackButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void NewHandButton_Clicked(
        object? sender, EventArgs e)
    {
    }

    private void FoldButton_Clicked(
        object? sender, EventArgs e)
    {
    }

    private void CallButton_Clicked(
        object? sender, EventArgs e)
    {
    }

    private void RaiseButton_Clicked(
        object? sender, EventArgs e)
    {
    }
}
