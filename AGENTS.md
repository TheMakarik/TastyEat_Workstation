# AGENTS.md — TastyEat.Workstation

Инструкции для ИИ-агентов, работающих с этим репозиторием. Прочитай файл целиком перед внесением изменений.

## Что это за проект

Настольное приложение для малого пищевого производства и дистрибуции: ведение клиентов, товаров и цен, производственные партии, сбор заказов от клиентов, распределение продукции, администрирование (резервные копии БД, архивация логов). Одиночное рабочее место (workstation), без сети — локальная SQLite.

## Стек

- **.NET 10** (`net10.0`), C# 14 (`LangVersion latest`)
- **Avalonia 12.1.1** + **Avalonia.Markup.Declarative 12.1.1** (C# UI, БЕЗ axaml) + тема Semi.Avalonia (`Locale="ru-RU"`) + Material.Icons + TreeDataGrid
- **CommunityToolkit.Mvvm 8.4.2** — `[ObservableProperty]` (partial properties), `[RelayCommand]`, `WeakReferenceMessenger`. ReactiveUI полностью удалён.
- **Microsoft.Net.Compilers.Toolset 5.6.0** — ОБЯЗАТЕЛЕН: source-generator AMD требует Roslyn ≥ 5.3, а SDK 10.0.110 поставляет 5.0. Без пакета генератор AMD не загрузится (ошибки CS1955 на `.Text(...)`, `.ItemsSource(...)` и т.п.)
- **EF Core 10** (SQLite), миграции в `Migrations/`, применяются автоматически при старте
- **Generic Host** + **Scrutor** (авто-регистрация) + `IOptions`; **Serilog**
- LiveChartsCore — в зависимостях (графические окна не перенесены, см. «Известные проблемы»)

## Команды

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
dotnet run --project TastyEat.Workstation
dotnet ef migrations add <ИмяМиграции> --project TastyEat.Workstation   # применится при старте приложения
```

База — `%APPDATA%/tasty-eat/tastyeat.db` (Linux: `~/.local/share/tasty-eat/`), рядом `logs/`, `backups/`, `Scripts/`. `appsettings.json` ищется от `AppContext.BaseDirectory` — запуск работает из любого каталога.

## Структура

```
TastyEat.Workstation/
├── Program.cs, App.cs, Bootstrapper.cs        # App: SemiTheme+стили в C#, LoadingWindow → Bootstrapper → MainWindow
├── Models/ (Tables, Dto, Analytics, DataContext.cs)
├── Services/ (+ Interfaces/, HostedServices/)
├── Components/                                # ЭКРАНЫ: XxxScreen : ScreenComponent<XxxScreen.State>
│   └── Dialogs/                               # ДИАЛОГИ: XxxDialog : Window (+ State внутри)
├── Ui/                                        # ScreenComponent, UiFactory, AppStyles, SearchTextBox, ChartColors
├── Messages/                                  # ClientPurchasesChangedMessage (WeakReferenceMessenger)
├── Views/                                     # MainWindow.cs, LoadingWindow.cs, Utils/ (MessageDialog, DeleteConfirmationDialog, DialogExtensions), ChartColorProvider.cs
└── Options/
Skills/                                        # скиллы для типовых задач
```

**AXAML-файлов в проекте нет вообще.** Весь UI — C#.

## Паттерн компонента (обязателен)

Экран — `partial class` (генератор AMD требует `partial`), состояние — вложенный `sealed partial class State : ObservableObject`. Свойства State — только через `[ObservableProperty]` на **partial-свойствах** (C# 14). Биндинги — compiled-binding-расширения AMD: `.Text(state, x => x.P)` / TwoWay / `.ItemsSource`, `.IsVisible`, `.Value`.

```csharp
public sealed partial class ClientsScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<ClientsScreen> logger) : ScreenComponent<ClientsScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;
    }

    public override string Title => "Клиенты";
    public override MaterialIconKind Icon => MaterialIconKind.AccountMultiple;

    private FlatTreeDataGridSource<Row>? _source;               // источники таблиц — лениво, в Build
    protected override object Build(State state)
    {
        _source ??= BuildSource();                              // НЕ в инициализаторах полей (CS0236)
        return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")...
    }
}
```

Проверенный API AMD: `.Children(...)`, `.Rows("Auto, *")`, `.Cols(...)`, `.Grid_Row(1)`, `.Grid_RowSpan(3)`, `.Grid_Column(0)`, `.Classes("accent")`, `.Text/.Value/.ItemsSource/.SelectedItem/.IsVisible/.IsEnabled/.IsChecked(state, x => x.P[, BindingMode.TwoWay])`, `.OnClick(async _ => await ...)`, `.PlaceholderText(...)`, `.Spacing(12)`, `.Margin(new Thickness(...))`. Если метод-расширение под вопросом — используй object initializer. События кроме клика — контрол в переменную + прямая подписка.

Грабли, проверенные сборкой:
- Свойство состояния базы называется `ScreenState` (не `State` — конфликт с вложенным типом).
- Внутри классов-наследников `Control` пиши `Avalonia.Threading.Dispatcher.UIThread` (свойство `Dispatcher` затеняет класс) и `Avalonia.Media.FontWeight.Bold` (свойство FontWeight затеняет enum).
- `Margin(0,1,0,1)` (4 аргумента) НЕ существует — только `.Margin(new Thickness(...))`.
- Инициализаторы полей не могут вызывать методы экземпляра (CS0236) — строй источники TreeDataGrid лениво в `Build`.

## Архитектура

- Запуск: `Program.cs` → `App.RunStartupAsync`: LoadingWindow → `Bootstrapper.BuildAppAsync` (конфиг → каталоги → Serilog → DI → миграции) → MainWindow.
- DI (Scrutor): все сервисы → `AsMatchingInterface()` transient; классы с именем `*Screen`/`*Dialog` → `AsSelf()` transient. Диалоги резолвятся из scope и переиспользуются через `Initialize(...)`.
- Слои: `Models/Tables` → `Services` (DTO, AsNoTracking/Include, CancellationToken, логи на русском) → `Components` (State + Build). ViewModel-папки нет; ViewModel никогда не трогает БД — только сервисы.
- Диалоги: `var dialog = scope.ServiceProvider.GetRequiredService<XxxDialog>(); dialog.Initialize(...); var result = await dialog.ShowDialog<T?>(this.GetOwnerWindow());` — у окна `Result`/`Close(result)`, у кнопки Отмена `Close(null)`. Валидация — computed-свойства `XxxError` + `CanSave` в State (partial-методы `OnXChanged` → `RaiseValidation()`), кнопка `.IsEnabled(state, x => x.CanSave)`.
- Сообщения: `WeakReferenceMessenger.Default.Send/...Register<ClientPurchasesChangedMessage>` (Productions отправляет, Clients перезагружается).
- Меню строки таблицы: НЕ Tag — `MenuFlyout` с `Command` из `[RelayCommand]`-методов и `CommandParameter = row`; кнопка действия в `TemplateColumn` через `FuncDataTemplate` + замыкание.
- Троттлинг поиска: `DispatcherTimer` 400 мс (Stop/Start по PropertyChanged), отмена загрузок — `CancellationTokenSource` + `Interlocked.Exchange` (см. `RefreshLoadCts`).
- Стили: `Ui/AppStyles.cs` (порт старых axaml-стилей; классы `accent`, `sidebarAction`, `action`, `topbar`, `dataGridHost`, `managementLayout`, TabItem, тюнинг TextBox/ComboBox/NumericUpDown/Calendar). Кисти — `AppStyles.Accent/AccentLight/AccentPurple`.
- Общие UI-хелперы: `UiFactory.Header/ActionButton/DialogField/ErrorText/LoadingOverlay`, контрол `SearchTextBox`.
- Настройки: класс в `Options/` + секция `nameof` в appsettings.json + `Configure<T>` в Bootstrapper, потребление через `IOptions<T>`. `StringLengthOptions` уходит в `DataContext`.

## Правила кода (обязательные)

1. **Никаких AXAML-файлов.** Весь UI — C# через AMD.
2. **Никаких Tag-хаков** — только замыкания и CommandParameter.
3. **Никаких конвертеров** — computed-свойства State (`CounterLabel => $"..."`) и extension-методы для C#-кода.
4. Не используй БД в компонентах — только сервисы (через ctor или `IServiceScopeFactory`).
5. Все классы `sealed` (компоненты/State — `sealed partial`).
6. `var` всегда; сахар C# 14 (primary constructors, collection expressions, partial properties).
7. Без сокращений (`directory`, `cancellationToken`); лаконичность, без пустых обёрток.
8. Логгируй: `ILogger<T>`, структурированные шаблоны, **сообщения на русском**.
9. CancellationToken в сервисах; в экранах — отменяемая перезагрузка (CTS + Interlocked).
10. Ошибки: `catch (OperationCanceledException) {}` + `catch (Exception ex)` с логом; пользователю — `MessageDialog`/`DeleteConfirmationDialog` из `Views/Utils`.
11. UI-текст на русском; тема Light.

## Скиллы

- `Skills/add-entity` — новая сущность БД
- `Skills/add-screen` — новый экран-вкладка (AMD-паттерн)
- `Skills/add-dialog` — модальное окно (AMD-паттерн)
- `Skills/add-migration` — миграции EF Core
- `Skills/add-options` — секция настроек
- `Skills/run-app` — сборка, запуск, диагностика

## Известные проблемы

- Графические окна (PieChart/LineChart) не перенесены — в старом UI кнопки были закомментированы (мёртвая фича). `Views/ChartColorProvider.cs` и пакет LiveChartsCore пока не используются.
- `bin/`, `obj/` закоммичены в git — нужен `.gitignore`.
- `IEntityService<T>`/`EntityService<T>` не используются — удалить или найти применение.
- `AdministrationOptions.ScriptsDirectoryName` есть в классе, отсутствует в appsettings.json.
- NU1903: транзитивный `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 с уязвимостью — уйдёт с обновлением EF Core.
