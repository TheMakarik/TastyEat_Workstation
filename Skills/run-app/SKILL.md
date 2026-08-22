---
name: run-app
description: Собрать, запустить и диагностировать TastyEat.Workstation (Avalonia .NET 10, SQLite, Serilog) — сборка, запуск, где лежат логи и база, как читать ошибки старта, известные проблемы сборки. Используй при любых просьбах собрать/запустить/отладить приложение, при падении старта, ошибках сборки, вопросах где база или логи.
---

# Сборка, запуск, диагностика

## Сборка и запуск

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
dotnet run --project TastyEat.Workstation
```

Порядок старта: `LoadingWindow` (прогресс по `IProgress<double>`) → `Bootstrapper.BuildAppAsync` (конфиг → каталоги → Serilog → DI → hosted-сервисы → миграция БД) → `MainWindow`. Задержка на LoadingWindow с прогрессом — норма, это не зависание.

## Где данные (Linux: `~/.local/share/tasty-eat/`, Windows: `%APPDATA%\tasty-eat\`)

- База: `tastyeat.db` — рабочая в каталоге данных приложения (создаётся `ApplicationDataService` при первом старте); `TastyEat.Workstation/tastyeat.db` в репозитории — тестовая, не путай.
- Логи Serilog: `logs/log-ГГГГММДД.txt` (ротация ежедневно, старше `LogArchiveAfterDays` дней — автоматом в zip-архив `logs_*.7z`).
- Резервные копии: `backups/tastyeat_*.db` (фоновый `DatabaseBackupHostedService` раз в `DatabaseBackupIntervalDays` дней + ручной бэкап из экрана «Администрирование»).

## Диагностика по симптомам

- **Ошибки CS1955 «невызываемый член ... не может использоваться как метод» на `.Text(...)`, `.ItemsSource(...)`** — не загрузился source-generator Avalonia.Markup.Declarative: он требует Roslyn ≥ 5.3, а SDK 10.0.110 поставляет 5.0. Проверь наличие `Microsoft.Net.Compilers.Toolset` 5.6.0 в csproj (обязателен). Также смотри предупреждение CS9057 в выводе сборки.
- **UI — только C# (Avalonia.Markup.Declarative), axaml-файлов нет.** Компоненты: `Components/*Screen.cs`, диалоги: `Components/Dialogs/*Dialog.cs`; паттерн — в AGENTS.md.
- **Приложение не открывается после LoadingWindow** — смотри консоль и последний `log-*.txt`: конфиг берётся из каталога приложения (`AppContext.BaseDirectory`), поэтому правки `appsettings.json` в репозитории требуют пересборки (`CopyToOutputDirectory`); миграции падают при несовместимой схеме SQLite.
- **`SQLite Error 5: database is locked`** — в `DataContext` уже стоит `PRAGMA busy_timeout = 5000`; проверь, что не держишь базу открытым внешним инструментом (DB Browser и т.п.).
- **Пустое окно/вкладка без данных** — проверь сервис и данные в тестовой базе; VM экранов грузятся конструктором, ошибки глотаются `catch (Exception)` с записью в лог — ищи `Failed to load ...` в логах.
- **Изменения XAML не применяются** — compiled bindings: у вью должен быть `x:DataType`; несоответствие свойств ловится на этапе сборки (`AVLN2xxx`).
- **Diagnostics-окно** (F12-инструменты AvaloniaUI.DiagnosticsSupport) доступно только в Debug-сборке.
- **dotnet ef отсутствует** — `dotnet tool restore` (объявлен в `dotnet-tools.json`).

## Перед завершением работы над кодом

- `dotnet build` без ошибок и предупреждений.
- Прогони приложение, если менял startup/миграции/сервисы.
- Сверься с чек-листом правил кода в AGENTS.md (sealed, var, primary constructors, логирование, CancellationToken).
