using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class AdministrationViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly IApplicationDataService _applicationDataService;
    private readonly ILogger<AdministrationViewModel> _logger;

    public AdministrationViewModel(
        IBackupService backupService,
        IApplicationDataService applicationDataService,
        ILogger<AdministrationViewModel> logger)
    {
        _backupService = backupService;
        _applicationDataService = applicationDataService;
        _logger = logger;
    }

    public override string Title => "Администрирование";
    public override string IconName => "CogOutline";

    public string BackupsDirectory => _applicationDataService.BackupsDirectory;

    public Interaction<Unit, string?> SelectBackupFolderInteraction { get; } = new();
    public Interaction<Unit, string?> SelectBackupFileInteraction { get; } = new();
    public Interaction<string, Unit> ShowInfoInteraction { get; } = new();
    public Interaction<string, bool> ConfirmInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task CreateBackupAsync()
    {
        var folder = await SelectBackupFolderInteraction.Handle(Unit.Default).FirstAsync();
        if (string.IsNullOrWhiteSpace(folder))
            return;

        try
        {
            var path = await _backupService.CreateBackupAsync(folder);
            _logger.LogInformation("Backup created at {BackupPath}", path);
            await ShowInfoInteraction.Handle($"Бекап сохранён:\n{path}").FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            await ShowInfoInteraction.Handle($"Ошибка при создании бекапа:\n{ex.Message}").FirstAsync();
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task RestoreBackupAsync()
    {
        var file = await SelectBackupFileInteraction.Handle(Unit.Default).FirstAsync();
        if (string.IsNullOrWhiteSpace(file))
            return;

        var confirmed = await ConfirmInteraction.Handle(
            "Текущая база данных будет заменена выбранным бекапом. Продолжить?").FirstAsync();
        if (!confirmed)
            return;

        try
        {
            await _backupService.RestoreBackupAsync(file);
            _logger.LogInformation("Database restored from {BackupPath}", file);
            await ShowInfoInteraction.Handle("База данных восстановлена. Перезапустите приложение.").FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup");
            await ShowInfoInteraction.Handle($"Ошибка при восстановлении бекапа:\n{ex.Message}").FirstAsync();
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task OpenLogsFolderAsync()
    {
        await Task.CompletedTask;
        _backupService.OpenLogsFolder();
    }
}
