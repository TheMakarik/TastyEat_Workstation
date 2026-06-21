using Avalonia;
using Avalonia.Controls;

namespace TastyEat.Workstation.Views.Controls;

public partial class SearchTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<SearchTextBox, string>(
        nameof(Text),
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public SearchTextBox()
    {
        InitializeComponent();
    }
}
