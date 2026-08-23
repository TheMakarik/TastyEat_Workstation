using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class DistributionDateDialog : Window
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
    }

    private readonly State _state = new();

    public DistributionDateDialog()
    {
        Width = 360;
        Height = 240;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Новый развоз";

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { Text = "Дата развоза", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 8 }.Grid_Row(1)
                    .Children(
                        new DatePicker()
                            .SelectedDate(_state, x => x.Date, Avalonia.Data.BindingMode.TwoWay)),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Создать", IsDefault = true }
                            .OnClick(_ => Close(_state.Date)),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }
}
