using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services.HostedServices;

public sealed class LogArchiveHostedService(
    IApplicationDataService applicationDataService,
    IOptions<AdministrationOptions> options,
    ILogger<LogArchiveHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ArchiveOldLogs();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось заархивировать старые файлы логов");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private void ArchiveOldLogs()
    {
        var threshold = DateTime.Now.AddDays(-options.Value.LogArchiveAfterDays);
        var logFiles = Directory.GetFiles(applicationDataService.LogsDirectory, "log-*.txt")
            .Where(f => File.GetLastWriteTime(f) < threshold)
            .ToList();

        if (logFiles.Count == 0)
            return;

        var archiveName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.7z";
        var archivePath = Path.Join(applicationDataService.LogsDirectory, archiveName);

        using var stream = File.Create(archivePath);
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var file in logFiles)
            {
                archive.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }

        foreach (var file in logFiles)
        {
            File.Delete(file);
        }

        logger.LogInformation(
            "Заархивировано старых файлов логов: {Count}, архив: {ArchivePath}",
            logFiles.Count,
            archivePath);
    }
}
