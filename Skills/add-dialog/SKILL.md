---
name: add-dialog
description: Добавить модальное окно редактирования в TastyEat.Workstation (Avalonia + ReactiveUI) — ValidatableViewModelBase с правилами валидации и Initialize, EditResult record, Interaction из родительского VM, RegisterHandler в code-behind, самозакрытие окна по SaveCommand. Используй, когда пользователь просит добавить окно, диалог, форму редактирования/создания сущности, подтверждение удаления.
---

# Добавление модального окна редактирования

Паттерн проекта: родительский VM объявляет `Interaction<EditViewModel, EditResult?>`, code-behind родителя открывает окно, окно закрывает само себя по команде сохранения. VM — transient и переиспользуется через `Initialize(...)`.

## 1. Результат — record в файле VM редактирования

```csharp
public sealed record WarehouseEditResult(Warehouse Warehouse, bool IsNew);
```

## 2. ViewModel редактирования — `ViewModels/<Name>EditViewModel.cs`

`sealed partial class : ValidatableViewModelBase`. Правила валидации — в конструкторе, гейт команды — из валидности. `Initialize(...)` вызывается перед каждым показом (VM transient и кэшируется/переиспользуется, состояние сбрасывается руками):

```csharp
public sealed partial class WarehouseEditViewModel(
    IServiceScopeFactory scopeFactory,
    IOptions<StringLengthOptions> stringLengthOptions) : ValidatableViewModelBase
{
    private readonly StringLengthOptions _stringLengthOptions = stringLengthOptions.Value;
    private readonly IObservable<bool> _canExecute = default!;

    [Reactive]
    private string _title = "Добавить склад";

    [Reactive]
    private string _name = string.Empty;

    public int Id { get; private set; }
    public bool IsNew => Id == 0;

    public void Initialize(Warehouse? warehouse)
    {
        Id = warehouse?.Id ?? 0;
        _name = warehouse?.Name ?? string.Empty;
        _title = IsNew ? "Добавить склад" : $"Редактировать: {warehouse!.Name}";
        this.RaisePropertyChanged(nameof(IsNew));
    }

    protected override void OnInitialized()
    {
        this.ValidationRule(vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name) && name.Length <= _stringLengthOptions.WarehouseNameMaxLength,
            "Введите название (не длиннее максимума)");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    [RelayCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<WarehouseEditResult?> SaveAsync()
    {
        // scope + resolve transient-сервиса, маппинг VM -> WarehouseEditDto, Create/Update по IsNew
        // образец: ClientEditViewModel.SaveAsync
    }
}
```

Образцы целиком: `ClientEditViewModel`, `ProductEditViewModel`, `DistributionEditViewModel`.

## 3. Окно — `Views/<Name>EditWindow.axaml` + `.axaml.cs`

Обычная `Window` с `x:DataType` (или `reactive:ReactiveWindow<T>`). Кнопки: «Сохранить» — `Command="{Binding SaveCommand}"`, «Отмена» — `Click="CancelButton_Click"`. Code-behind закрывает окно сам:

```csharp
public partial class WarehouseEditWindow : Window
{
    public WarehouseEditWindow()
    {
        InitializeComponent();
        ViewModel?.SaveCommand.Subscribe(result => Close(result));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}
```

Окно центрируй (`WindowStartupLocation="CenterOwner"`, `SizeToContent="WidthAndHeight"`), стиль кнопок — `Classes="accent"` для сохранения.

## 4. Родительский VM — Interaction и команда открытия

```csharp
public Interaction<WarehouseEditViewModel, WarehouseEditResult?> EditWarehouseInteraction { get; } = new();

[RelayCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
private async Task EditWarehouseAsync(Warehouse? warehouse = null)
{
    await using var scope = scopeFactory.CreateAsyncScope();
    var editViewModel = scope.ServiceProvider.GetRequiredService<WarehouseEditViewModel>();
    editViewModel.Initialize(warehouse);
    var result = await EditWarehouseInteraction.Handle(editViewModel);
    if (result is not null)
        await SearchAsync(); // перезагрузить данные
}
```

## 5. Code-behind родительского View — открытие окна

```csharp
this.WhenActivated(disposables =>
{
    ViewModel?.EditWarehouseInteraction.RegisterHandler(async interaction =>
        await interaction.ShowDialogAsync(this, vm => new WarehouseEditWindow { DataContext = vm }))
        .DisposeWith(disposables);
});
```

ВНИМАНИЕ: `ShowDialogAsync` живёт в утерянном файле `Views/Utils/` — сейчас сборка проекта сломана (см. «Известные проблемы» в AGENTS.md). Если получаешь ошибки `CS0103`/`CS0246` на `ShowDialogAsync`/`GetOwnerWindow`/`MessageDialog` — сначала восстанови utils-расширения в `TastyEat.Workstation/Views/Utils/` (namespace `TastyEat.Workstation.Views.Utils`), затем продолжай. Временная альтернатива без utils: `await new WarehouseEditWindow { DataContext = vm }.ShowDialog<WarehouseEditResult?>(GetOwnerWindow())`.

## 6. Проверка

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
```

## Частые ошибки

- Не создавай окно редактирования из ViewModel напрямую (`new Window()` в VM) — только через `Interaction`, иначе VM перестаёт быть тестируемым.
- Не пересоздавай edit-VM на каждое открытие — вызывай `Initialize(...)`.
- Сохранение с невалидной формой должно быть невозможно: всегда геть `CanExecute` через `this.IsValid()`.
- После успешного сохранения не забудь оповестить другие экраны при необходимости: `MessageBus.Current.SendMessage(...)` (образец `ClientPurchasesChangedMessage`).
