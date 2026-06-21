using System;
using Avalonia;
using Avalonia.ReactiveUI;

namespace TastyEat.Workstation;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        File.AppendAllText("/tmp/tastyeat_startup.log", $"[{DateTime.Now:O}] Program.Main started{Environment.NewLine}");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        File.AppendAllText("/tmp/tastyeat_startup.log", $"[{DateTime.Now:O}] Program.Main finished{Environment.NewLine}");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
