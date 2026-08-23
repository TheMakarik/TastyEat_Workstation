using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Semi.Avalonia;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views;

namespace TastyEat.Workstation;

public class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        AddResources();

        Styles.Add(new SemiTheme { Locale = new CultureInfo("ru-RU") });
        Styles.Add(new MaterialIconStyles(null));
        Styles.Add(new StyleInclude(new Uri("avares://TastyEat.Workstation")) { Source = new Uri("avares://Avalonia.Controls.TreeDataGrid/Themes/Fluent.axaml") });
        Styles.Add(AppStyles.Instance);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            _ = RunStartupAsync(desktop);

        base.OnFrameworkInitializationCompleted();
    }

    private void AddResources()
    {
        Resources.ThemeDictionaries[ThemeVariant.Default] = new ResourceDictionary
        {
            ["SystemListLowColor"] = Color.Parse("#FFE0E0E0"),
            ["SystemBaseHighColor"] = Color.Parse("#FF000000"),
            ["SystemBaseMediumLowColor"] = Color.Parse("#FF666666"),
            ["SystemAccentColor"] = Color.Parse("#512BD4"),
            ["SystemAccentColorLight1"] = Color.Parse("#6C46DC"),
            ["SystemAccentColorLight2"] = Color.Parse("#8B6CE4"),
            ["SystemAccentColorLight3"] = Color.Parse("#AB95EC"),
            ["SystemAccentColorDark1"] = Color.Parse("#4726B7"),
            ["SystemAccentColorDark2"] = Color.Parse("#3B1F9A"),
            ["SystemAccentColorDark3"] = Color.Parse("#2F187C")
        };
        Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["SystemListLowColor"] = Color.Parse("#FF2D2D2D"),
            ["SystemBaseHighColor"] = Color.Parse("#FFFFFFFF"),
            ["SystemBaseMediumLowColor"] = Color.Parse("#FFA0A0A0"),
            ["SystemAccentColor"] = Color.Parse("#7559E0"),
            ["SystemAccentColorLight1"] = Color.Parse("#8B7BE6"),
            ["SystemAccentColorLight2"] = Color.Parse("#A99CEF"),
            ["SystemAccentColorLight3"] = Color.Parse("#C4B9F4"),
            ["SystemAccentColorDark1"] = Color.Parse("#6347C4"),
            ["SystemAccentColorDark2"] = Color.Parse("#5239AD"),
            ["SystemAccentColorDark3"] = Color.Parse("#412C95")
        };

        foreach (var (key, color) in (Resources.ThemeDictionaries[ThemeVariant.Default] as ResourceDictionary)!)
            Resources.Add(key, color);

        Resources.Add("DotNetPurpleColor", Color.Parse("#512BD4"));
        Resources.Add("AccentColor", new SolidColorBrush(Color.Parse("#512BD4")));
        Resources.Add("AccentLightBrush", new SolidColorBrush(Color.Parse("#7559E0")));
        Resources.Add("AccentPurpleBrush", new SolidColorBrush(Color.Parse("#E6E1F9")));

        foreach (var (key, color) in ChartColors.Palette)
            Resources.Add(key, color);
    }

    private static async Task RunStartupAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loadingState = new LoadingWindow.State();
        var loadingWindow = new LoadingWindow(loadingState);
        loadingWindow.Show();

        try
        {
            var bootstrapper = new Bootstrapper();
            var progress = new Progress<double>(value =>
            {
                loadingState.Progress = value;
                loadingState.Status = value switch
                {
                    <= 25 => "Чтение конфигурации...",
                    <= 55 => "Регистрация сервисов...",
                    <= 80 => "Сборка приложения...",
                    <= 99 => "Подключение к базе данных...",
                    _ => "Готово"
                };
            });

            var host = await bootstrapper.BuildAppAsync(progress);

            var mainWindow = new MainWindow(host.Services);
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            loadingWindow.Close();
        }
        catch (Exception exception)
        {
            loadingState.Status = $"Ошибка запуска: {exception.Message}";
        }
    }
}
