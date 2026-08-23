using Avalonia.Controls;
using Material.Icons;

namespace TastyEat.Workstation.Views.Utils;

public static class DeleteConfirmationDialog
{
    public static async Task<bool> ShowAsync(Window? owner, string message) =>
        await MessageDialog.ShowAsync(
            owner,
            "Удаление",
            message,
            MaterialIconKind.DeleteOutline,
            "Удалить",
            "Отмена",
            MaterialIconKind.Delete,
            MaterialIconKind.Close) ?? false;
}
