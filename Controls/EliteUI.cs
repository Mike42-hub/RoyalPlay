
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using RoyalPlay.Services;

namespace RoyalPlay.Controls;

// One shared visual style for RoyalPlay.
public static class EliteUI
{
    public const string Ink = "#071018";
    public const string Navy = "#0D1B28";
    public const string PanelColor = "#152635";
    public const string Gold = "#F0CE91";
    public const string Muted = "#A3BAC3";

    public static Color C(string hex) =>
        Color.FromArgb(hex);

    public static Label Label(
        string text,
        double size = 14,
        string color = "#FFFFFF",
        bool bold = false)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            TextColor = C(color),
            FontAttributes = bold
                ? FontAttributes.Bold
                : FontAttributes.None,
            VerticalTextAlignment = TextAlignment.Center
        };
    }

    public static Border Panel(
        View child,
        string background = PanelColor,
        int radius = 18,
        Thickness? pad = null)
    {
        return new Border
        {
            Content = child,
            Padding = pad ?? new Thickness(16),
            BackgroundColor = C(background),
            Stroke = new SolidColorBrush(C("#294454")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(radius)
            },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Opacity = 0.34f,
                Radius = 14,
                Offset = new Point(0, 7)
            }
        };
    }

    public static Button Button(
        string title,
        EventHandler handler,
        bool accent = true)
    {
        var button = new Button
        {
            Text = title,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = C(accent ? "#10202D" : "#F6E8CF"),
            BackgroundColor = C(accent ? Gold : "#294253"),
            CornerRadius = 14,
            HeightRequest = 51,
            Padding = new Thickness(12, 0)
        };

        button.Clicked += handler;
        return button;
    }

    public static Border Badge(
        string text,
        string fill = "#234737")
    {
        return new Border
        {
            BackgroundColor = C(fill),
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(C("#658770")),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            },
            Padding = new Thickness(9, 4),
            Content = Label(text, 10, Gold, true)
        };
    }

    public static View Background(View content)
    {
        var grid = new Grid
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(C("#061017"), 0),
                    new GradientStop(C("#142B38"), 0.54f),
                    new GradientStop(C("#071219"), 1)
                }
            }
        };

        grid.Children.Add(new GraphicsView
        {
            Drawable = new AmbientGlowDrawable(),
            InputTransparent = true
        });

        grid.Children.Add(content);
        return grid;
    }

    public static View PlayingCard(
        PlayingCard? card,
        bool facedown = false,
        double width = 57)
    {
        bool visible = card != null && !facedown;
        bool emptySlot = card == null && !facedown;

        var frame = new Border
        {
            WidthRequest = width,
            HeightRequest = width * 1.43,
            Margin = new Thickness(2, 1),
            StrokeThickness = 1.5,
            Stroke = new SolidColorBrush(
                C(visible ? "#E3D8C5" : "#D5B16A")),
            BackgroundColor = C(
                visible ? "#FFF9EF" : "#183F60"),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(7)
            },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Radius = 5,
                Opacity = 0.48f,
                Offset = new Point(1, 3)
            }
        };

        if (!visible)
        {
            var back = new Grid
            {
                Margin = new Thickness(4)
            };

            back.Children.Add(new Border
            {
                Stroke = new SolidColorBrush(C("#D7B678")),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(4)
                },
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(C("#326B8E"), 0),
                        new GradientStop(C("#0D2B49"), 1)
                    }
                }
            });

            back.Children.Add(new Label
            {
                Text = emptySlot ? "·" : "♠",
                TextColor = C(Gold),
                FontSize = width * 0.48,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });

            frame.Content = back;
            return frame;
        }

        string suit = card!.Suit;
        var color = C(
            suit == "♥" || suit == "♦"
                ? "#C82B3B"
                : "#172637");

        double smallFont = Math.Max(10, width * 0.22);

        var content = new Grid
        {
            Padding = new Thickness(4, 2),
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        var top = new Label
        {
            Text = card.Rank + suit,
            FontSize = smallFont,
            TextColor = color,
            FontAttributes = FontAttributes.Bold
        };

        var center = new Label
        {
            Text = suit,
            FontSize = width * 0.57,
            TextColor = color,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var bottom = new Label
        {
            Text = card.Rank + suit,
            FontSize = smallFont,
            TextColor = color,
            FontAttributes = FontAttributes.Bold,
            Rotation = 180,
            HorizontalTextAlignment = TextAlignment.End
        };

        content.Add(top, 0, 0);
        content.Add(center, 0, 1);
        content.Add(bottom, 0, 2);

        frame.Content = content;
        return frame;
    }

    public static View Chip(
        string value,
        string color = "#DA3545",
        double size = 39)
    {
        var circle = new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = C(color),
            Stroke = new SolidColorBrush(C("#FFF2D3")),
            StrokeThickness = 3,
            StrokeShape = new Ellipse(),
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(1, 3),
                Radius = 5,
                Opacity = 0.5f
            }
        };

        circle.Content = new Border
        {
            Margin = new Thickness(5),
            Stroke = new SolidColorBrush(C("#F8E1B1")),
            StrokeThickness = 1,
            StrokeShape = new Ellipse(),
            Content = new Label
            {
                Text = value,
                FontSize = size * 0.23,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        return circle;
    }

    public static View Seat(
        string name,
        string move,
        string color,
        bool active,
        IEnumerable<PlayingCard>? cards,
        bool showCards,
        double cardWidth = 29)
    {
        var column = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center
        };

        var avatar = new Border
        {
            WidthRequest = 39,
            HeightRequest = 39,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(C(color), 0),
                    new GradientStop(C("#142438"), 1)
                }
            },
            Stroke = new SolidColorBrush(
                C(active ? Gold : "#5A6672")),
            StrokeThickness = 2,
            StrokeShape = new Ellipse(),
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = name[..1],
                TextColor = Colors.White,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };

        column.Children.Add(avatar);

        column.Children.Add(new Label
        {
            Text = name,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = C(active ? "#FFFFFF" : "#849BA1"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        var row = new HorizontalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.Center
        };

        if (cards != null)
        {
            foreach (var card in cards)
            {
                row.Children.Add(
                    PlayingCard(card, !showCards, cardWidth));
            }
        }

        column.Children.Add(row);

        column.Children.Add(new Label
        {
            Text = active ? move : "FOLD",
            FontSize = 9,
            TextColor = C(active ? Gold : "#D98E8E"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        return new Border
        {
            BackgroundColor = C("#112B32"),
            Stroke = new SolidColorBrush(C("#47716B")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12)
            },
            Padding = new Thickness(5),
            MinimumWidthRequest = 76,
            Content = column
        };
    }
}

// Decorative background.
public sealed class AmbientGlowDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = Color.FromRgba(
            22, 124, 111, 17);

        canvas.FillEllipse(
            rect.Width * -0.3f,
            rect.Height * -0.17f,
            rect.Width * 1.05f,
            rect.Width * 1.05f);

        canvas.FillColor = Color.FromRgba(
            211, 167, 89, 12);

        canvas.FillEllipse(
            rect.Width * 0.55f,
            rect.Height * 0.37f,
            rect.Width * 0.85f,
            rect.Width * 0.85f);
    }
}

