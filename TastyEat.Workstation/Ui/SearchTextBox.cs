using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;

namespace TastyEat.Workstation.Ui;

public sealed class SearchTextBox : TextBox
{
    public SearchTextBox()
    {
        PlaceholderText = "Поиск";
        InnerLeftContent = new MaterialIcon { Kind = MaterialIconKind.Magnify };
    }
}
