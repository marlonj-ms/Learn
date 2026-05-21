# 2026-05-21 — DI + ILogger Hands-On + Lambda 深拆解 Summary

## 今天的主线

```text
晨间：DI + ILogger + Service Lifetime 概念（已记录在 DI-ILogger-Fundamentals-Summary.md）
     ↓
午后：最小可运行项目 DI-Logging-MinHost（ServiceCollection + AddLogging + AddTransient + GetRequiredService）
     ↓
深入：lambda + 泛型委托类型 + 扩展方法 三层语法烧脑点逐字拆解
```

今天的核心收获：把概念变成了真实能跑的代码，并且把 `services.AddLogging(builder => {...})` 这一行的每一个字符都搞透了。

## 完成的代码

- `2026-05-21/DI-Logging-MinHost/DI-Logging-MinHost.csproj` — 最小 console 项目
  - TFM: `net9.0`
  - 包：`Microsoft.Extensions.DependencyInjection` 10.0.8、`Microsoft.Extensions.Logging` 10.0.8、`Microsoft.Extensions.Logging.Console` 10.0.8
- `2026-05-21/DI-Logging-MinHost/TemperatureSensorService.cs` — 业务服务，通过构造函数注入 `ILogger<TemperatureSensorService>`
- `2026-05-21/DI-Logging-MinHost/Program.cs` — DI 容器最小 host，6 步流程注释清晰
- 实际输出：

```text
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 72.5
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 101.3
warn: DI_Logging_MinHost.TemperatureSensorService[0]
      Temperature is too high: 101.3
```

## 概念清单（按主题分类）

### A. 业务数据 vs 依赖项

| 概念 | 中文 / English | 一句话理解 |
|---|---|---|
| 业务数据 | business data | 这次调用要处理的内容（如 `Order order`、`double celsius`） |
| 依赖项 | dependency | 类长期依赖的协作者（如 `ILogger<T>`、`IOrderRepository`） |
| 协作者 | collaborator | 帮我完成工作的对象 |
| 业务实体 | business entity | `Order` 这种领域对象 |
| 仓储抽象 | repository abstraction | `IOrderRepository` 这种数据访问能力 |

放置位置默认规则：

```text
构造函数：放长期依赖（long-lived dependencies）
方法参数：放本次输入（per-call input data）
```

### B. 依赖注入（DI）

| 概念 | 中文 / English | 一句话 |
|---|---|---|
| 依赖注入 | dependency injection (DI) | 依赖由外部提供，不由类自己创建 |
| 构造函数注入 | constructor injection | 通过构造函数参数接收依赖项 |
| 显式依赖 | explicit dependency | 类清晰声明自己需要什么 |
| 依赖抽象 | depend on abstraction | 依赖 `ILogger<T>` 而非 `FileLogger` |
| 紧耦合 | tight coupling | 类自己 `new` 具体实现 → 难替换、难测试 |
| 私有只读字段 | private readonly field | `_logger` 这种用来长期保存依赖的字段 |

关键句：

```text
DI 就是：类需要的依赖由外部 / DI 容器提供，而不是类自己创建具体依赖。
类应该清晰声明依赖，但不应该自己创建具体实现。
```

### C. `ILogger<T>` 与结构化日志

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 日志类别 | log category | `ILogger<T>` 里的 `T` 决定 category，不是消息类型 |
| 消息模板 | message template | `"Recording {Reading}"` 这种带占位符的字符串 |
| 结构化字段 | structured field | `{Reading}` 是字段名，不是 C# 变量名 |
| 位置匹配 | positional matching | 模板占位符按位置和参数配对，不按名字 |
| 日志等级 | log level | Information / Warning / Error |
| `LogError(ex, ...)` | — | 第一个参数传 exception，可保留 type/message/stack trace |

字符串插值 vs 结构化日志的核心差别：

```csharp
// 字符串插值：早早变成普通文本，结构化字段丢失
_logger.LogInformation($"Reading: {reading}");

// 结构化日志：保留 Reading 字段名和值，便于查询
_logger.LogInformation("Reading: {Reading}", reading);
```

