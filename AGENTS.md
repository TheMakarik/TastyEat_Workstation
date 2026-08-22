# AGENTS.md — TastyEat.Workstation

Инструкции для ИИ-агентов, работающих с этим репозиторием. Прочитай файл целиком перед внесением изменений. Этот файл расширяет и заменяет `TastyEat.Workstation/KIMI.md`.

## Что это за проект

Настольное приложение для малого пищевого производства и дистрибуции: ведение клиентов, товаров и цен, производственные партии, сбор заказов от клиентов, распределение продукции, аналитика (графики продаж) и администрирование (резервные копии БД, архивация логов). Одиночное рабочее место (workstation), без сети — локальная SQLite.

## Стек

- **.NET 10** (`net10.0`), C# 14
- **Avalonia 12** + тема Semi.Avalonia (`Locale="ru-RU"`) + Material.Icons + TreeDataGrid + LiveChartsCore
- **ReactiveUI** + `ReactiveUI.SourceGenerators` (`[Reactive]`, `[RelayCommand]`) + `ReactiveUI.Validation`
- **EF Core 10** (SQLite), миграции в `Migrations/`, применяются автоматически при старте
- **Generic Host** (`Microsoft.Extensions.Hosting`) + **Scrutor** (авто-регистрация сервисов и VM) + `IOptions`
- **Serilog** — консоль + файл с дневной ротацией
- `dotnet-ef 10.0.9` — единственный локальный инструмент (`dotnet-tools.json`)

## Команды

```bash
# сборка
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj

# запуск (окно загрузки → миграции → главное окно)
dotnet run --project TastyEat.Workstation

# новая миграция (миграции НЕ применяются командой update — они накатываются при старте приложения)
dotnet ef migrations add <ИмяМиграции> --project TastyEat.Workstation
```

База данных — файл `tastyeat.db` в каталоге приложения `%APPDATA%/tasty-eat/` (создаётся `ApplicationDataService`), рядом `logs/`, `backups/`, `Scripts/`. В корне репозитория лежит тестовая `TastyEat.Workstation/tastyeat.db` — не путать с рабочей.

## Структура каталогов

```
TastyEat.Workstation/
├── Program.cs, App.axaml(.cs), Bootstrapper.cs   # запуск: LoadingWindow → Bootstrapper → MainWindow
├── Models/
│   ├── DataContext.cs        # DbContext, fluent-конфигурация, длины колонок из appsettings
│   ├── Tables/               # POCO-сущности БД (Client, Product, OrderCollection, ...)
│   ├── Dto/                  # плоские DTO для редактирования и статистики
│   └── Analytics/            # DTO для графиков
├── Services/
│   ├── Interfaces/           # IXxxService
│   ├── HostedServices/       # фоновые задачи (бэкап БД, архив логов)
│   └── *.cs                  # реализация: работа с DbContext, маппинг в DTO
├── ViewModels/               # ReactiveObject + source generators; ViewModelBase (Title, IconName)
├── Views/
│   ├── *.axaml(.cs)          # экраны и окна редактирования
│   ├── Controls/             # переиспользуемые контролы (LoadingControl, SearchTextBox, SectionHeader)
│   ├── Converters/           # ТОЛЬКО MakripExtensions (см. раздел «Конвертеры»)
│   └── Styles/               # Common.axaml агрегирует ButtonStyles/ControlsStyles/ManagementViewStyles
├── Options/                  # классы настроек, привязанные к секциям appsettings.json
├── Messages/                 # записи для MessageBus.Current
└── Migrations/               # EF Core миграции
Skills/                       # скиллы для типовых задач этого репозитория (см. раздел «Скиллы»)
```

## Архитектура

### Запуск

`Program.cs` (Serilog, `RxApp.DefaultExceptionHandler`, `UseReactiveUI()`) → `App.axaml.cs`: показывается `LoadingWindow`, внутри запускается `Bootstrapper.BuildAppAsync(IProgress<double>)` с прогрессом в процентах → по готовности открывается `MainWindow`, loading закрывается. `Bootstrapper`:

1. `Host.CreateApplicationBuilder()` + `appsettings.json` (обязательный);
2. `Configure<TOptions>` для всех секций настроек;
3. `ApplicationDataService` создаётся вручную до build (нужны пути для логов/БД), регистрируется singleton;
4. Serilog: `WriteTo.File(LogsDirectory/log-.txt, RollingInterval.Day)`;
5. `AddDbContext<DataContext>(UseSqlite, ServiceLifetime.Transient)` — DbContext **transient**;
6. два hosted-сервиса: `LogArchiveHostedService`, `DatabaseBackupHostedService`;
7. Scrutor: все классы → `AsMatchingInterface()` → transient; классы с именем `*ViewModel` → `AsSelf()` → transient;
8. `app.StartAsync()` + `context.Database.MigrateAsync()` — миграции применяются автоматически.

### Слои и поток данных

```
Models/Tables (POCO)  →  Services (DbContext + DTO)  →  ViewModels (валидация, команды)  →  Views (compiled bindings)
```

