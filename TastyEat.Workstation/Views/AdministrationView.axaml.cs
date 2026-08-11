using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Views;

public partial class AdministrationView : ReactiveUserControl<AdministrationViewModel>
{
    public AdministrationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.SelectBackupFolderInteraction.RegisterHandler(async interaction =>
                interaction.SetOutput(await DoSelectBackupFolderAsync())).DisposeWith(disposables);

            ViewModel?.SelectBackupFileInteraction.RegisterHandler(async interaction =>
                interaction.SetOutput(await DoSelectBackupFileAsync())).DisposeWith(disposables);

            ViewModel?.ShowInfoInteraction.RegisterHandler(async interaction =>
                await DoShowInfoAsync(interaction)).DisposeWith(disposables);

            ViewModel?.ConfirmInteraction.RegisterHandler(async interaction =>
                interaction.SetOutput(await DoConfirmAsync(interaction))).DisposeWith(disposables);
        });
    }

    private async Task<string?> DoSelectBackupFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;

        var options = new FolderPickerOpenOptions
        {
            Title = "Выберите папку для сохранения бекапа",
            AllowMultiple = false,
            SuggestedStartLocation =
                await topLevel.StorageProvider.TryGetFolderFromPathAsync(ViewModel!.BackupsDirectory)
        };

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private async Task<string?> DoSelectBackupFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Выберите файл бекапа",
            AllowMultiple = false,
            SuggestedStartLocation =
                await topLevel.StorageProvider.TryGetFolderFromPathAsync(ViewModel!.BackupsDirectory),
            FileTypeFilter =
            [
                new FilePickerFileType("Бекап SQLite") { Patterns = ["*.db"] },
                FilePickerFileTypes.All
            ]
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private async Task DoShowInfoAsync(IInteractionContext<string, Unit> interaction)
    {
        var owner = this.GetOwnerWindow();
        await MessageDialog.ShowInfoAsync(owner, interaction.Input);
        interaction.SetOutput(Unit.Default);
    }

    private async Task<bool> DoConfirmAsync(IInteractionContext<string, bool> interaction)
    {
        var owner = this.GetOwnerWindow();
        var result = await MessageDialog.ConfirmAsync(owner, interaction.Input);
        return result;
    }
}