### D. Service Lifetime

| Lifetime | 中文 | 创建时机 | 何时释放 |
|---|---|---|---|
| Transient | 瞬时 | 每次 resolve 都创建新实例 | 由所属 scope/container dispose 时释放 |
| Scoped | 作用域 | 每个 scope 内共享一个实例 | scope 结束时 dispose（在 ASP.NET Core = 一个 HTTP request 结束） |
| Singleton | 单例 | 整个容器只有一个实例 | 应用关闭时 dispose |

判断规则：

```text
请求级状态（current request state） → Scoped
全局共享协调器/限流器/缓存（app-wide shared）→ Singleton
轻量无状态工作对象（stateless）→ Transient
```

关键提醒：

- `Scoped` ≠ per user；ASP.NET Core 里默认 = per HTTP request
- 用户身份跨请求存在是靠 cookie/JWT/session，不是靠 scoped object 一直活着
- `SemaphoreSlim(10)` 这种全局并发限制器若做成 Scoped，限制会失效
- `CurrentUserContext` 这种请求级状态若做成 Singleton 会跨用户串数据，是严重安全风险

### E. ServiceCollection / ServiceProvider 工作流

```text
ServiceCollection (清单)
    │
    │ AddLogging(...) + AddTransient<T>()      ← 注册阶段（registration phase）
    ▼
BuildServiceProvider()
    │
    ▼
ServiceProvider (容器)
    │
    │ GetRequiredService<T>()                 ← 解析阶段（resolution phase）
    ▼
T 的实例（容器读取构造函数自动注入依赖）
```

关键认知：

- `AddTransient<TemperatureSensorService>()` 不会立刻创建对象，只是注册服务描述（service descriptor）
- 真正创建发生在 `GetRequiredService<T>()` 或某个上层服务把它当依赖时
- 容器读取构造函数，自动把构造函数参数注入进去

### F. Lambda 深拆解（今天踩的大坑）

| 概念 | 中文 / English | 一句话 |
|---|---|---|
| Lambda | lambda expression | 没有名字的方法（anonymous method） |
| Lambda 参数 | lambda parameter | `n =>` 里的 `n`，由 lambda 自己定义 |
| 类型推断 | type inference | `n` 的类型由委托签名推断 |
| 表达式 lambda | expression lambda | `x => x * 2`，单表达式，无 `{}`，无 `return` |
| 语句 lambda | statement lambda | `x => { ...; return ...; }` |
| `=>` 的两种含义 | — | 值位置 = lambda，成员声明位置 = expression-bodied member |
| `Action` | — | 无参、无返回值的委托 |
| `Action<T>` | — | 接受一个 T 参数、无返回值的委托 |
| `Func<TIn, TOut>` | — | 接受参数并返回值的委托 |
| 配置回调 | configuration callback | 框架内部 new 出 builder 后调用你的 lambda |

把 `services.AddLogging(builder => { builder.AddConsole(); });` 彻底拆开：

```text
1. services = IServiceCollection 实例
2. AddLogging 是扩展方法（extension method），接受 (this IServiceCollection, Action<ILoggingBuilder>)
3. builder => { ... } 是一个 lambda，类型为 Action<ILoggingBuilder>
4. builder 不是“未定义”——它是 lambda 参数，类型由 Action<ILoggingBuilder> 推断为 ILoggingBuilder
5. lambda 没 return 因为 Action<T> 的返回值是 void
6. lambda 不会自己执行；AddLogging 内部 new 出 ILoggingBuilder 后调用 configure(builder)
7. 你的 builder.AddConsole() 在 AddLogging 内部那一刻才真正运行
```

### G. `<>` vs `()` 终极区分

| 符号 | 用途 | 例子 |
|---|---|---|
| `<>` | 泛型类型实参（generic type argument），给类型加类型参数 | `List<int>`、`Action<int>`、`ILogger<OrderService>` |
| `()` | 调用括号，给方法或委托传值参数 | `WriteLine(x)`、`log(42)`、`AddLogging(b => ...)` |

