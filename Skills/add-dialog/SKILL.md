---
name: add-dialog
description: Добавить модальное окно в TastyEat.Workstation на Avalonia.Markup.Declarative (C# UI без axaml) — Window со вложенным State, computed-валидацией (XxxError/CanSave), Initialize для переиспользования, открытие ShowDialog(GetOwnerWindow()) из экрана. Используй при просьбах добавить окно, диалог, форму редактирования/создания, подтверждение удаления.
---

# Добавление модального окна (AMD, без axaml)

Образцы: `Components/Dialogs/ClientEditDialog.cs` (валидация), `Components/Dialogs/ProductionEditDialog.cs` (динамические строки), `Components/Dialogs/OrderCollectionClientEditDialog.cs` (группы + фильтры).

## 1. Диалог — `Components/Dialogs/<Name>Dialog.cs`

Обычный класс `: Window` (не primary-ctor с параметрами вместе с пустым конструктором — CS8862), зависимости — поля через конструктор:

```csharp
public sealed partial class WarehouseEditDialog : Window
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        public string? NameError => string.IsNullOrWhiteSpace(Name) ? "Название обязательно" : null;
        public bool CanSave => NameError is null;

        partial void OnNameChanged(string value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state;

    public WarehouseEditDialog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Width = 440; Height = 380; CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { Text = "Склад", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new StackPanel { Spacing = 16, Margin = new Thickness(0, 20, 0, 0) }.Grid_Row(1)
                    .Children(UiFactory.DialogField("Название",
                        new TextBox().Text(_state, x => x.Name, Avalonia.Data.BindingMode.TwoWay),
                        UiFactory.ErrorText(_state, x => x.NameError))),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }
}
```

Scrutor регистрирует `*Dialog` в DI автоматически (transient). Ссылочные списки (города, клиенты) — `ObservableCollection` в State + `.ItemsSource(_state, ...)`; для ComboBox задай `ItemTemplate = new FuncDataTemplate<T>((x, _) => new TextBlock { Text = x?.Name })`.

## 2. Initialize + Result

`public void Initialize(...)` сбрасывает State под режим добавления/редактирования (окно переиспользуется); результат — `Close(result)` / `Close(null)`. Заголовок окна динамический — подпишись на `PropertyChanged` State и обновляй `Title`. Асинхронные проверки (уникальность) — флаг `ServerError` в State, проверяется в Save перед записью (образец `CityEditDialog`).

## 3. Открытие из экрана

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var dialog = scope.ServiceProvider.GetRequiredService<WarehouseEditDialog>();
dialog.Initialize(warehouse);
var result = await dialog.ShowDialog<WarehouseEditResult?>(this.GetOwnerWindow());
if (result is not null)
    await SearchAsync();
```

Подтверждения — `MessageDialog` (ShowInfoAsync/ConfirmAsync/ConfirmCancelAsync/ChoiceAsync) и `DeleteConfirmationDialog.ShowAsync` из `Views/Utils`; `GetOwnerWindow()` — там же.

## 4. Проверка

`dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj`. Грабли: `FontWeight`/`Dispatcher` внутри Window пиши полностью квалифицированно; `Margin` — только `new Thickness(...)`; строки-подсписки (ItemsControl) — шаблон `FuncDataTemplate<Row>((row, _) => BuildRowUi(row))`.
