using Avalonia.Controls;
using Avalonia.VisualTree;

namespace TastyEat.Workstation.Views.Utils;

public static class DialogExtensions
{
    public static Window? GetOwnerWindow(this Control control) =>
        control as Window ?? control.GetVisualAncestors().OfType<Window>().FirstOrDefault();

    public static async Task<TDialogResult?> ShowDialogAsync<TDialog, TDialogResult>(
        this Control owner,
        Func<TDialog> dialogFactory)
        where TDialog : Window
    {
        var ownerWindow = owner.GetOwnerWindow()
            ?? throw new InvalidOperationException("Не найдено родительское окно для диалога");

        return await dialogFactory().ShowDialog<TDialogResult?>(ownerWindow);
    }
}
