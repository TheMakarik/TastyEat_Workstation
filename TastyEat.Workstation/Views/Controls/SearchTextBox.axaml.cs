using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

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

        var accentBrush = Application.Current?.FindResource("AccentColor") as IBrush ?? Brushes.Purple;

        var iconForeground = searchTextBox.GetObservable(TextBox.IsFocusedProperty)
            .Select(isFocused => isFocused ? accentBrush : searchTextBox.Foreground);

        SearchIcon.Bind(TextElement.ForegroundProperty, iconForeground);
    }
}
