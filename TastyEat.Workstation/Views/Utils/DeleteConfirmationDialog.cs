using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;

namespace TastyEat.Workstation.Views.Utils;

public static class DeleteConfirmationDialog
{
    public static async Task<bool> ShowAsync(Window? owner, string message) =>
        await MessageDialog.ShowAsync(
            owner,
            "Удаление",
            message,
            MaterialIconKind.Delete,
            "Удалить",
            "Отмена",
            new SolidColorBrush(Color.FromRgb(211, 47, 47))) ?? false;
}
