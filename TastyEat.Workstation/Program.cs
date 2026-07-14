using System;
using System.IO;
using System.Reactive;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using Serilog.Extensions.Logging;

namespace TastyEat.Workstation;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        File.AppendAllText("/tmp/tastyeat_startup.log", $"[{DateTime.Now:O}] Program.Main started{Environment.NewLine}");

        var logger = CreateProgramLogger();

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
            logger.LogError(ex, "Unhandled ReactiveUI exception"));

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        File.AppendAllText("/tmp/tastyeat_startup.log", $"[{DateTime.Now:O}] Program.Main finished{Environment.NewLine}");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    private static ILogger<Program> CreateProgramLogger()
    {
        var logsPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "tasty-eat",
            "logs");

        Directory.CreateDirectory(logsPath);

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Join(logsPath, "log-.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var loggerFactory = new SerilogLoggerFactory(serilog);
        return loggerFactory.CreateLogger<Program>();
    }
}