- **ViewModel никогда не трогает БД** — только сервисы через конструктор (или `IServiceScopeFactory` для transient-сервисов внутри команд).
- **Сервисы**: `sealed class XService(DataContext context, ILogger<XService> logger) : IXService`, без репозиториев. Чтение — `AsNoTracking()` + `Include`/`ThenInclude`; запись — tracked entity + `SaveChangesAsync`. `CreateAsync/UpdateAsync` принимают DTO, возвращают сущность. После записи — структурированный лог: `logger.LogInformation("Client created: {ClientName} (Id: {ClientId})", ...)`.
- **DTO редактирования** — плоские `sealed` POCO с `Id`-ссылками вместо навигаций (`ClientEditDto`); аналитические — positional records (`ClientProductShareDto(...)`), часто считаются на сервере через `.Select(...)` прямо в SQL.
- **ViewModel-ы экранов**: `sealed partial class XxxViewModel : ViewModelBase`. `ViewModelBase` требует `Title` и `IconName` (название вкладки и имя иконки Material.Icons для `TabControl`). Данные для TreeDataGrid собираются в конструкторе VM: `FlatTreeDataGridSource<T>` или `HierarchicalTreeDataGridSource<T>` + `HierarchicalExpanderColumn`.
- **Окна редактирования**: transient VM с методом `Initialize(...)` для повторного использования, правила валидации в конструкторе через `this.ValidationRule(...)`, команда сохранения с гейтом `this.IsValid()`, результат — record `XxxEditResult` (или `bool`/`null` для отмены). View открывает окно через `Interaction<TInput, TOutput>` из VM + `RegisterHandler` в `WhenActivated` code-behind; окно закрывает само себя `Close(result)` по подписке на команду сохранения.
- **Сообщения между VM** — `MessageBus.Current.SendMessage(...)` / `.Listen<T>().ObserveOn(RxApp.MainThreadScheduler).Subscribe(...)` (см. `Messages/ClientPurchasesChangedMessage`).
- **Навигация**: `MainWindowViewModel` получает VM-экранов через primary constructor, публикует `ObservableCollection<ViewModelBase> TabItems`; `MainWindow.axaml` маппит тип VM → View через вложенные `DataTemplate`.

### Настройки (Options)

Класс в `Options/` (sealed POCO с дефолтами) + секция в `appsettings.json` с именем ровно `nameof(Класс)` + `Configure<T>` в `Bootstrapper`. Потребляются через `IOptions<T>`. `StringLengthOptions` уходит даже в `DataContext` (длины колонок БД) — при добавлении полей с ограничением длины добавляй значение туда.

## Правила кода (обязательные)

Расширенные правила из `KIMI.md`; проверяй изменения по этому списку перед завершением работы:

1. **Не используй БД в ViewModel.** Все обращения к данным — через сервисы. DI через конструктор.
2. **Все классы `sealed`**, где это возможно (наследников нет — `sealed` обязателен). VM с source generators — `sealed partial`.
3. **`var`** вместо явного типа, всегда.
4. **Сахар C# 14**: primary constructors (в т.ч. для классов), collection expressions (`= [];`, `[ a, b ]`), pattern matching, target-typed `new`.
5. **Без сокращений в именах**: `directory`, а не `dir`; `cancellationToken`, а не `token` в публичных API сервисов.
6. **Лаконичность**: без лишних `{}` (expression-bodied члены, `if (x) return;`), без пустых классов-обёрток и «на всякий случай» кода.
7. **Логгируй**: `ILogger<T>` в каждом сервисе; структурированные шаблоны с именованными плейсхолдерами, не интерполяция строк.
8. **CancellationToken**: каждый метод сервиса принимает `CancellationToken cancellationToken = default` и пробрасывает в EF-вызовы. В VM — паттерн отменяемой перезагрузки через `CancellationTokenSource` + `Interlocked.Exchange` (см. `ClientsViewModel.RefreshLoadCts`).
9. **Ошибки**: в командах — `catch (OperationCanceledException) {}` + `catch (Exception ex)` с логом; пользователю — через `Interaction`, не через `MessageBox`.
10. **Команды**: только `[RelayCommand]` (source generator), у async-команд явно `OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler"`, гейт через `CanExecute = nameof(_canExecute)` из валидации.
11. **AXAML**: compiled bindings по умолчанию включены в csproj — у каждого окна/вью `x:DataType`. Отключение (`x:CompileBindings="False"`) — только для сложных DataTemplate, как в `ClientsView.axaml`. Стилизация по классам (`Classes="accent"`) из `Views/Styles`, не inline-атрибуты.
12. **UI-текст — на русском** (запятая как разделитель дробных чисел и т.п.), тема Light, `SemiTheme Locale="ru-RU"`.

## Конвертеры: паттерн MakripExtensions (обязателен)

**Все конвертеры для bindings — это статический класс расширений `MakripExtensions` в `Views/Converters/MakripExtensions.cs`.** Никаких отдельных `IValueConverter`-классов и никаких `<conv:XxxConverter x:Key="..." />` в ресурсах окон.

