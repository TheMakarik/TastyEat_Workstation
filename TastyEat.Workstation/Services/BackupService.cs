using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class BackupService(IApplicationDataService applicationDataService) : IBackupService
{
    public Task<string> CreateBackupAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);

        var fileName = $"tastyeat_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var targetPath = Path.Join(targetDirectory, fileName);

        File.Copy(applicationDataService.DatabasePath, targetPath, true);
        return Task.FromResult(targetPath);
    }

    public Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found", backupFilePath);

        File.Copy(backupFilePath, applicationDataService.DatabasePath, true);
        return Task.CompletedTask;
    }

    public Task<string> CreateScheduledBackupAsync(CancellationToken cancellationToken = default)
    {
        return CreateBackupAsync(applicationDataService.BackupsDirectory, cancellationToken);
    }

    public void OpenLogsFolder()
    {
        applicationDataService.OpenLogsFolder();
    }
}
