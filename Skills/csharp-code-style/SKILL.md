---
name: csharp-code-style
description: Use when writing or editing any C# code in the ForestProof backend (ForestProof.Backend, ForestProof.Testing). Covers var, null checks, braces, naming, modifiers, async, comments, file structure and collections/LINQ. Trigger on any C# file, ".cs", "стиль", "code style", "рефакторинг".
---

# C# code style (ForestProof backend)

Общий принцип: пиши современный, лаконичный и читаемый C#. При изменении существующего кода в первую очередь придерживайся стиля файла, а не догматично следуй правилам ниже.

## Язык и платформа

- Целевая платформа — последняя стабильная .NET (сейчас `net10.0`).
- Используй возможности современного C#: file-scoped namespaces, primary constructors, pattern matching, switch expressions, null-conditional операторы, collection expressions, target-typed `new`.

## `var`

**По умолчанию используй `var`** везде, где тип переменной очевиден из правой части выражения.

```csharp
// Хорошо
var user = await storage.LoadAsync();
var name = user.Name;
var sum = Calculate(a, b);
```

**Исключение — collection expressions.** Для них указывай явный тип коллекции, чтобы читатель сразу видел её вид.

**Исключение — адаптация под файл.** Если класс полностью написан без `var` (явные типы везде), при изменении такого класса **поддерживай его стиль**. Не внедряй `var` насильно и не переписывай существующие строки только ради `var`.

## Проверка на `null`

Используй `is null` / `is not null` вместо `== null` / `!= null`.

```csharp
// Хорошо
if (value is null) return;
if (items is not null)
    Process(items);

// Плохо
if (value == null) return;
if (items != null)
    Process(items);
```

## Фигурные скобки и вложенность

- Если тело `if`/`else`/`for`/`while`/`foreach` состоит из одной строки, **не оборачивай его в фигурные скобки**.

  ```csharp
  if (count > 0)
      Process(count);

  foreach (var item in items)
      writer.Write(item);
  ```

- Избегай лишней вложенности: используй ранний `return`, `continue` и `break`, когда это упрощает чтение.

  ```csharp
  // Хорошо
  if (value is null)
      return default;

  return value.Transform();

  // Плохо
  if (value is not null)
  {
      return value.Transform();
  }
  else
  {
      return default;
  }
  ```

## Именование

- `PascalCase` для классов, интерфейсов, методов, свойств, публичных полей, enum-ов и namespace-ов.
- `camelCase` для локальных переменных, параметров и приватных полей.
- `_camelCase` для приватных полей класса.
- Интерфейсы начинаются с `I`.
- Асинхронные методы заканчиваются на `Async`.
- **Не используй сокращения**: `directory` вместо `dir`, `options` вместо `opts`, `buffer` вместо `buf`, `configuration` вместо `config`. Допускаются только общепринятые аббревиатуры (`db`, `id`, `ui` там, где это стандарт).

## Модификаторы

- Явно указывай `private` для членов класса.
- Указывай `readonly` для полей, которые не изменяются после инициализации.
- Предпочитай `sealed` классам, которые не проектируются для наследования.

## Конструкторы

- **По умолчанию используй primary constructors** (конструкторы первого уровня) для сервисов и других классов с зависимостями.
- Не пиши вручную конструктор, который только присваивает параметры полям. Объявляй параметры в заголовке класса.
- Если значение зависимости нужно закешировать, инициализируй `private readonly` поле прямо в теле класса:

  ```csharp
  public sealed class CarbonCalculator(IOptions<CalculationOptions> options) : ICarbonCalculator
  {
      private readonly CalculationOptions _options = options.Value;
  }
  ```

- Параметры primary constructor документируются тегом `<param>` в XML-комментарии класса.
- Обычный конструктор оставляй только при необходимости: валидация аргументов, вычисления, несколько перегрузок, инициализация базового класса.

## Пути к файлам

