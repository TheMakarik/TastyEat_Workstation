using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using TastyEat.Workstation.Components;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Views;

public sealed class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        Title = "Кушай Вкусно! - Рабочая станция";
        Width = 1200;
        Height = 800;

        IScreen[] screens =
        [
            services.GetRequiredService<ClientsScreen>(),
            services.GetRequiredService<ProductsScreen>(),
            services.GetRequiredService<ProductionsScreen>(),
            services.GetRequiredService<OrderCollectionScreen>(),
            services.GetRequiredService<AdministrationScreen>()
        ];

        Content = new TabControl
        {
            ItemsSource = screens.Select(screen => new TabItem
            {
                Header = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 20,
                    Children =
                    {
                        new MaterialIcon { Kind = screen.Icon, Width = 22, Height = 22 },
                        new TextBlock { Text = screen.Title, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                Content = screen
            }).ToArray()
        };
    }
}
