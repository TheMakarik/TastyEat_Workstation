---
name: add-options
description: Добавить секцию настроек в TastyEat.Workstation — Options-класс, секция в appsettings.json, регистрация Configure в Bootstrapper, потребление через IOptions. Используй, когда пользователь просит добавить настройку, параметр конфигурации, вынести что-то в конфиг, поменять appsettings.json, упоминает StringLengthOptions, ClientValidationOptions, LoadingAnimationOptions или AdministrationOptions.
---

# Добавление настроек (Options)

Все настройки приложения живут в `appsettings.json` и читаются через `IOptions<T>`. Никакого `Configuration` в ViewModel и сервисах напрямую.

## Шаги

1. **Класс — `Options/<Name>Options.cs`**: sealed POCO со значениями по умолчанию (по умолчанию приложение должно запускаться даже без секции в конфиге):

```csharp
namespace TastyEat.Workstation.Options;

public sealed class BackupOptions
{
    public int RetentionCount { get; init; } = 5;
    public string BackupDirectoryName { get; init; } = "backups";
}
```

2. **Секция в `TastyEat.Workstation/appsettings.json`** — имя секции ровно `nameof(<Name>Options)`:

```json
{
  "BackupOptions": {
    "RetentionCount": 5,
    "BackupDirectoryName": "backups"
  }
}
```

3. **Регистрация в `Bootstrapper.BuildAppAsync`** рядом с остальными:

```csharp
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection(nameof(BackupOptions)));
```

4. **Потребление** — через `IOptions<T>` в конструкторе сервиса/VM (в VM — с сохранением `.Value` в поле, образец `ProductEditViewModel`):

```csharp
public sealed class BackupService(IOptions<BackupOptions> options, ILogger<BackupService> logger) : IBackupService
{
    private readonly BackupOptions _options = options.Value;
}
```

5. **Проверка**: `dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj` и запуск — `appsettings.json` обязателен (`optional: false`), ошибка в нём уронит старт.

## Особые случаи

- **Раннее использование до build** (нужно до построения host, как `AdministrationOptions` для `ApplicationDataService`): `var options = builder.Configuration.GetSection(nameof(XOptions)).Get<XOptions>() ?? new XOptions();`.
- **Длины строк БД** — только через `StringLengthOptions`: значение используется в `DataContext.OnModelCreating` и меняет схему БД → после изменения нужна миграция (скилл `add-migration`).
- **Фоновые сервисы** берут свои интервалы из `AdministrationOptions` (`LogArchiveAfterDays`, `DatabaseBackupIntervalDays`); если добавляешь туда поле — не забудь дефолт в классе, в json его можно не дублировать (пример: `ScriptsDirectoryName`).

## Частые ошибки

- Не читай `appsettings.json` вручную (`AddJsonFile` уже сделан в Bootstrapper) и не хардкодь пути в сервисах — пути каталогов (`logs/`, `backups/`, `Scripts/`) даёт `IApplicationDataService`.
- Не регистрируй Options как singleton-класс — только `Configure<T>` + `IOptions<T>`.
- Не забывай дефолты в классе: у пользователей старый конфиг без новой секции должен продолжать работать.
