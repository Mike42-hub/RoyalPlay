
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using RoyalPlay.Services;

namespace RoyalPlay.Controls;

public static class CardVisual
{
    public static Border Make(
        PlayingCard? card,
        bool faceDown = false,
        double width = 54)
    {
        bool showFace = card != null && !faceDown;
        bool emptySlot = card == null && !faceDown;

        var frame = new Border
        {
            WidthRequest = width,
            HeightRequest = width * 1.44,
            Margin = new Thickness(2, 0),
            Padding = new Thickness(2),
            StrokeThickness = emptySlot ? 1 : 2,

            Stroke = new SolidColorBrush(
                Color.FromArgb(
                    emptySlot ? "#4C9B83" : "#E2C389")),

            BackgroundColor = Color.FromArgb(
                showFace
                    ? "#FFF9EC"
                    : emptySlot
                        ? "#194737"
                        : "#142E4C"),

            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(9)
            }
        };

        if (!showFace)
        {
            var back = new Grid
            {
                Padding = new Thickness(3)
            };

            back.Children.Add(new Border
            {
                Stroke = new SolidColorBrush(
                    Color.FromArgb("#D6B677")),

                StrokeThickness = 1,

                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(5)
                },

                BackgroundColor = Color.FromArgb(
                    emptySlot ? "#194737" : "#20476C")
            });

            back.Children.Add(new Label
            {
                Text = emptySlot ? "+" : "♠",

                FontSize = emptySlot
                    ? width * 0.35
                    : width * 0.45,

                TextColor = Color.FromArgb(
                    emptySlot ? "#51917F" : "#E5CA93"),

                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            });

            frame.Content = back;
            return frame;
        }

        bool red =
            card!.Suit == "♥" ||
            card.Suit == "♦";

        var ink = Color.FromArgb(
            red ? "#C72E40" : "#152638");

        double smallFont = Math.Max(10, width * 0.25);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition
                {
                    Height = GridLength.Auto
                },
                new RowDefinition
                {
                    Height = GridLength.Star
                },
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            },

            Padding = new Thickness(2)
        };

        var top = new Label
        {
            Text = card.Rank + card.Suit,
            TextColor = ink,
            FontSize = smallFont,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Start
        };

        var center = new Label
        {
            Text = card.Symbol,
            TextColor = ink,
            FontSize = width * 0.60,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var bottom = new Label
        {
            Text = card.Rank + card.Suit,
            TextColor = ink,
            FontSize = smallFont,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.End,
            Rotation = 180
        };

        grid.Add(top, 0, 0);
        grid.Add(center, 0, 1);
        grid.Add(bottom, 0, 2);

        frame.Content = grid;

        return frame;
    }
}

public sealed class TableDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF area)
    {
        float w = area.Width;
        float h = area.Height;

        if (w <= 0 || h <= 0)
            return;

        // Outer background.
        canvas.FillColor = Color.FromArgb("#071217");

        canvas.FillRoundedRectangle(
            0, 0, w, h, 65);

        // Dark wood rail.
        canvas.FillColor = Color.FromArgb("#3A251A");

        canvas.FillRoundedRectangle(
            3, 3, w - 6, h - 6, 78);

        canvas.StrokeColor = Color.FromArgb("#C89D55");
        canvas.StrokeSize = 4;

        canvas.DrawRoundedRectangle(
            6, 6, w - 12, h - 12, 76);

        // Leather cushion.
        canvas.FillColor = Color.FromArgb("#102B2C");

        canvas.FillRoundedRectangle(
            13, 13, w - 26, h - 26, 70);

        // Gold decoration.
        canvas.StrokeColor = Color.FromArgb("#E7C688");
        canvas.StrokeSize = 2;

        canvas.DrawRoundedRectangle(
            17, 17, w - 34, h - 34, 70);

        // Emerald felt.
        canvas.FillColor = Color.FromArgb("#126348");

        canvas.FillRoundedRectangle(
            22, 22, w - 44, h - 44, 66);

        // Inner felt border.
        canvas.StrokeColor = Color.FromArgb("#367D62");
        canvas.StrokeSize = 2;

        canvas.DrawRoundedRectangle(
            31, 31, w - 62, h - 62, 61);

        // Subtle center medallion.
        canvas.StrokeColor = Color.FromArgb("#28735B");
        canvas.StrokeSize = 2;

        canvas.DrawEllipse(
            w * 0.18f,
            h * 0.30f,
            w * 0.64f,
            h * 0.38f);

        canvas.DrawEllipse(
            w * 0.25f,
            h * 0.34f,
            w * 0.50f,
            h * 0.30f);
    }
}
