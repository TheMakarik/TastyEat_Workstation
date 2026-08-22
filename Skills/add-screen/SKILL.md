---
name: add-screen
description: Добавить новый экран-вкладку в TastyEat.Workstation (Avalonia, ReactiveUI) — ViewModel с Title/IconName, UserControl-вью с TreeDataGrid, регистрация вкладки в MainWindowViewModel и DataTemplate в MainWindow. Используй, когда пользователь просит добавить экран, вкладку, страницу, раздел интерфейса, новое представление данных.
---

# Добавление экрана-вкладки

Каждый экран — вкладка `TabControl` в `MainWindow`. Цепочка: ViewModel → View → регистрация в `MainWindowViewModel` → `DataTemplate` в `MainWindow.axaml`.

## 1. ViewModel — `ViewModels/<Name>ViewModel.cs`

`sealed partial class`, наследник `ViewModelBase` (даёт требования `Title` и `IconName` — имя вкладки и имя иконки из перечисления `MaterialIconKind`, строкой). Свойства — `[Reactive]` на приватальных полях, команды — `[RelayCommand]`:

```csharp
using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class WarehousesViewModel(IWarehouseService warehouseService, ILogger<WarehousesViewModel> logger) : ViewModelBase
{
    public override string Title => "Склады";
    public override string IconName => "Warehouse";

    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private bool _isLoading = true;

    public FlatTreeDataGridSource<WarehouseRowViewModel> WarehousesSource { get; } = new([])
    {
        Columns =
        {
            new TextColumn<WarehouseRowViewModel, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
        }
    };
}
```

Конвенции:
- иконка — точное имя значения `MaterialIconKind` (проверь по `Material.Icons`); вкладка превращает строку в Kind через `MakripExtensions.ToIconKindConverter`;
- async-команды: `[RelayCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]`, у них внутри `try/catch (OperationCanceledException) {} / catch (Exception ex) { лог } / finally { IsLoading = false }`;
- отмена повторных загрузок — `CancellationTokenSource` + `Interlocked.Exchange` (образец `ClientsViewModel.RefreshLoadCts`);
- троттлинг поиска: `WhenAnyValue(vm => vm.SearchText).Throttle(TimeSpan.FromMilliseconds(400)).DistinctUntilChanged().Select(_ => Unit.Default).InvokeCommand(SearchCommand)`;
- VM регистрируется Scrutor автоматически (суффикс `ViewModel`, transient) — вручную не регистрируй.

## 2. View — `Views/<Name>View.axaml` + `.axaml.cs`

`UserControl` с `x:DataType` (compiled bindings включены глобально) и code-behind `ReactiveUserControl<XxxViewModel>`. Единый каркас экрана (скопируй из `ProductsView.axaml`):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:TastyEat.Workstation.ViewModels"
             xmlns:controls="clr-namespace:TastyEat.Workstation.Views.Controls"
             x:Class="TastyEat.Workstation.Views.WarehousesView"
             x:DataType="vm:WarehousesViewModel">
  <Grid Classes="managementLayout" RowDefinitions="Auto,Auto,*">
    <controls:SectionHeader Grid.Row="0" IconKind="Warehouse" Title="Склады" Subtitle="Управление складами" />
    <StackPanel Grid.Row="1" Classes="topbar" Orientation="Horizontal">
      <controls:SearchTextBox Width="300" Text="{Binding SearchText}" />
    </StackPanel>
    <Border Grid.Row="2" Classes="dataGridHost">
      <TreeDataGrid Source="{Binding WarehousesSource}" />
    </Border>
  </Grid>
</UserControl>
```

```csharp
using Avalonia.Controls;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class WarehousesView : ReactiveUserControl<WarehousesViewModel>
{
    public WarehousesView() { }
}
```

Стилизация — только готовыми классами из `Views/Styles/` (`managementLayout`, `topbar`, `dataGridHost`, `Button.accent`, `Button.sidebarAction`, `Button.action`), не пиши inline-стили. UI-текст на русском.

## 3. Вкладка — `ViewModels/MainWindowViewModel.cs`

Добавь параметр в primary constructor и элемент в `TabItems`:

```csharp
public sealed partial class MainWindowViewModel(
    ClientsViewModel clients, WarehousesViewModel warehouses, ...)
    : ViewModelBase
{
    public ObservableCollection<ViewModelBase> TabItems { get; } =
        [ clients, warehouses, ... ];
}
```

Порядок вкладок — по смыслу для пользователя; администрация обычно последняя.

## 4. DataTemplate — `Views/MainWindow.axaml`

В `ContentControl.DataTemplates` главного окна добавь маппинг тип VM → View:

```xml
<DataTemplate DataType="{x:Type vm:WarehousesViewModel}">
    <views:WarehousesView DataContext="{Binding}" />
</DataTemplate>
```

## 5. Проверка

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
```

Запусти приложение (`Skills/run-app`), вкладка должна появиться с иконкой и заголовком.

Дальше: диалоги редактирования — скилл `add-dialog`; кнопки действий в `topbar` открывают их через `Interaction`.
