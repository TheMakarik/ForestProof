---
name: options-creating
description: Use when adding or changing configurable values in the C# backend (ForestProof.Backend). Covers IOptions<T> instead of constants, the AddOptions<T> extension on IHostApplicationBuilder, required properties, and appsettings.json. Trigger on "options", "настройки", "конфиг", "appsettings", "IOptions", "константы".
---

# Конфигурация через IOptions<T>

Любое настраиваемое значение — это **не константа**. Не создавай `const`/`static readonly` для того, что может отличаться между окружениями (строки подключения, адреса, таймауты, лимиты, ключи, названия секций и т.д.). Такие значения описываются отдельным классом опций и живут в `appsettings.json`.

## Правила

- Настройки — это `IOptions<T>`, а не `IConfiguration` напрямую и не константы. Потребитель получает `IOptions<T>` (или `IOptionsSnapshot<T>`/`IOptionsMonitor<T>`, если нужны изменения на лету) через DI.
- Класс опций — отдельный публичный класс, имя файла совпадает с именем класса.
- **Все свойства класса опций обязательные** — `required`, **без значений по умолчанию**.
- Значения хранятся в `appsettings.json` (переопределяются в `appsettings.{Environment}.json`).
- Регистрация — методом расширения `AddOptions<T>()` для `IHostApplicationBuilder` (реализация ниже). Он биндит секцию по имени типа, валидирует и падает на старте, если значения нет или оно невалидно.
- Имя секции в `appsettings.json` должно совпадать с именем класса опций (либо передавай `sectionName` явно).

## Реализация расширения

Файл `Extensions/OptionsExtensions.cs` в `ForestProof.Backend`:

```csharp
namespace ForestProof.Backend.Extensions;

public static class OptionsExtensions
{
    public static IHostApplicationBuilder AddOptions<TOptions>(
        this IHostApplicationBuilder builder,
        string? sectionName = null)
        where TOptions : class
    {
        builder.Services
            .AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName ?? typeof(TOptions).Name))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }
}
```

## Пример

Класс опций `Options/DatabaseOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ForestProof.Backend.Options;

public sealed class DatabaseOptions
{
    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required int CommandTimeoutSeconds { get; init; }
}
```

`appsettings.json`:

```json
{
  "DatabaseOptions": {
    "ConnectionString": "Host=localhost;Database=forestproof",
    "CommandTimeoutSeconds": 30
  }
}
```

Регистрация и использование в `Program.cs`:

```csharp
using ForestProof.Backend.Extensions;
using ForestProof.Backend.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddOptions<DatabaseOptions>();

var app = builder.Build();

app.MapGet("/", (IOptions<DatabaseOptions> options) => options.Value.CommandTimeoutSeconds);

app.Run();
```

## Чек-лист

- [ ] Для настраиваемого значения создан класс опций, а не константа.
- [ ] Все свойства `required`, без значений по умолчанию.
- [ ] Значения добавлены в `appsettings.json` (и, при необходимости, в окружение).
- [ ] Регистрация выполнена через `builder.AddOptions<T>()`.
- [ ] Потребитель получает `IOptions<T>` из DI, а не `IConfiguration`.
