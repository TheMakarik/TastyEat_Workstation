using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Services;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components;

public sealed partial class AdministrationScreen(
    IBackupService backupService,
    IApplicationDataService applicationDataService,
    IServiceScopeFactory scopeFactory,
    ILogger<AdministrationScreen> logger) : ScreenComponent<AdministrationScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsBusy { get; set; }
    }

    public override string Title => "Администрирование";
    public override MaterialIconKind Icon => MaterialIconKind.CogOutline;

    protected override object Build(State state)
    {
        var logsPathText = new TextBlock
        {
            Text = $"Путь к логам: {applicationDataService.LogsDirectory}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#666666")
        };
        logsPathText.Margin = new Thickness(0, 15, 0, 0);

        var backupsPathText = new TextBlock
        {
            Text = $"Путь к резервным копиям: {applicationDataService.BackupsDirectory}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#666666")
        };
        backupsPathText.Margin = new Thickness(0, 15, 0, 0);

        var content = new StackPanel { Margin = new Thickness(0, 15, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };

        var openLogsButton = UiFactory.ActionButton(MaterialIconKind.FolderOpenOutline, "Открыть папку логов", () => _ = OpenLogsFolderAsync(), "sidebarAction");
        var createBackupButton = UiFactory.ActionButton(MaterialIconKind.ContentSaveAll, "Создать резервную копию", () => _ = CreateBackupAsync(), "sidebarAction");
        var restoreBackupButton = UiFactory.ActionButton(MaterialIconKind.Restore, "Восстановить из копии", () => _ = RestoreBackupAsync(), "sidebarAction");
        foreach (var button in new Control[] { openLogsButton, createBackupButton, restoreBackupButton })
            button.Margin = new Thickness(0, 15, 0, 15);

        content.Children.Add(logsPathText);
        content.Children.Add(openLogsButton);
        content.Children.Add(backupsPathText);
        content.Children.Add(createBackupButton);
        content.Children.Add(restoreBackupButton);
#if DEBUG
        var seedButton = UiFactory.ActionButton(MaterialIconKind.DatabasePlus, "Заполнить базу данных", () => _ = SeedDebugDataAsync(), "sidebarAction");
        seedButton.Margin = new Thickness(0, 15, 0, 15);
        content.Children.Add(seedButton);
#endif

        return new Grid().Rows("Auto, *").Classes("managementLayout")
            .Children(
                UiFactory.Header(MaterialIconKind.CogOutline, "Администрирование", "Резервные копии и обслуживание"),
                new ScrollViewer().Grid_Row(1).Content(content));
    }

    private async Task<string?> SelectBackupFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;

        var options = new FolderPickerOpenOptions
        {
            Title = "Выберите папку для сохранения бекапа",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(applicationDataService.BackupsDirectory)
        };

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private async Task<string?> SelectBackupFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Выберите файл бекапа",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(applicationDataService.BackupsDirectory),
            FileTypeFilter =
            [
                new FilePickerFileType("Бекап SQLite") { Patterns = ["*.db"] },
                FilePickerFileTypes.All
            ]
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        ScreenState.IsBusy = true;
        try
        {
            var folder = await SelectBackupFolderAsync();
            if (string.IsNullOrWhiteSpace(folder))
                return;

            var path = await backupService.CreateBackupAsync(folder);
            logger.LogInformation("Резервная копия создана: {BackupPath}", path);
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Бекап сохранён:\n{path}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось создать резервную копию");
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Ошибка при создании бекапа:\n{ex.Message}");
        }
        finally
        {
            ScreenState.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        ScreenState.IsBusy = true;
        try
        {
            var file = await SelectBackupFileAsync();
            if (string.IsNullOrWhiteSpace(file))
                return;

            var confirmed = await MessageDialog.ConfirmAsync(this.GetOwnerWindow(), "Текущая база данных будет заменена выбранным бекапом. Продолжить?");
            if (!confirmed)
                return;

            await backupService.RestoreBackupAsync(file);
            logger.LogInformation("База данных восстановлена из {BackupPath}", file);
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), "База данных восстановлена. Перезапустите приложение.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось восстановить резервную копию");
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Ошибка при восстановлении бекапа:\n{ex.Message}");
        }
        finally
        {
            ScreenState.IsBusy = false;
        }
    }

#if DEBUG
    [RelayCommand]
    private async Task SeedDebugDataAsync()
    {
        var confirmed = await MessageDialog.ConfirmAsync(
            this.GetOwnerWindow(),
            "Заполнить базу тестовыми данными (500 клиентов, ~100 товаров, 30 партий, 20 развозов, 9 сборов заказов)?\nСуществующие данные останутся.");
        if (!confirmed)
            return;

        ScreenState.IsBusy = true;
        try
        {
            await DebugDataSeeder.SeedAsync(scopeFactory, logger);
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), "База данных заполнена тестовыми значениями.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось заполнить базу тестовыми данными");
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Ошибка при заполнении базы:\n{ex.Message}");
        }
        finally
        {
            ScreenState.IsBusy = false;
        }
    }
#endif

    [RelayCommand]
    private Task OpenLogsFolderAsync()
    {
        backupService.OpenLogsFolder();
        return Task.CompletedTask;
    }
}
