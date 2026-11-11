
using RoyalPlay.Controls;

namespace RoyalPlay.Pages;

public partial class SportsPage : ContentPage
{
    private readonly RoyalWebSession session = new("sports");

    public SportsPage()
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

    // Compatibility with the old SportsPage.xaml.
    // Sports selection, API loading and predictions
    // are handled by the new HTML interface.

    private void SportPicker_SelectedIndexChanged(
        object sender, EventArgs e)
    {
    }

    private void RefreshButton_Clicked(
        object sender, EventArgs e)
    {
    }

    private void CheckResultsButton_Clicked(
        object sender, EventArgs e)
    {
    }
}
