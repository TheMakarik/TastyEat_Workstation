using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material.Icons;
using Material.Icons.Avalonia;

namespace TastyEat.Workstation.Views.Utils;

public static class MessageDialog
{
    public static Task ShowInfoAsync(Window? owner, string message) =>
        ShowAsync(owner, "Информация", message, MaterialIconKind.InformationOutline, "Понятно", null);

    public static async Task<bool> ConfirmAsync(Window? owner, string message) =>
        await ShowAsync(owner, "Подтверждение", message, MaterialIconKind.HelpCircleOutline, "Да", "Отмена") ?? false;

    public static async Task<bool> ConfirmCancelAsync(Window? owner, string message) =>
        await ShowAsync(owner, "Подтверждение", message, MaterialIconKind.HelpCircleOutline, "Да", "Отмена") ?? false;

    public static Task<bool?> ChoiceAsync(Window? owner, string message, string title, string confirmButtonText, string cancelButtonText) =>
        ShowAsync(owner, title, message, MaterialIconKind.HelpCircleOutline, confirmButtonText, cancelButtonText);

    internal static async Task<bool?> ShowAsync(
        Window? owner,
        string title,
        string message,
        MaterialIconKind iconKind,
        string confirmButtonText,
        string? cancelButtonText,
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
            Content = confirmButtonText,
            MinWidth = 120,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        confirmButton.Classes.Add("accent");
        confirmButton.Click += (_, _) => { choice = true; window.Close(); };

        var buttons = new UniformGrid { Rows = 1, Columns = 1, HorizontalAlignment = HorizontalAlignment.Right };

        if (cancelButtonText is not null)
        {
            var cancelButton = new Button { Content = cancelButtonText, MinWidth = 120 };
            cancelButton.Click += (_, _) => { choice = false; window.Close(); };
            buttons.Children.Add(cancelButton);
            buttons.Columns = 2;
        }

        buttons.Children.Add(confirmButton);

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

    private static IBrush GetAccentBrush() =>
        Application.Current?.TryFindResource("AccentColor", ThemeVariant.Light, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.SlateBlue;
}
