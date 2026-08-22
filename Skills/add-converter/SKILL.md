---
name: add-converter
description: Добавить конвертер для binding в TastyEat.Workstation (Avalonia) по паттерну MakripExtensions — extension-метод + статическое свойство FuncValueConverter + использование через x:Static. Используй, когда пользователю нужно преобразовать значение в интерфейсе (скрыть/показать, формат money/даты, иконки, булево в видимость), при упоминании конвертера, IValueConverter, FuncValueConverter, MakripExtensions.
---

# Конвертеры: паттерн MakripExtensions

Единственный правильный способ — статический класс `Views/Converters/MakripExtensions.cs`. Запрещено: новые классы `IValueConverter`, объявление конвертеров в `Window.Resources` через `x:Key` + `StaticResource`.

## Шаги

1. **Проверь встроенные конвертеры Avalonia** — если подходит, ничего писать не надо:

```xml
<TextBlock IsVisible="{Binding Name, Converter={x:Static StringConverters.NotNullOrEmpty}}" />
```

(`StringConverters.NotNull`, `StringConverters.NotNullOrEmpty`, `BoolConverters.True/False/Null`, `MathConverters`. Простое форматирование — `StringFormat` у binding.)

2. **Добавь extension-метод в `MakripExtensions`** — он же переиспользуется в C#-коде:

```csharp
// Views/Converters/MakripExtensions.cs
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TastyEat.Workstation.Views.Converters;

public static class MakripExtensions
{
    public static string ToMoney(this decimal amount) =>
        amount.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";

    public static IBrush ToStatusBrush(this bool isActive) =>
        isActive ? Brushes.SeaGreen : Brushes.Gray;

    // ...существующие конвертеры
}
```

3. **Добавь статическое свойство `FuncValueConverter`** (имя = метод + суффикс `Converter`) — оно отдаёт метод в XAML:

```csharp
    public static FuncValueConverter<decimal, string> ToMoneyConverter { get; } = new(MakripExtensions.ToMoney);
    public static FuncValueConverter<bool, IBrush> ToStatusBrushConverter { get; } = new(MakripExtensions.ToStatusBrush);
```

4. **Используй в AXAML без ресурсов**:

```xml
xmlns:conv="using:TastyEat.Workstation.Views.Converters"

<TextBlock Text="{Binding Total, Converter={x:Static conv:MakripExtensions.ToMoneyConverter}}" />
<Border Background="{Binding IsActive, Converter={x:Static conv:MakripExtensions.ToStatusBrushConverter}}" />
```

5. **Проверь сборку** — `x:Static` с методом вместо свойства ломает компиляцию с `AVLN2000`, а несоответствие типов входа/выхода — с ошибкой compiled binding:

```bash
dotnet build TastyEat.Workstation/TastyEat.Workstation.csproj
```

## Правила

- Вход метода — nullable (`decimal` для значений value-типов приходит не null, но ссылочные принимай `string?`, сущности — `T?`): binding легко передаёт null до инициализации; null-безопасность обязательна, исключения недопустимы.
- Многовходовые — `FuncMultiValueConverter` в статическом свойстве типа `IMultiValueConverter` + `{MultiBinding Converter={x:Static ...}}`.
- Параметризуемые (например `{Binding X, Converter={x:Static ...}, ConverterParameter=3}`) — лямбда в свойстве: `public static FuncValueConverter<double, double> MulConverter { get; } = new(v => v * 3);`.
- Русская локаль для чисел/дат — внутри метода (`ru-RU`), не надейся на `culture`.
- В `OrderCollectionView.axaml` есть забытый неиспользуемый `xmlns:conv="using:Avalonia.Data.Converters"` — при работе с файлом удали.

## Легаси для замены

`Views/Converters/StringToIconKindConverter.cs` (классический `IValueConverter` в `Window.Resources` главного окна) заменить на:

```csharp
public static MaterialIconKind ToIconKind(this string? name) =>
    Enum.TryParse<MaterialIconKind>(name, out var kind) ? kind : default;

public static FuncValueConverter<string?, MaterialIconKind> ToIconKindConverter { get; } = new(MakripExtensions.ToIconKind);
```

и в `MainWindow.axaml`: удалить `<conv:StringToIconKindConverter x:Key="..." />` из `Window.Resources`, подключить `xmlns:conv="using:TastyEat.Workstation.Views.Converters"` и писать `Kind="{Binding IconName, Converter={x:Static conv:MakripExtensions.ToIconKindConverter}}"`.
