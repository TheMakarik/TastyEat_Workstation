using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material.Icons;
using Material.Icons.Avalonia;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Views.Utils;

public static class MessageDialog
{
    public static Task ShowInfoAsync(Window? owner, string message) =>
        ShowAsync(owner, "Информация", message, MaterialIconKind.InformationOutline, "Понятно", null,
            MaterialIconKind.CheckCircle, null);

    public static async Task<bool> ConfirmAsync(Window? owner, string message) =>
        await ShowAsync(owner, "Подтверждение", message, MaterialIconKind.HelpCircleOutline, "Да", "Отмена",
            MaterialIconKind.Check, MaterialIconKind.Close) ?? false;

    public static async Task<bool> ConfirmCancelAsync(Window? owner, string message) =>
        await ShowAsync(owner, "Подтверждение", message, MaterialIconKind.HelpCircleOutline, "Да", "Отмена",
            MaterialIconKind.Check, MaterialIconKind.Close) ?? false;

    public static Task<bool?> ChoiceAsync(Window? owner, string message, string title, string confirmButtonText, string cancelButtonText) =>
        ShowAsync(owner, title, message, MaterialIconKind.HelpCircleOutline, confirmButtonText, cancelButtonText,
            MaterialIconKind.Check, MaterialIconKind.Close);

    internal static async Task<bool?> ShowAsync(
        Window? owner,
        string title,
        string message,
        MaterialIconKind iconKind,
        string confirmButtonText,
        string? cancelButtonText,
        MaterialIconKind? confirmButtonIcon = null,
        MaterialIconKind? cancelButtonIcon = null,
        IBrush? iconBrush = null)
    {
        bool? choice = null;

        var window = new Window
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            MinWidth = 380,
            MaxWidth = 560
        };

        var confirmButton = new Button
        {
            Content = BuildButtonContent(confirmButtonText, confirmButtonIcon),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = AppStyles.Accent,
            Foreground = Brushes.White,
            BorderBrush = AppStyles.Accent
        };
        confirmButton.Classes.Add("accent");
        confirmButton.Click += (_, _) => { choice = true; window.Close(); };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right };

        Button? cancelButton = null;
        if (cancelButtonText is not null)
        {
            cancelButton = new Button
            {
                Content = BuildButtonContent(cancelButtonText, cancelButtonIcon),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Foreground = AppStyles.Accent,
                BorderBrush = AppStyles.Accent
            };
            cancelButton.Click += (_, _) => { choice = false; window.Close(); };
            buttons.Children.Add(cancelButton);
        }

        buttons.Children.Add(confirmButton);
        MakeSameWidth(confirmButton, cancelButton);

        window.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 20,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 14,
                    Children =
                    {
                        new MaterialIcon
                        {
                            Kind = iconKind,
                            Width = 26,
                            Height = 26,
                            Foreground = iconBrush ?? GetAccentBrush(),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 15,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                buttons
            }
        };

        if (owner is null)
            window.Show();
        else
            await window.ShowDialog(owner);

        return choice;
    }

    private static object BuildButtonContent(string text, MaterialIconKind? iconKind)
    {
        if (iconKind is null)
            return text;

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new MaterialIcon { Kind = iconKind.Value, Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    private static void MakeSameWidth(params Button?[] buttons)
    {
        var present = buttons.Where(b => b is not null).Cast<Button>().ToList();
        if (present.Count < 2)
        {
            foreach (var button in present)
                button.MinWidth = 150;
            return;
        }

        var width = 0.0;
        foreach (var button in present)
        {
            button.Measure(Size.Infinity);
            width = Math.Max(width, button.DesiredSize.Width);
        }

        foreach (var button in present)
            button.Width = Math.Max(width, 150);
    }

    private static IBrush GetAccentBrush() =>
        Application.Current?.TryFindResource("AccentColor", ThemeVariant.Light, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.SlateBlue;
}