```csharp
Action<int> log = x => Console.WriteLine(x);
//     <>  类型实参                ()  方法调用
log(42);
// () 委托调用
```

### H. 扩展方法（extension method）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 扩展方法 | extension method | 一个静态方法，第一个参数前加 `this`，使得目标类型可以用 `.` 调用 |
| `this` 关键字位置 | — | 写在“定义里”的第一个参数前；调用时不写 |
| 编译期改写 | compile-time rewrite | `"hello".LengthSquared()` ≡ `StringExtensions.LengthSquared("hello")` |

`AddLogging` 之所以可以写 `services.AddLogging(...)`，是因为定义里有 `this IServiceCollection services`。

## 关键心智模型

### 1. DI 三层职责

```text
服务类 → 在构造函数里声明依赖（"我需要什么"）
注册   → 告诉容器它能创建什么，及生命周期（"以后能 new，按 Transient/Scoped/Singleton 处理"）
容器   → 读取构造函数，自动把依赖传进来（"我帮你 new，并把 ILogger<T> 喂给你"）
```

### 2. ILogger<T> 不是"日志写什么"，而是"日志归类给谁"

```text
ILogger<OrderService>
    决定 category = OrderService
LogInformation("Order {OrderId} submitted", orderId)
    决定 message + structured fields
```

### 3. Service Lifetime 决策树

```text
这个对象代表“当前请求/操作的状态”？
    是 → Scoped
    否 → 它需要在整个应用中共享/协调？
        是 → Singleton
        否 → 它是轻量无状态？
            是 → Transient
```

### 4. lambda 三件套：参数 / 体 / 执行者

```text
n => Console.WriteLine(n)
↑     ↑
参数  方法体（这里是单表达式）

谁调用：
- Action<int> log = n => ...; log(5);        → 你自己调用
- services.AddLogging(builder => ...);       → 框架内部调用
```

### 5. `services.AddLogging(builder => { ... })` 隐含的执行流程

```text
1. 你创建 lambda L = builder => { ... }
2. 把 L 当参数传给 AddLogging
3. AddLogging 内部：var b = new LoggingBuilder(services);
4. AddLogging 内部：L(b);  ← 你的 lambda 这一刻被调用
5. AddLogging 内部：完成注册并返回 services
```

## 关键代码模板

### 业务服务（依赖注入风格）

```csharp
using Microsoft.Extensions.Logging;

namespace DI_Logging_MinHost;

public sealed class TemperatureSensorService
{
    private readonly ILogger<TemperatureSensorService> _logger;

    public TemperatureSensorService(ILogger<TemperatureSensorService> logger)
    {
        _logger = logger;
    }

    public void Record(double reading)
    {
        _logger.LogInformation("The temperature sensor reported {Reading}", reading);

        if (reading > 100)
        {
            _logger.LogWarning("Temperature is too high: {Reading}", reading);
        }
    }
}
```

### 最小 console host（6 步流程）

```csharp
using DI_Logging_MinHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1. Build the registration list.
var services = new ServiceCollection();

// 2. Register the logging infrastructure (console output).
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// 3. Register our own business service.
services.AddTransient<TemperatureSensorService>();

// 4. Build the DI container.
using var provider = services.BuildServiceProvider();

// 5. Resolve the service from the container.
var sensor = provider.GetRequiredService<TemperatureSensorService>();

// 6. Call business methods.
sensor.Record(72.5);
sensor.Record(101.3);
```

## 今天反复纠正的点

- `ILogger<T>` 的 `T` 不是消息类型，是日志类别
- DI 不是“类不声明依赖”，而是“类不创建具体依赖”
- 类内部写 `private readonly FileLogger _logger = new FileLogger();` 不是 DI，是“自己创建依赖”
- `var services = new ServiceCollection();` 不能在同一段顶层代码里重复声明（笔记应写成注释）
- 字符串插值 `$"...{reading}"` 会丢失结构化字段；结构化日志 `"...{Reading}", reading` 才保留
- 结构化日志按位置匹配，不按变量名匹配
- `Scoped` ≠ per user，默认是 per HTTP request
- `Action<int>` 是“类型”，不是“方法调用”；类型参数用 `<>`，调用参数用 `()`
- `builder` 不是缺乏定义，它是 lambda 参数，类型由委托签名推断
- lambda 没有 `return` 不是错误，是因为 `Action<T>` 返回 `void`
- `services.AddLogging(...)` 不写 `this`，因为 `this` 是写在 `AddLogging` 定义里的扩展方法标记