// Custom table with wood, rail, and emerald felt.
public sealed class EliteFeltDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF r)
    {
        float x = 3;
        float y = 4;
        float w = Math.Max(10, r.Width - 6);
        float h = Math.Max(10, r.Height - 8);

        canvas.FillColor = Color.FromArgb("#02090C");
        canvas.FillRoundedRectangle(x, y, w, h, 85);

        canvas.FillColor = Color.FromArgb("#523222");
        canvas.FillRoundedRectangle(
            x + 3, y + 3, w - 6, h - 6, 81);

        canvas.StrokeColor = Color.FromArgb("#D9B575");
        canvas.StrokeSize = 3;
        canvas.DrawRoundedRectangle(
            x + 7, y + 7, w - 14, h - 14, 79);

        canvas.FillColor = Color.FromArgb("#10272C");
        canvas.FillRoundedRectangle(
            x + 13, y + 13, w - 26, h - 26, 72);

        canvas.FillColor = Color.FromArgb("#096149");
        canvas.FillRoundedRectangle(
            x + 22, y + 22, w - 44, h - 44, 65);

        canvas.StrokeColor = Color.FromArgb("#23866B");
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(
            x + 34, y + 35, w - 68, h - 70, 60);

        canvas.StrokeColor =
            Color.FromRgba(215, 200, 148, 30);

        canvas.StrokeSize = 2;

        canvas.DrawEllipse(
            r.Width * 0.14f,
            r.Height * 0.30f,
            r.Width * 0.72f,
            r.Height * 0.40f);

        canvas.DrawEllipse(
            r.Width * 0.26f,
            r.Height * 0.37f,
            r.Width * 0.48f,
            r.Height * 0.27f);
    }
}
