---
name: add-entity
description: Добавить новую сущность/таблицу БД в TastyEat.Workstation (Avalonia + EF Core SQLite) — таблица POCO, конфигурация DataContext, сервис с интерфейсом, DTO, миграция. Используй, когда пользователь просит добавить сущность, таблицу, объект хранения, новую доменную область с данными в базе, CRUD для новой таблицы.
---

# Добавление новой сущности БД

Порядок обязателен: таблица → конфигурация → (настройки длин) → миграция → сервис → DTO. В конце проверь сборку.

## 1. Таблица — `Models/Tables/<Name>.cs`

Чистый POCO: `sealed class`, без атрибутов данных (конфигурация только fluent в `DataContext`), без primary constructor, навигации `null!`, коллекции `= []`:

```csharp
namespace TastyEat.Workstation.Models.Tables;

public sealed class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public City City { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
```

## 2. Регистрация в `Models/DataContext.cs`

`DbSet` + fluent-конфигурация в `OnModelCreating`. Длины строк — не хардкод, а из `_stringLengthOptions` (значения задаются в `appsettings.json`, см. скилл `add-options`). Уникальные поля — `HasIndex(...).IsUnique()`, даты создания — `HasDefaultValueSql("datetime('now')")`:

```csharp
public DbSet<Warehouse> Warehouses { get; set; }

// в OnModelCreating:
modelBuilder.Entity<Warehouse>()
    .Property(e => e.Name).HasMaxLength(_stringLengthOptions.WarehouseNameMaxLength);
modelBuilder.Entity<Warehouse>()
    .HasIndex(e => e.Name).IsUnique();
modelBuilder.Entity<Warehouse>()
    .Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
```

Если появились новые строковые длины — добавь свойства в `Options/StringLengthOptions.cs` и секцию `StringLengthOptions` в `appsettings.json` ДО создания миграции (длины попадают в миграцию).

## 3. Миграция

Из корня репозитория (детали и откаты — скилл `add-migration`):

```bash
dotnet ef migrations add Add<Name> --project TastyEat.Workstation
```

Проверь сгенерированные `Up`/`Down` в `Migrations/`. Ничего не выполняй вручную — миграции применяются автоматически при старте приложения (`Bootstrapper` вызывает `MigrateAsync`).

## 4. Сервис — `Services/Interfaces/I<Name>Service.cs` + `Services/<Name>Service.cs`

Интерфейс + реализация с primary constructor. Вручную в DI НЕ регистрируй — Scrutor в `Bootstrapper` сам связывает `XService → IXService` (transient) по конвенции `AsMatchingInterface`.

Правила реализации:
- чтение — `AsNoTracking()` (+ `Include`/`ThenInclude` навигаций), сортировка на сервере;
- запись — tracked entity + `SaveChangesAsync`;
- `CreateAsync`/`UpdateAsync` принимают DTO, возвращают сущность;
- каждый метод принимает `CancellationToken cancellationToken = default` и пробрасывает его в EF-вызовы;
- структурированный лог после записи: `logger.LogInformation("Warehouse created: {WarehouseName} (Id: {WarehouseId})", ...)`; при отсутствии записи — `LogWarning` и тихий выход, не исключение.

```csharp
public interface IWarehouseService
{
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Warehouse> CreateAsync(WarehouseEditDto dto, CancellationToken cancellationToken = default);
}

public sealed class WarehouseService(DataContext context, ILogger<WarehouseService> logger) : IWarehouseService
{
    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

    public async Task<Warehouse> CreateAsync(WarehouseEditDto dto, CancellationToken cancellationToken = default)
    {
        var warehouse = new Warehouse { Name = dto.Name.Trim(), CityId = dto.CityId };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Warehouse created: {WarehouseName} (Id: {WarehouseId})", warehouse.Name, warehouse.Id);
        return warehouse;
    }
}
```

## 5. DTO — `Models/Dto/<Name>EditDto.cs` (если сущность редактируется из UI)

Плоский sealed POCO с `Id`-ссылками вместо навигаций (образец — `ClientEditDto`). Маппинг VM → DTO делает ViewModel, DTO → сущность — сервис.

## 6. Проверка

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
```

Затем создай экран (скилл `add-screen`, компонент `Components/<Name>Screen.cs`) и окно редактирования (скилл `add-dialog`, `Components/Dialogs/<Name>Dialog.cs`) если сущность нужна в UI.

## Частые ошибки

- Атрибуты `[Required]`/`[MaxLength]` на POCO — не используй, только fluent-конфигурация.
- Обращение к `DataContext` из ViewModel — запрещено, только через сервис.
- Ручная регистрация сервиса в `Bootstrapper` — не нужна (кроме singleton'ов типа `ApplicationDataService`).
- `dotnet ef database update` — не запускай, миграции применяются при старте приложения.
