using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TastyEat.Workstation.ViewModels;
using TastyEat.Workstation.Views;

namespace TastyEat.Workstation;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        File.AppendAllText("/tmp/tastyeat_startup.log", $"[{DateTime.Now:O}] App.OnFrameworkInitializationCompleted{Environment.NewLine}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new LoadingWindowViewModel();
            var loadingWindow = new LoadingWindow { DataContext = viewModel };
            
            loadingWindow.Show();

            await InitializeAsync(desktop, loadingWindow, viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeAsync(IClassicDesktopStyleApplicationLifetime desktop, LoadingWindow loadingWindow, LoadingWindowViewModel viewModel)
    {
        var bootstrapper = new Bootstrapper();
        var progress = new Progress<double>(value =>
        {
            viewModel.Progress = value;
            viewModel.Status = value switch
            {
                <= 25 => "Чтение конфигурации...",
                <= 55 => "Регистрация сервисов...",
                <= 80 => "Сборка приложения...",
                <= 99 => "Подключение к базе данных...",
                _ => "Готово"
            };
        });

        var host = await bootstrapper.BuildAppAsync(progress);

        var mainWindowViewModel = host.Services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = mainWindowViewModel };
        
        mainWindow.Show();
        loadingWindow.Close();
        Console.Write("DSGDG");
        
        desktop.ShutdownRequested += (_, _) =>
        {
            host.Dispose();
        };
    }
}
