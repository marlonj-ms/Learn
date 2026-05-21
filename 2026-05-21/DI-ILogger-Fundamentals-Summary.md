# 2026-05-21 — ILogger + DI Fundamentals Summary

## Today's Pace

Today was intentionally slow. The goal was not to write code yet, but to build the mental model for production C# logging and dependency injection.

Main thread:

```text
Console.WriteLine problem
    -> ILogger<T> as logging abstraction
    -> T as log category
    -> structured logging
    -> constructor injection
    -> DI container
    -> service lifetimes
```

## Concepts Covered

### 1. Why `Console.WriteLine` Is Not Enough

`Console.WriteLine` directly writes text to the console.

It can be useful during debugging, but in production:

- The app may not have a visible terminal window.
- The app may run as a Web API, Windows Service, Azure Function, Docker container, or Kubernetes Pod.
- Core business logic should not decide the output channel.

Production wording:

```text
Core library should not decide where diagnostic information goes.
Core library should expose behavior.
The host application decides logging, UI, files, telemetry, or monitoring.
```

Chinese + English terms:

- 控制台输出（direct console output）
- 核心类库（Core library）
- 宿主应用（host application）
- 关注点分离（separation of concerns）
- 基础设施关注点（infrastructure concern）

## 2. `ILogger<T>`

`ILogger<T>` is a .NET official logging abstraction.

Namespace:

```csharp
using Microsoft.Extensions.Logging;
```

Package / assembly area:

```text
Microsoft.Extensions.Logging.Abstractions
```

Important correction:

```text
T in ILogger<T> is not the log content type.
T is the log category type.
```

Example:

```csharp
ILogger<TemperatureSensorService> logger;
ILogger<OrderService> orderLogger;
```

Categories:

```text
TemperatureSensorService
OrderService
```

Mental model:

```text
ILogger<TemperatureSensorService>
    roughly means:
Create a logger categorized as "TemperatureSensorService".
```

It does not mean the logger can only write `TemperatureSensorService` objects.

## 3. Message Template + Arguments

Example:

```csharp
_logger.LogWarning("Temperature is high: {Reading}", 101);
```

Parts:

| Part | Role |
|---|---|
| `LogWarning` | 日志等级（log level） |
| `"Temperature is high: {Reading}"` | 消息模板（message template） |
| `101` | 日志参数 / 结构化字段值（log argument / structured field value） |
| `{Reading}` | 结构化字段（structured field） |

Difference from string interpolation:

```csharp
Console.WriteLine($"Temperature is high: {reading}");
```

This creates plain text early.

But:

```csharp
_logger.LogWarning("Temperature is high: {Reading}", reading);
```

preserves structured data:

```text
Message: Temperature is high: 101
Reading: 101
Level: Warning
Category: TemperatureSensorService
```

Key sentence:

```text
Console.WriteLine writes text.
ILogger writes a log record:
level + category + message template + structured fields.
```

## 4. Log Levels

Examples:

```csharp
_logger.LogInformation("Sensor started");
_logger.LogWarning("Temperature is high: {Reading}", reading);
_logger.LogError(ex, "Failed to record temperature");
```

| Level | Meaning |
|---|---|
| `Information` | Normal operational information. Useful for timeline reconstruction. |
| `Warning` | Suspicious condition or potential risk. The app can still continue. |
| `Error` | Operation failed. Usually needs investigation. |

`ex` in `LogError(ex, ...)` is an exception object. The logging system can preserve:

- Exception type
- Exception message
- Stack trace
- Inner exception

## 5. Constructor Injection

Example:

```csharp
public sealed class TemperatureSensorService
{
    private readonly ILogger<TemperatureSensorService> _logger;

    public TemperatureSensorService(ILogger<TemperatureSensorService> logger)
    {
        _logger = logger;
    }

    public void Record(double reading)
    {
        _logger.LogInformation("Recording temperature: {Reading}", reading);
    }
}
```

Meaning:

```text
The constructor parameter receives the dependency from outside.
The private readonly field stores it for later use.
Methods use the field.
```

Important correction from today:

```text
DI does not mean the class stops declaring dependencies.
The class should clearly declare dependencies.
DI means the class does not create concrete dependencies itself.
```

Good:

```csharp
public OrderService(IOrderRepository repository)
{
}
```

Risky / tightly coupled:

```csharp
private readonly IOrderRepository _repository = new SqlOrderRepository();
```

Terms:

- 构造函数注入（constructor injection）
- 显式依赖（explicit dependency）
- 私有只读字段（private readonly field）
- 依赖对象（dependency object）
- 依赖抽象（depend on abstraction）
- 具体实现（concrete implementation）
- 紧耦合（tight coupling）

## 6. Data Parameters vs Dependency Parameters

Old familiar pattern: passing business data.

```csharp
public void RecordReading(double celsius)
{
}
```

Meaning:

```text
Call this method with temperature data.
```

DI pattern: passing collaborator objects.

```csharp
public OrderService(IOrderRepository repository)
{
}
```

Meaning:

```text
Create this object with something that can access order storage.
```

Comparison:

```text
普通参数：传业务数据（business data）
DI 参数：传协作者对象 / 依赖对象（collaborator object / dependency）
```

Example classification:

```csharp
public ReportService(
    string reportTitle,
    int maxRows,
    ILogger<ReportService> logger,
    IReportRepository repository)
{
}
```

| Parameter | Kind |
|---|---|
| `string reportTitle` | 业务配置数据（business configuration data） |
| `int maxRows` | 业务配置数据（business configuration data） |
| `ILogger<ReportService> logger` | 依赖对象（dependency object） |
| `IReportRepository repository` | 依赖对象（dependency object） |

Future note: business configuration data is often grouped into an options class and injected using the options pattern.

## 7. DI Container

Manual DI:

```csharp
var logger = ...;
var service = new TemperatureSensorService(logger);
```

This is valid. The dependency still comes from outside.

Container DI:

```csharp
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<TemperatureSensorService>();
```

The container internally does the object assembly:

```text
Request TemperatureSensorService
    -> inspect constructor
    -> sees ILogger<TemperatureSensorService>
    -> logging system can provide ILogger<T>
    -> create logger
    -> new TemperatureSensorService(logger)
```

DI container is best understood as:

```text
object factory + dependency graph resolver
```

Terms:

- 依赖注入容器（DI container）
- 服务集合（IServiceCollection）= registration list
- 服务提供者（IServiceProvider）= object creator / resolver
- 依赖图（dependency graph）= A needs B, B needs C

## 8. Registration

Business dependency example:

```csharp
services.AddSingleton<IOrderRepository, SqlOrderRepository>();
```

Meaning:

```text
When something needs IOrderRepository,
provide SqlOrderRepository.
```

Logging dependency example:

```csharp
services.AddLogging(builder => builder.AddConsole());
```

Meaning:

```text
Register the logging infrastructure.
Register console as one logging provider.
```

Important point:

```text
You usually do not register ILogger<OrderService> one class at a time.
AddLogging registers the logging system so ILogger<T> can be created for many T categories.
```

Terms:

- 日志系统（logging system）
- 日志提供程序（logging provider）
- 开放泛型注册（open generic registration）

## 9. Service Lifetimes

### Singleton

```csharp
services.AddSingleton<OrderService>();
```

Meaning:

```text
One instance for the whole DI container.
```

Request 3 times -> 1 object.

Good for:

- Shared global services
- Expensive reusable services
- Stateless thread-safe services
- Shared concurrency limiters

### Transient

```csharp
services.AddTransient<OrderService>();
```

Meaning:

```text
New instance every time it is requested.
```

Request 3 times -> 3 objects.

Good for:

- Lightweight stateless services
- Temporary operation objects

### Scoped

```csharp
services.AddScoped<OrderService>();
```

In Web API:

```text
One instance per HTTP request.
Same request reuses it.
Different requests get different instances.
```

Example:

```text
Request A asks twice -> one instance for Request A
Request B asks once -> another instance for Request B
Total -> two instances
```

Good for:

- Current user context
- EF Core DbContext
- Request-specific state

## 10. Lifetime Decision Rule

Ask:

```text
Who should share this state?
```

Then decide:

| Sharing Need | Lifetime |
|---|---|
| Whole app shares it | Singleton |
| One HTTP request shares it | Scoped |
| Nobody shares; create fresh every time | Transient |

Examples:

| Object | Likely Lifetime | Reason |
|---|---|---|
| Global config reader | Singleton | Same for whole app |
| Current user context | Scoped | Different per request/user |
| EF Core `DbContext` | Scoped | One unit of work per request |
| Small stateless calculator | Transient or Singleton | No state |
| Global `SemaphoreSlim` concurrency limiter | Singleton | Must share one counter globally |

Important correction:

```text
Current user information should not be Singleton.
Singleton would risk cross-request state leak.
```

## Memory Diagrams

### ILogger<T>

