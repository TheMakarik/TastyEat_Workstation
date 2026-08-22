---
name: add-screen
description: Добавить новый экран-вкладку в TastyEat.Workstation на Avalonia.Markup.Declarative (C# UI без axaml) — компонент ScreenComponent со вложенным State на CommunityToolkit.Mvvm, таблица TreeDataGrid, регистрация вкладки в MainWindow, DI-скан Scrutor. Используй при просьбах добавить экран, вкладку, страницу, раздел интерфейса.
---

# Добавление экрана-вкладки (AMD, без axaml)

Весь UI — чистый C#. Образцы: `Components/ClientsScreen.cs` (плоская таблица), `Components/ProductsScreen.cs` (дерево), `Components/AdministrationScreen.cs` (без таблицы).

## 1. Компонент — `Components/<Name>Screen.cs`

```csharp
using Avalonia.Controls; // и др. по необходимости
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Components;

public sealed partial class WarehousesScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<WarehousesScreen> logger) : ScreenComponent<WarehousesScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;
    }

    public override string Title => "Склады";
    public override MaterialIconKind Icon => MaterialIconKind.Warehouse;

    protected override object Build(State state) => /* каркас ниже */;
}
```

Обязательно: `sealed partial` (генератору AMD нужен partial); состояние читай через `ScreenState` (не `State` — это вложенный тип). Команды — `[RelayCommand]` на приватных методах (в т.ч. с параметром-строкой: `EditNodeCommand`/`CommandParameter`).

## 2. Каркас (единый для экранов-таблиц)

```csharp
protected override object Build(State state)
{
    _source ??= BuildSource();  // Flat/HierarchicalTreeDataGridSource — лениво, НЕ в инициализаторе поля
    ...
    return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")
        .Children(
            UiFactory.Header(Icon, Title, "подзаголовок"),
            new Grid().Cols("*, Auto").Classes("topbar").Grid_Row(1)
                .Children(
                    new SearchTextBox { Width = 320 }.Text(state, x => x.SearchText, Avalonia.Data.BindingMode.TwoWay),
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }
                        .Children(UiFactory.ActionButton(MaterialIconKind.Plus, "Добавить", () => _ = AddAsync()))
                        .Grid_Column(1)),
            new Border().Classes("dataGridHost").Grid_Row(2)
                .Child(new TreeDataGrid { Source = _source }),
            UiFactory.LoadingOverlay(state, x => x.IsLoading).Grid_RowSpan(3));
}
```

Кнопка действий в строке — `TemplateColumn` + `FuncDataTemplate<Row>` + замыкание `OnClick(_ => ShowRowActions(row))`; меню — `MenuFlyout` с `Command` из `[RelayCommand]`-методов. Никаких Tag.

## 3. Данные

Загрузка — `Task.Run` + scope + сервисы, обновление коллекций через `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync`; отмена — `CancellationTokenSource` + `Interlocked.Exchange` (скопируй `RefreshLoadCts` из ClientsScreen); `catch (OperationCanceledException) {}` + `catch (Exception ex)` c логом на русском. Троттлинг поиска — `DispatcherTimer` 400мс Stop/Start по `state.PropertyChanged`. Стартовая загрузка — `Avalonia.Threading.Dispatcher.UIThread.Post(async () => await SearchAsync())` в конце Build. Межэкранные события — `WeakReferenceMessenger`.

## 4. Вкладка — `Views/MainWindow.cs`

Добавь экран в массив `screens` (резолвится из DI — Scrutor регистрирует `*Screen` автоматически).

## 5. Проверка

`dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj` — если падает CS1955 на fluent-методах AMD, проверь наличие `Microsoft.Net.Compilers.Toolset` в csproj (генератор AMD требует новый Roslyn).

Диалоги редактирования — скилл `add-dialog`.
