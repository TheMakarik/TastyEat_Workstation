using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation;

public sealed class Bootstrapper
{
    public async Task<IHost> BuildAppAsync(IProgress<double> progress)
    {
        progress.Report(5);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        progress.Report(10);

        builder.Services.Configure<StringLengthOptions>(builder.Configuration.GetSection(nameof(StringLengthOptions)));
        builder.Services.Configure<ClientValidationOptions>(builder.Configuration.GetSection(nameof(ClientValidationOptions)));
        progress.Report(25);
      
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
        builder.Services.AddDbContext<DataContext>((sp, options) => options.UseSqlite(connectionString), ServiceLifetime.Transient, ServiceLifetime.Transient);
        progress.Report(40);

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<Bootstrapper>()
            .AddClasses()
            .AsMatchingInterface()
            .WithTransientLifetime());
        progress.Report(55);

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<Bootstrapper>()
            .AddClasses(c => c.Where(t => t.Name.EndsWith("ViewModel")))
            .AsSelf()
            .WithTransientLifetime());
        progress.Report(70);

        progress.Report(80);

        var app = builder.Build();
        await app.StartAsync();
        progress.Report(90);

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await context.Database.MigrateAsync();

        progress.Report(100);
        return app;
    }
}