```text
ILogger<TemperatureSensorService>
        |
        +-- T = category type
        +-- category name = TemperatureSensorService
        +-- not the log content type
```

### Log Record

```text
_logger.LogWarning("Temperature is high: {Reading}", reading)

creates a log record:
    Level: Warning
    Category: TemperatureSensorService
    Template: Temperature is high: {Reading}
    Field: Reading = 101
    Destination: decided by provider/config
```

### Constructor Injection

```text
constructor parameter logger
        |
        v
_logger = logger
        |
        v
methods reuse _logger
```

### DI Container

```text
You ask for TemperatureSensorService
        |
        v
Container checks constructor
        |
        v
Needs ILogger<TemperatureSensorService>
        |
        v
Logging system provides logger
        |
        v
Container creates TemperatureSensorService(logger)
```

### Lifetimes

```text
Singleton:
container -> one object reused everywhere

Scoped:
request A -> one object reused inside request A
request B -> one object reused inside request B

Transient:
each request for service -> new object
```

## Learner Corrections Logged Today

1. `ILogger<T>`: `T` is not `int/string/object` content type. It is the log category type.
2. `TemperatureSensorService` in `ILogger<TemperatureSensorService>` is used as category metadata, not as business data.
3. `readonly` means the field can be assigned during declaration or constructor, then not replaced.
4. DI does not mean the class should avoid declaring dependencies. It should declare them explicitly.
5. DI means dependencies are provided from outside instead of being created inside the class.
6. `Singleton` is dangerous for current-user state because users/requests would share state incorrectly.
7. `SemaphoreSlim` for global concurrency limiting should be Singleton because the counter must be shared.

## Final Understanding Statement

The final corrected DI sentence:

```text
DI 就是：类需要的依赖由外部 / DI 容器提供，而不是类自己创建具体依赖。
```

English:

```text
Dependency Injection means dependencies are provided from outside,
usually by a DI container, instead of being created inside the class.
```

## Next Session Starting Point

Do not add more theory first. Start with a tiny hands-on console host.

Goal:

```text
Use DI to create TemperatureSensorService,
and use ILogger to output one log line.
```

Planned steps:

```text
1. Create a small console host project.
2. Add Microsoft.Extensions.Hosting / logging packages if needed.
3. Register logging with AddConsole.
4. Register TemperatureSensorService.
5. Resolve the service from DI.
6. Call a method and observe ILogger output.
```

Keep the code small and explain each line before expanding.

## Review Questions For Next Day

### ILogger<T>

1. In `ILogger<TemperatureSensorService>`, what does `TemperatureSensorService` represent?
2. Does `ILogger<OrderService>` mean the logger can only log `OrderService` objects?
3. What decides where logs go: `T`, message template, log level, or provider/configuration?
4. In `_logger.LogWarning("Temperature is high: {Reading}", 101)`, what is the message template?
5. In that same line, what is the structured field name?
6. Why is structured logging more useful than `Console.WriteLine($"...")`?

### Log Levels

7. When should `LogInformation` be used?
8. When should `LogWarning` be used?
9. When should `LogError(ex, ...)` be used?
10. What extra information can `ex` give the logging system?

### Dependency Injection

11. What is the difference between declaring a dependency and creating a dependency?
12. Why is `private readonly ConsoleLogger _logger = new ConsoleLogger();` tightly coupled?
13. Why is `public TemperatureSensorService(ILogger<TemperatureSensorService> logger)` more flexible?
14. What does `_logger = logger;` do?
15. Why is `_logger` usually private and readonly?
16. What is manual DI?
17. What extra convenience does a DI container provide over manual DI?

### Parameters

18. What is the difference between business data and dependency objects?
19. In `ReportService(string title, ILogger<ReportService> logger)`, which parameter is business data?
20. In that same constructor, which parameter is a dependency object?

### Registration

21. What does `services.AddSingleton<IOrderRepository, SqlOrderRepository>()` mean?
22. Why don't we usually write `services.AddSingleton<ILogger<OrderService>, ...>()` manually?
23. What does `AddLogging(builder => builder.AddConsole())` register?

### Lifetimes

24. What does Singleton mean?
25. What does Transient mean?
26. What does Scoped mean in a Web API?
27. If a Singleton service is requested three times, how many instances are created?
28. If a Transient service is requested three times, how many instances are created?
29. If a Scoped service is requested twice in Request A and once in Request B, how many instances are created?
30. Why should current-user state not be Singleton?
31. Why should a global `SemaphoreSlim` concurrency limiter usually be Singleton?
