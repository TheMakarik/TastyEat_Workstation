using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TastyEat.Workstation.Views;

public sealed partial class LoadingWindow : Window
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial double Progress { get; set; }

        [ObservableProperty]
        public partial string Status { get; set; } = "Загрузка...";
    }

    public LoadingWindow(State state)
    {
        Title = "Кушай Вкусно!";
        Width = 420;
        Height = 180;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowDecorations = WindowDecorations.BorderOnly;

        Content = new Border
        {
            Padding = new Thickness(32),
            Child = new StackPanel
            {
                Spacing = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Кушай Вкусно! - Управление",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock()
                        .Text(state, x => x.Status)
                        .FontSize(14)
                        .HorizontalAlignment(HorizontalAlignment.Center),
                    new ProgressBar { Height = 12, Maximum = 100 }
                        .Value(state, x => x.Progress)
                }
            }
        };
    }
}