- **Для склейки путей используй `Path.Join`, а не `Path.Combine`.** `Path.Combine` ведёт себя нестабильно на Unix (по-разному обрабатывает абсолютные сегменты и завершающие разделители).
- Разделитель пути не хардкодь: используй `Path.Join`/`Path.DirectorySeparatorChar`.

  ```csharp
  // Хорошо
  var path = Path.Join(directory, fileName);

  // Плохо — нестабильно на Unix
  var path = Path.Combine(directory, fileName);
  ```

## Коллекции: тип в сигнатурах

Выбирай тип параметра/возврата по тому, что реально нужно потребителю:

- **Только чтение** (перебрать, посчитать, прочитать по индексу) — `IReadOnlyCollection<T>`, а если нужен доступ по индексу — `IReadOnlyList<T>`.
- **Только LINQ-цепочка** — `IEnumerable<T>`, но только если перечисление гарантированно одноразовое.
- **Возможно повторное перечисление** (несколько LINQ-операторов, `Count()`/`Any()` + `foreach`, повторный `foreach`) — `ICollection<T>`/`IReadOnlyCollection<T>` (или один раз материализуй), чтобы не было двойного перечисления.
- **Изменение** (`Add`/`Remove`/`Clear`) — `ICollection<T>`/`IList<T>`/`IDictionary<TKey,TValue>` либо конкретный тип, если он нужен снаружи.

Не отдавай `List<T>`/`Dictionary<TKey,TValue>`, если потребителю достаточно интерфейса.

```csharp
// Хорошо — коллекция только читается
public IReadOnlyCollection<ThemeColorEntry> Colors { get; }

// Хорошо — одноразовое LINQ-перечисление
public IEnumerable<ThemeMetadata> Themes => _cache.Values;

// Хорошо — перечисление возможно несколько раз
public IReadOnlyCollection<ThemeMetadata> Themes { get; }

// Плохо — наружу отдаётся конкретный List без необходимости
public List<ThemeColorEntry> Colors { get; }
```

## Двойное перечисление

Если по `IEnumerable<T>` идёт больше одного прохода (несколько LINQ, `Any()` + `foreach`, `Count()` + доступ), материализуй один раз или прими `IReadOnlyCollection<T>`.

```csharp
// Плохо — source перечисляется дважды
if (source.Any())
    foreach (var item in source)
        Process(item);

// Хорошо
var items = source as IReadOnlyCollection<Item> ?? source.ToList();
if (items.Count > 0)
    foreach (var item in items)
        Process(item);
```

## Пустые коллекции и collection expressions

Для пустых коллекций используй `[]` вместо `new List<T>()` и `Array.Empty<T>()`.

Collection expressions (`[...]`) — исключение из правила `var`: указывай явный тип, чтобы читатель сразу видел вид коллекции.

```csharp
// Хорошо
int[] numbers = [1, 2, 3];
List<string> items = [];
Dictionary<int, string> map = [];
Span<char> buffer = ['a', 'b', 'c'];

// Плохо
var numbers = [1, 2, 3];
var items = new List<string>();
var map = new Dictionary<int, string>();
```

## Потокобезопасные коллекции

Обычные `List<T>`/`Dictionary<TKey,TValue>` не потокобезопасны. Если коллекция читается/пишется из нескольких потоков:

- `ConcurrentDictionary<TKey,TValue>`, `ConcurrentQueue<T>`, `ConcurrentBag<T>`, `ConcurrentStack<T>` — параллельный доступ без внешней блокировки.
- `ImmutableArray<T>`/`ImmutableDictionary<TKey,TValue>` — когда нужно отдавать неизменяемый снимок.
- `FrozenDictionary<TKey,TValue>`/`FrozenSet<T>` — справочники, созданные один раз и только читаемые (быстрее обычных).
- `BlockingCollection<T>`/`Channel<T>` — producer/consumer.

Не оборачивай обычную коллекцию в `lock`, если подходит concurrent/immutable.

