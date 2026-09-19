---
name: csharp-testing
description: Use when writing or editing tests in ForestProof.Testing. Covers xUnit, FakeItEasy, FluentAssertions, AutoFixture, test class structure, naming and assertions. Trigger on any test file, ".cs tests", "тесты", "xUnit", "NUnit", "FluentAssertions", "FakeItEasy", "assert", "тест-кейс".
---

# Тестирование (ForestProof.Testing)

## Инструменты

- **xUnit** — тестовый фреймворк (`[Fact]`, `[Theory]` + `[InlineData]`).
- **FakeItEasy** — изоляция зависимостей (`A.Fake`, `A.Dummy`, `A.CallTo`).
- **FluentAssertions** — утверждения (`.Should()`).
- **AutoFixture** — генерация тестовых данных (`fixture.Create<T>()`).

Пакеты уже подключены в `ForestProof.Testing.csproj`; не добавляй NUnit и MSTest.

## Глобальные using

Общие namespace подключаются в `GlobalUsings.cs`. В тестовом файле оставляй только специфичные `using`.

## Структура тестового класса

- Наследуй `IDisposable`, если нужна очистка ресурсов.
- Поля зависимостей — вверху класса.
- Общие фейки инициализируются в конструкторе.
- Тестируемый сервис называй **`systemUnderTests`**.
- Один публичный тип на файл, имя файла = имя класса тестов (как в остальном коде).

## Dummy vs Fake

- **`A.Dummy<T>()`** — зависимость нужна конструктору, но не влияет на поведение.
- **`A.Fake<T>()`** — зависимость, чьё поведение настраивается.

```csharp
// Dummy — без настройки
var logger = A.Dummy<ILogger<CarbonCalculator>>();

// Fake — с настройкой
var options = A.Fake<IOptions<CalculationOptions>>();
A.CallTo(() => options.Value).Returns(new CalculationOptions { ... });
```

Для повторяющихся фейков используй общие хелперы в `Infrastructure` (например, `TestCalculationOptions`).

## Структура теста

Разделяй тест на блоки `// Arrange`, `// Act`, `// Assert`.

## Именование

`Действие_Условие_ОжидаемыйРезультат`.

```csharp
[Fact]
public void AggregateCarbonStock_WhenManualExample_ReturnsExpectedStock()
{
    // Arrange
    var systemUnderTests = new CarbonCalculator(_options);
    PixelSample[] samples = [new() { Biomass = 100, AreaHectares = 100 }];

    // Act
    var stock = systemUnderTests.AggregateCarbonStock(samples);

    // Assert
    stock.TotalCarbon.Should().BeApproximately(4700, 1e-9);
}
```

## Утверждения

- FluentAssertions: `.Should().Be(...)`, `.Should().BeApproximately(...)`, `.Should().BeNull()`, `.Should().Throw<T>()`.
- Избегай нескольких ассертов без веской причины.
- Для чисел с плавающей точкой всегда указывай точность: `.BeApproximately(expected, 1e-9)`.

## Параметризация

Одинаковые кейсы с разными данными оформляй через `[Theory]` + `[InlineData]`, а не копированием тестов.

## Правила

- Тестируй поведение и контракт, а не реализацию.
- Тесты детерминированы: без сети, времени и случайных значений без seed.
- Используй FakeItEasy вместо самописных моков.
- Каждый тест проверяет одну причину падения.
- Для формул обязательно закрепляй ручной пример из ТЗ отдельным тестом.
- Пути к файлам формируй через `Path.Join`, а не `Path.Combine` (нестабилен на Unix).
