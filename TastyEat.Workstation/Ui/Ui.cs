using System.ComponentModel;
using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace TastyEat.Workstation.Ui;

public static class UiFactory
{
    public static StackPanel Header(MaterialIconKind icon, string title, string subtitle) =>
        new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children =
            {
                new Border
                {
                    Background = AppStyles.AccentPurple,
                    CornerRadius = new CornerRadius(12),
                    Width = 56,
                    Height = 56,
                    Padding = new Thickness(14),
                    Child = new MaterialIcon { Kind = icon, Width = 28, Height = 28, Foreground = AppStyles.Accent }
                },
                new StackPanel
                {
                    Spacing = 2,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Brush.Parse("#1A1A1A") },
                        new TextBlock { Text = subtitle, FontSize = 14, Foreground = Brush.Parse("#666666") }
                    }
                }
            }
        };

    public static Button ActionButton(MaterialIconKind icon, string text, Action onClick, string styleClass = "accent") =>
        new Button()
        {
            Classes = { styleClass },
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new MaterialIcon { Kind = icon, Width = 18, Height = 18 },
                    new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
                }
            }
        }.OnClick(_ => onClick());

    public static TextBlock ErrorText<TState>(TState state, Expression<Func<TState, string?>> errorSelector)
        where TState : INotifyPropertyChanged =>
        new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#C0392B")),
            TextWrapping = TextWrapping.Wrap
        }.Text(state, errorSelector);

    public static Border LoadingOverlay<TState>(TState state, Expression<Func<TState, bool>> isVisible)
        where TState : INotifyPropertyChanged =>
        new Border
        {
            Background = new SolidColorBrush(Color.Parse("#88FFFFFF")),
            ZIndex = 10,
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new ProgressBar { Width = 220, IsIndeterminate = true },
                    new TextBlock { Text = "Загрузка...", HorizontalAlignment = HorizontalAlignment.Center }
                }
            }
        }.IsVisible(state, isVisible);

    public static StackPanel DialogField(string label, Control editor, Control? error = null)
    {
        var field = new StackPanel { Spacing = 4 };
        field.Children.Add(new TextBlock { Text = label });
        field.Children.Add(editor);
        if (error is not null)
            field.Children.Add(error);
        return field;
    }
}