```csharp
// Хорошо — параллельные записи
private readonly ConcurrentDictionary<string, ThemeMetadata> _themes = new(StringComparer.Ordinal);

// Плохо — lock поверх обычного словаря, когда есть ConcurrentDictionary
private readonly Dictionary<string, ThemeMetadata> _themes = [];
private readonly object _gate = new();
```

## LINQ

- Используй LINQ для декларативных преобразований, но не злоупотребляй: если запрос становится нечитаемым, разбей его или используй цикл.
- Материализуй (`ToList`/`ToArray`) осознанно: это один проход, но и аллокация.
- Не смешивай LINQ с побочными эффектами в `Select`.

## Асинхронность

- Всегда используй `async`/`await`, избегай `.Result`, `.Wait()` и синхронных блокировок в асинхронном коде.
- Не лови исключения без причины; если ловишь — обрабатывай, логируй или добавь комментарий, почему подавляешь.

## Комментарии и XML-документация

- Комментарии должны объяснять *почему*, а не *что*.
- Избегай закомментированного кода.
- **Обязательно пиши XML-документацию** (`/// <summary>`) на все публичные типы и их публичные члены в **домене и сервисах**.
- Документируй каждый публичный тип, свойство, метод, а также параметры (`<param>`) и возвращаемое значение (`<returns>`), если они есть.
- XML-документация пишется на русском языке; для величин указывай единицы измерения (т C, т CO₂-экв., га, т C/га и т.д.).

## Структура файлов

- **Категорически запрещается создавать подклассы (вложенные классы)** внутри других классов.
- **В одном файле — ровно один публичный тип** (class, record, struct, enum, interface). Без исключений: никакие вспомогательные типы, enum-ы или record-ы не размещаются рядом с другим типом.
- **Имя файла строго соответствует имени публичного типа**, который в нём содержится.
- **Enum-ы выносятся в отдельные файлы**; для них используется отдельная папка (`Enums`).
- Типы группируются по папкам, и папка соответствует namespace (например, `Domain/Enums/UnitStatus.cs` → `...Domain.Enums`).

```csharp
// Хорошо — файл UserService.cs
public sealed class UserService
{
    // ...
}

// Плохо — файл UserService.cs с двумя классами
public sealed class UserService
{
    // ...
}

public sealed class UserValidator
{
    // ...
}

// Плохо — вложенный класс
public sealed class UserService
{
    private sealed class InnerHelper
    {
        // ...
    }
}
```

## Адаптация под файл

- Главное — единообразие внутри файла.
- Правила про `{}` и лаконичность применяются в разумных пределах: если класс использует исключительно блочный стиль, не ломай его ради единообразия.
- Не превращай правку в рефакторинг ради рефакторинга.

## Чек-лист

- [ ] `var` там, где тип очевиден; явный тип у collection expressions.
- [ ] `is null` / `is not null`, а не `== null` / `!= null`.
- [ ] Без лишних `{}` для однострочных тел и без лишней вложенности.
- [ ] Имена без сокращений; `Async` у асинхронных методов.
- [ ] Явный `private`, `readonly`, `sealed`.
- [ ] Primary constructors там, где конструктор только присваивает зависимости.
- [ ] Пути через `Path.Join`, а не `Path.Combine`.
- [ ] Тип параметра/возврата минимально достаточный: `IReadOnlyCollection`/`IReadOnlyList` для чтения, `ICollection`/`IList`/`IDictionary` для изменения.
- [ ] Нет двойного перечисления `IEnumerable<T>`.
- [ ] Пустые коллекции — `[]`, не `new List<T>()`/`Array.Empty<T>()`.
- [ ] Для многопоточного доступа — concurrent/immutable/frozen коллекция, а не `lock` поверх `List<T>`.
- [ ] Ровно один публичный тип на файл, без вложенных классов, имя файла = имя типа.
- [ ] Enum-ы в отдельных файлах, в папке `Enums`.
- [ ] XML-документация на публичные типы и члены домена и сервисов.
- [ ] Без закомментированного кода.
