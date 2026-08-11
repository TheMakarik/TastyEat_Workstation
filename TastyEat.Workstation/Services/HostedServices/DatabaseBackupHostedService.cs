using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services.HostedServices;

public sealed class DatabaseBackupHostedService(
    IApplicationDataService applicationDataService,
    IOptions<AdministrationOptions> options,
    ILogger<DatabaseBackupHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldCreateBackup())
                    RunDetachedBackup();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start database backup");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private bool ShouldCreateBackup()
    {
        var interval = options.Value.DatabaseBackupIntervalDays;
        if (interval <= 0)
            return false;

        var backups = Directory.GetFiles(applicationDataService.BackupsDirectory, "tastyeat_*.db")
            .Select(f => new FileInfo(f))
            .Where(f => f.Exists)
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        if (backups.Count == 0)
            return true;

        return backups[0].LastWriteTime < DateTime.Now.AddDays(-interval);
    }

    private void RunDetachedBackup()
    {
        Directory.CreateDirectory(applicationDataService.BackupsDirectory);
        Directory.CreateDirectory(applicationDataService.ScriptsDirectory);

        var fileName = $"tastyeat_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var targetPath = Path.Join(applicationDataService.BackupsDirectory, fileName);

        if (OperatingSystem.IsWindows())
        {
            var scriptPath = Path.Join(applicationDataService.ScriptsDirectory, "backup.bat");
            File.WriteAllText(scriptPath,
                $"@echo off{Environment.NewLine}" +
                $"copy /Y \"{applicationDataService.DatabasePath}\" \"{targetPath}\"{Environment.NewLine}");

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        else
        {
            var scriptPath = Path.Join(applicationDataService.ScriptsDirectory, "backup.sh");
            File.WriteAllText(scriptPath,
                $"#!/bin/bash{Environment.NewLine}" +
                $"cp \"{applicationDataService.DatabasePath}\" \"{targetPath}\"{Environment.NewLine}");

            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"nohup bash \\\"{scriptPath}\\\" > /dev/null 2>&1 &\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        logger.LogInformation("Started detached database backup to {TargetPath}", targetPath);
    }
}
