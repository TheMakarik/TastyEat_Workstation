---
name: add-migration
description: Создать, проверить или откатить миграцию EF Core в TastyEat.Workstation (SQLite, dotnet-ef 10.0.9). Используй, когда пользователь просит добавить миграцию, изменить схему БД/таблиц, обновить базу, откатить миграцию, или после любых правок Models/Tables или DataContext.
---

# Миграции EF Core

Инструмент `dotnet-ef 10.0.9` уже объявлен в `dotnet-tools.json` в корне репозитория (и продублирован в каталоге проекта). Если команды `dotnet ef` нет — `dotnet tool restore`.

## Создание

Из корня репозитория:

```bash
dotnet ef migrations add <ИмяМиграции> --project TastyEat.Workstation
```

Имена в стиле существующих: `AddOrderCollections`, `RefactorDistributionClients`, `RemoveProductDescription` (PascalCase, глагол + объект, по-английски).

## Обязательная проверка перед коммитом

1. Открой сгенерированные `Migrations/<timestamp>_<Name>.cs` и проверь `Up`/`Down`: состав колонок, индексы, дефолты соответствуют задумке.
2. `DataContextModelSnapshot.cs` обновится сам — не редактируй его руками и не удаляй.
3. Собери проект: `dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj`.

## Применение

`dotnet ef database update` НЕ используется: миграции применяются автоматически при каждом запуске приложения (`Bootstrapper.BuildAppAsync` → `context.Database.MigrateAsync()`). Чтобы применить — просто запусти приложение:

```bash
dotnet run --project TastyEat.Workstation
```

Важно для SQLite: `MigrateAsync` не умеет менять колонки на месте — EF генерирует таблицу заново только при наличии пустой БД; для разрушающих изменений на существующей рабочей базе проверяй сгенерированный `Up` на `DropColumn`/`AlterColumn` и предупреждай пользователя о потере данных.

## Откат

- Последняя миграция ещё не закоммичена/не применялась: `dotnet ef migrations remove --project TastyEat.Workstation` (удалит файлы и откатит snapshot).
- Сброс всей базы на чистую: останови приложение и удали файл `tastyeat.db` (и `-wal`/`-shm` файлы рядом) в `%APPDATA%/tasty-eat/` — при следующем старте база пересоздастся всеми миграциями с нуля. Это уничтожает данные — делай только с явного разрешения пользователя.

## Связанное

- Длины строковых колонок приходят из `StringLengthOptions` (`appsettings.json`) — изменение длин требует новой миграции.
- Рабочая база и логи лежат в `%APPDATA%/tasty-eat/` (`ApplicationDataService`), тестовая `tastyeat.db` в каталоге проекта — не путай их при отладке.
- Автоматический бэкап базы делает `DatabaseBackupHostedService` (раз в `AdministrationOptions.DatabaseBackupIntervalDays` дней, копия в `backups/`).