## 明日复习题（quiz me 用）

### DI / Lifetime

1. 业务数据和依赖项的关键区别是什么？
2. 为什么类内部 `new FileLogger()` 不是依赖注入？
3. `Transient`、`Scoped`、`Singleton` 三者的创建时机分别是什么？
4. ASP.NET Core 里 `Scoped` 的默认 scope 是什么？
5. 为什么 `CurrentUserContext` 不能用 `Singleton`？
6. 为什么 `SemaphoreSlim(10)` 全局限流器应该是 `Singleton` 而不是 `Scoped`？
7. `services.AddTransient<X>()` 这行代码会立刻创建 `X` 实例吗？为什么？
8. 容器是怎么知道该把哪个 `ILogger<T>` 传给某个服务的？

### ILogger / 结构化日志

9. `ILogger<OrderService>` 里的 `OrderService` 表示什么？
10. 字符串插值 `$"{x}"` 和结构化日志 `"{X}", x` 在日志层面有什么区别？
11. `_logger.LogInformation("User {UserId} ordered {OrderId}", a, b)` 里 `{OrderId}` 拿到的是 `a` 还是 `b`？
12. `LogError(ex, "...")` 第一个参数为什么是 exception？

### Lambda / 委托

13. `Action` 和 `Action<int>` 的区别？
14. `Action<int>` 和 `Func<int, int>` 的区别？
15. lambda `n => Console.WriteLine(n)` 里的 `n` 是什么角色？类型怎么确定？
16. 为什么 lambda 可以省略 `return`？什么情况下必须写？
17. `=>` 在什么位置是 lambda、什么位置是 expression-bodied member？
18. `services.AddLogging(builder => { ... })` 这个 lambda 是谁调用的？什么时候调用？

### 扩展方法

19. 扩展方法的定义里 `this` 出现在哪里？调用时要不要写 `this`？
20. `"hello".LengthSquared()` 在编译期被改写成什么？

### `<>` vs `()`

21. `Action<int>` 里的 `<int>` 为什么用尖括号而不是圆括号？
22. `log(42)` 里的 `()` 是什么作用？

## 下一步建议

今天已经把 DI + Logging + Lambda + 扩展方法这 4 个点织成一张完整的网。明天可以二选一：

- 选项 A：**quiz me** — 用上面 22 道题做一次 review，巩固 DI + lambda 心智模型
- 选项 B：**抽象 + 替换实现** — 给 `TemperatureSensorService` 抽出一个 `ITemperatureReader` 接口，再注入两个不同实现（真实 sensor + fake sensor），看 DI 替换实现的威力
- 选项 C：**Scoped 实战** — 用 `IServiceScopeFactory` 在 console host 里手动创建 scope，亲眼看到 Scoped 服务跨 scope 是新实例、同 scope 内是同实例

## 今天最终状态

```text
2026-05-21/
├── DI-ILogger-Fundamentals-Summary.md       ← 晨间理论
├── Current-Learning-Status.md                ← 学习追踪（待更新）
└── DI-Logging-MinHost/                       ← 今天的代码
    ├── DI-Logging-MinHost.csproj             ← TFM net9.0 + 3 个 NuGet 包
    ├── Program.cs                            ← 最小 DI host（6 步）
    └── TemperatureSensorService.cs           ← 业务服务（构造函数注入 ILogger<T>）
```

`dotnet run` 输出：

```text
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 72.5
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 101.3
warn: DI_Logging_MinHost.TemperatureSensorService[0]
      Temperature is too high: 101.3
```