### Почему так

- Классический `IValueConverter` — это бойлерплейт (`ConvertBack`, `targetType`, `CultureInfo`), регистрация экземпляра в ресурсах каждого окна, где нужен конвертер, и невозможность переиспользовать логику в C#-коде.
- `FuncValueConverter<TIn, TOut>` типизирован, не требует `ConvertBack`/`culture`, экземпляр создаётся один раз как статическое свойство и используется из любого AXAML через `{x:Static}` без ресурсов.
- Логика пишется как **extension-метод** — её можно вызывать из сервисов, VM и code-behind (`name.ToIconKind()`), а не только из XAML.

### Техническое ограничение (проверено сборкой на Avalonia 12.0.4)

`x:Static` в Avalonia 12 принимает **только статические поля и свойства** — `Converter={x:Static conv:MakripExtensions.MyMethod}` с методом или extension-методом напрямую **не компилируется** (ошибка `AVLN2000: Unable to resolve ... as static field, property, constant or enum value`). Поэтому схема двухэлементная:

1. логика — extension-метод (для C#-кода и переиспользования);
2. обёртка — статическое свойство `FuncValueConverter`, которое передаёт method group в конструктор (для XAML).

### Образец

```csharp
// Views/Converters/MakripExtensions.cs
using Avalonia.Data.Converters;
using Material.Icons;

namespace TastyEat.Workstation.Views.Converters;

public static class MakripExtensions
{
    // 1) Логика: extension-метод — вызывается из C# как name.ToIconKind()
    public static MaterialIconKind ToIconKind(this string? name) =>
        Enum.TryParse<MaterialIconKind>(name, out var kind) ? kind : default;

    // 2) Обёртка для XAML: имя свойства = имя метода + "Converter"
    public static FuncValueConverter<string?, MaterialIconKind> ToIconKindConverter { get; } = new(MakripExtensions.ToIconKind);
}
```

```xml
<!-- В любом axaml, без ресурсов: -->
xmlns:conv="using:TastyEat.Workstation.Views.Converters"

<MaterialIcon Kind="{Binding IconName, Converter={x:Static conv:MakripExtensions.ToIconKindConverter}}" />
```

### Правила

- Новый конвертер = extension-метод + свойство `FuncValueConverter` в том же классе `MakripExtensions`. Имя свойства — имя метода + суффикс `Converter`.
- Никогда не создавай новые классы `IValueConverter` и не объявляй конвертеры в `Window.Resources`/`UserControl.Resources`.
- Сначала проверь встроенные: `Avalonia.Data.Converters.StringConverters` (`NotNullOrEmpty`), `BoolConverters`, `MathConverters` — если подходит встроенный, используй его (`Converter={x:Static StringConverters.NotNullOrEmpty}`).
- Для нескольких входов — `FuncMultiValueConverter` (статическое свойство `IMultiValueConverter`), для параметризуемых — лямбда в свойстве.
- Метод должен принимать `TIn?` (binding может передать null, например до инициализации) и не бросать исключений.
- Текущий `StringToIconKindConverter` — легаси, подлежит замене на `ToIconKindConverter` из примера выше (вместе с `Window.Resources` в `MainWindow.axaml`).

## Скиллы

Для типовых задач в репозитории есть пошаговые скиллы — используй их целиком, а не изобретай порядок действий:

- `Skills/add-entity` — новая сущность БД: таблица + конфигурация + миграция + сервис + DTO
- `Skills/add-screen` — новый экран-вкладка: ViewModel + View + регистрация в навигации
- `Skills/add-dialog` — модальное окно редактирования: Interaction + EditResult + валидация
- `Skills/add-migration` — создание и применение миграций EF Core
- `Skills/add-converter` — конвертер в стиле MakripExtensions
- `Skills/add-options` — новая секция настроек appsettings.json + Options-класс
- `Skills/run-app` — сборка, запуск и диагностика приложения

## Известные проблемы (не забудь при первой возможности)

- **Сборка сломана**: утерян `Views/Utils/` с `ShowDialogAsync`, `GetOwnerWindow`, `MessageDialog`, `DeleteConfirmationDialog` (namespace `TastyEat.Workstation.Views.Utils`) — используется в 6 code-behind файлах (`AdministrationView`, `OrderCollectionView`, `ProductsView`, `DistributionEditWindow` и др.), но определений нет ни в рабочем дереве, ни в истории git. Нужно восстановить.
- `bin/` и `obj/` закоммичены в git — стоит добавить `.gitignore`.
- `App.axaml.cs` пишет отладочный лог в `/tmp/tastyeat_startup.log` — убрать после отладки.
- `IEntityService<T>`/`EntityService<T>` сейчас никем не используются (реальная работа через специализированные сервисы) — либо найти применение, либо удалить.
- `AdministrationOptions.ScriptsDirectoryName` есть в классе, но отсутствует в `appsettings.json` (работает дефолт `"Scripts"`).
