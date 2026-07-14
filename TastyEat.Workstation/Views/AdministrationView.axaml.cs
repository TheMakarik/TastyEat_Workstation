using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

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
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var okButton = new Button { Content = "ОК", IsDefault = true };
        okButton.Classes.Add("accent");

        var dialog = new Window
        {
            Title = "Информация",
            Width = 460,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 24,
                Children =
                {
                    new TextBlock
                    {
                        Text = interaction.Input,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { okButton }
                    }
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
        interaction.SetOutput(Unit.Default);
    }

    private async Task<bool> DoConfirmAsync(IInteractionContext<string, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var result = false;
        var yesButton = new Button { Content = "Да" };
        yesButton.Classes.Add("accent");
        var noButton = new Button { Content = "Нет", IsCancel = true };

        var dialog = new Window
        {
            Title = "Подтверждение",
            Width = 460,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 24,
                Children =
                {
                    new TextBlock
                    {
                        Text = interaction.Input,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { yesButton, noButton }
                    }
                }
            }
        };

        yesButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        noButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
