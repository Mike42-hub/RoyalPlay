
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace RoyalPlay.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly Switch soundToggle = new();
    private readonly Switch animationToggle = new();

    public SettingsPage()
    {
        InitializeComponent();

        soundToggle.IsToggled =
            Preferences.Default.Get(
                "RoyalPlaySound", true);

        animationToggle.IsToggled =
            Preferences.Default.Get(
                "RoyalPlayAnimations", true);

        soundToggle.Toggled += SavePreferences;
        animationToggle.Toggled += SavePreferences;

        Content = BuildScreen();
    }

    private View BuildScreen()
    {
        var layout = new VerticalStackLayout
        {
            Padding = new Thickness(24, 30),
            Spacing = 22
        };

        var back = new Button
        {
            Text = "‹ BACK",
            BackgroundColor = Color.FromArgb("#203442"),
            TextColor = Color.FromArgb("#F0CE91"),
            HorizontalOptions = LayoutOptions.Start,
            CornerRadius = 12
        };

        back.Clicked += BackButton_Clicked;
        layout.Children.Add(back);

        layout.Children.Add(new Label
        {
            Text = "SETTINGS",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F0CE91")
        });

        layout.Children.Add(new Label
        {
            Text = "PERSONALIZE YOUR EXPERIENCE",
            FontSize = 12,
            TextColor = Color.FromArgb("#A3BAC3")
        });

        var preferences = new VerticalStackLayout
        {
            Spacing = 25
        };

        preferences.Children.Add(
            CreateOption(
                "CARD ANIMATIONS",
                "Smooth dealing and table transitions",
                animationToggle));

        preferences.Children.Add(
            CreateOption(
                "SOUND PREFERENCE",
                "Saved for future audio support",
                soundToggle));

        var panel = new Border
        {
            Padding = new Thickness(20),
            BackgroundColor = Color.FromArgb("#152635"),
            Stroke = new SolidColorBrush(
                Color.FromArgb("#425568")),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(20)
            },
            Content = preferences
        };

        layout.Children.Add(panel);

        layout.Children.Add(new Label
        {
            Text = "ROYALPLAY",
            FontSize = 12,
            TextColor = Color.FromArgb("#7D969C"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        return new ScrollView
        {
            BackgroundColor = Color.FromArgb("#071018"),
            Content = layout
        };
    }

    private static View CreateOption(
        string title,
        string description,
        Switch toggle)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        var text = new VerticalStackLayout
        {
            Spacing = 5
        };

        text.Children.Add(new Label
        {
            Text = title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        });

        text.Children.Add(new Label
        {
            Text = description,
            FontSize = 11,
            TextColor = Color.FromArgb("#A3BAC3")
        });

        grid.Add(text, 0, 0);

        toggle.VerticalOptions = LayoutOptions.Center;
        grid.Add(toggle, 1, 0);

        return grid;
    }

    private void SavePreferences(
        object? sender,
        ToggledEventArgs e)
    {
        Preferences.Default.Set(
            "RoyalPlayAnimations",
            animationToggle.IsToggled);

        Preferences.Default.Set(
            "RoyalPlaySound",
            soundToggle.IsToggled);
    }

    // Compatibility with SettingsPage.xaml.
    private void Preference_Toggled(
        object sender,
        ToggledEventArgs e)
    {
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
