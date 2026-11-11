
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class HistoryPage : ContentPage
{
    private readonly RoyalWebSession session = new("history");

    public HistoryPage()
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
}
