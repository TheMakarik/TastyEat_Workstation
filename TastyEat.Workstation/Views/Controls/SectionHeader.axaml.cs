using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace TastyEat.Workstation.Views.Controls;

public partial class SectionHeader : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconKindProperty =
        AvaloniaProperty.Register<SectionHeader, MaterialIconKind>(nameof(IconKind));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<SectionHeader, string>(nameof(Title));

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<SectionHeader, string>(nameof(Subtitle));

    public MaterialIconKind IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public SectionHeader()
    {
        InitializeComponent();
    }
}
