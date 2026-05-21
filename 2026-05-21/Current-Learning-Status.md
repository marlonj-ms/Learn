# Current C# Learning Status — May 21, 2026

## Today's Topic

Paused after a slow conceptual session on:

```text
ILogger<T> + Dependency Injection + Service Lifetimes
```

No code was authored today. This was intentionally a mental-model session.

## Concepts Understood

- `Console.WriteLine` directly writes text to a terminal and is not suitable for reusable Core library diagnostic output.
- `ILogger<T>` is a .NET logging abstraction from `Microsoft.Extensions.Logging`.
- In `ILogger<T>`, `T` is the log category type, not the log message/content type.
- `ILogger<TemperatureSensorService>` means logs are categorized as coming from `TemperatureSensorService`.
- Message templates plus arguments define log content:

```csharp
_logger.LogWarning("Temperature is high: {Reading}", reading);
```

- `{Reading}` is a structured logging field, not just plain string interpolation.
- `LogInformation`, `LogWarning`, and `LogError` represent different log levels.
- `LogError(ex, ...)` records exception details such as type, message, stack trace, and inner exception.
- Constructor injection stores an externally provided dependency in a private readonly field:

```csharp
private readonly ILogger<TemperatureSensorService> _logger;

public TemperatureSensorService(ILogger<TemperatureSensorService> logger)
{
    _logger = logger;
}
```

- DI means dependencies are provided from outside / by a DI container, not created inside the class.
- A class should declare dependencies explicitly, but avoid creating concrete implementations internally.
- Constructor parameters can be business data/configuration or dependency objects.
- `IOrderRepository` and `ILogger<T>` are dependency objects; `string title` and `int maxRows` are configuration/business data.
- `AddLogging` registers the logging infrastructure; `AddConsole` registers the console provider.
- `ILogger<T>` is generally provided by the logging system, not manually registered per class.

## Service Lifetimes Learned

```text
Singleton = one shared instance for the whole container/application
Scoped = one instance per scope; in Web API usually one per HTTP request
Transient = new instance every time requested
```

Key examples:

- Current user context -> Scoped, not Singleton.
- Global `SemaphoreSlim` concurrency limiter -> Singleton.
- Lightweight stateless operation object -> often Transient.

## Important Corrections

- `ILogger<T>`'s `T` is not `int`, `string`, or object content type. It is category metadata.
- DI does not mean the class avoids declaring dependencies. It should declare dependencies clearly.
- DI means the class avoids creating concrete dependencies itself.
- Current-user state should not be Singleton because it can leak across requests/users.

## Best Learner Summary From Today

Corrected final statement:

```text
DI 就是：类需要的依赖由外部 / DI 容器提供，而不是类自己创建具体依赖。
```

## Next Session Recommendation

Start with hands-on code, not more theory.

Build a tiny console host that:

```text
1. Registers logging.
2. Registers TemperatureSensorService.
3. Resolves TemperatureSensorService from DI.
4. Calls a method.
5. Shows ILogger output in the console.
```

Suggested next prompt:

```text
继续今天的 DI 练习，从最小 console host 开始
```

## Files Created Today

- `2026-05-21/DI-ILogger-Fundamentals-Summary.md`
- `2026-05-21/Current-Learning-Status.md`
