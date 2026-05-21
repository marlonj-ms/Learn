# Current C# Learning Status — May 21, 2026 (Updated: End of Day)

## Today's Topic — Final State

```text
DI + ILogger 概念建模（晨间，理论）
    ↓
最小可运行 DI host 项目（午后，实操）
    ↓
Lambda / 泛型委托 / 扩展方法 三层语法逐字拆解（下午）
```

今天从“纯理论”过渡到了“能跑的代码”，并且把 `services.AddLogging(builder => {...})` 这一行的所有语法疑点全部清掉。

## Today's Final State (Files)

```text
2026-05-21/
├── DI-ILogger-Fundamentals-Summary.md          ← 晨间理论总结
├── DI-Logging-HandsOn-And-Lambda-Summary.md    ← 当日完整 summary（理论+实操+lambda 拆解）
├── Current-Learning-Status.md                  ← 本文件
└── DI-Logging-MinHost/                         ← 第一个 DI 项目
    ├── DI-Logging-MinHost.csproj               ← TFM net9.0
    ├── Program.cs                              ← 6 步 DI host
    └── TemperatureSensorService.cs             ← 构造函数注入 ILogger<T>
```

`dotnet run` 实际输出：

```text
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 72.5
info: DI_Logging_MinHost.TemperatureSensorService[0]
      The temperature sensor reported 101.3
warn: DI_Logging_MinHost.TemperatureSensorService[0]
      Temperature is too high: 101.3
```

## Concepts Covered (累计到今天)

### DI / 服务生命周期

- 业务数据 vs 依赖项的区分：构造函数放依赖、方法参数放业务数据
- 构造函数注入（constructor injection）+ 私有只读字段
- 依赖抽象 vs 具体实现：`ILogger<T>` vs `FileLogger`
- 紧耦合（tight coupling）问题：类自己 `new` 依赖
- 三种服务生命周期 `Transient` / `Scoped` / `Singleton` 的语义和何时释放
- `Scoped` 在 ASP.NET Core 默认 = per HTTP request（不是 per user）
- 用户身份跨请求由 cookie/JWT/session 保存，不是靠 Scoped 对象长期活着
- 错把 `CurrentUserContext` 注册成 Singleton 是安全风险（跨用户串数据）
- 全局并发限流器 `SemaphoreSlim(10)` 必须 Singleton，否则限制失效

### ILogger / 结构化日志

- `ILogger<T>` 中 `T` 是日志类别（log category），不是消息类型
- 消息模板 + 结构化字段：`"...{Reading}", reading`
- 结构化字段按位置匹配（positional matching），不按变量名匹配
- `LogInformation` / `LogWarning` / `LogError(ex, ...)` 三个等级
- 字符串插值 `$"...{x}"` 会丢失结构化字段
- `AddLogging` + `AddConsole` + `SetMinimumLevel(LogLevel.Information)` 三件套

### ServiceCollection / ServiceProvider

- `new ServiceCollection()` → 注册清单
- `services.AddLogging(...)` / `services.AddTransient<T>()` → 注册阶段
- `services.BuildServiceProvider()` → 构建容器
- `provider.GetRequiredService<T>()` → 解析阶段（这一步才真正创建对象）
- 容器读取构造函数自动注入依赖
- `using var provider = ...` → 应用结束时自动 dispose 容器

### Lambda / 泛型委托 / 扩展方法

- `Action` = 无参无返回值的委托类型
- `Action<T>` = 接受一个 T 参数、无返回值的委托类型
- `Func<TIn, TOut>` = 有参有返回值的委托类型
- lambda 是“没有名字的方法”
- lambda 参数由 lambda 自身定义，类型由委托签名推断
- 表达式 lambda（`x => x*2`）vs 语句 lambda（`x => { ...; return ...; }`）
- 没有 `return` 是因为 `Action<T>` 返回 `void`，不是错误
- lambda 不会自己执行；要么自己存到变量后调用，要么传给别人在内部调用
- `services.AddLogging(builder => {...})` 是配置回调模式（configuration callback）
- 扩展方法（extension method）：`this` 写在定义里，调用时不写
- `"hello".LengthSquared()` ≡ `StringExtensions.LengthSquared("hello")`
- `<>` 给类型加类型参数；`()` 给方法/委托加调用参数

## 完整路径回顾

```text
LINQ + Lambdas → Delegates → Events → Inheritance →
Async/Await → Throttling → Core Syntax → LINQ Drills →
Events Practice → Unit Testing + xUnit + .NET Internals →
DLL/API/SDK + NuGet/SemVer + IL 预览 →
DI + ILogger 理论 → DI + ILogger 实操 + Lambda 深拆解 ✅
```

## Important Corrections Today

- `ILogger<T>` 的 `T` 是日志类别，不是消息类型
- DI 不是“类不声明依赖”，而是“类不创建具体依赖”
- 字符串插值 vs 结构化日志：`$"...{x}"` ≠ `"...{X}", x`
- 结构化日志按位置匹配，不按变量名匹配
- `Scoped` ≠ per user（默认 per HTTP request）
- 全局限流器必须 Singleton，否则限制失效
- `Action<int>` 是“类型”，不是“方法签名”；类型参数用 `<>` 不是 `()`
- lambda 参数 `builder` 不是“缺乏定义”，它是 lambda 自身定义的
- lambda 没有 `return` 不是错误，是 `Action<T>` 返回 `void`
- `services.AddLogging(...)` 不写 `this`，因为 `this` 写在 `AddLogging` 定义里
- 同一段顶层语句不能重复声明 `var services = new ServiceCollection();`（笔记应做注释）
- `dotnet build` / `dotnet run` 必须从有 `.csproj` 的目录运行（今天解决了 csproj 误进 bin 的小事故）

## 明日 Next Session Recommendation

三选一：

- **选项 A — quiz me**：用 `DI-Logging-HandsOn-And-Lambda-Summary.md` 末尾的 22 道复习题做一次全量 review
- **选项 B — 抽象 + 替换实现**：给 `TemperatureSensorService` 抽出 `ITemperatureReader` 接口，注入两个不同实现（real / fake），实战体验 DI 替换的威力
- **选项 C — Scoped 实战**：用 `IServiceScopeFactory` 在 console host 里手动创建 scope，亲眼看 Scoped 在同 scope 内是同实例、跨 scope 是新实例

我个人推荐：先 A（巩固），再 B（拓展），再 C（深入 lifetime）。

## Files Created Today

- `2026-05-21/DI-ILogger-Fundamentals-Summary.md`
- `2026-05-21/DI-Logging-HandsOn-And-Lambda-Summary.md`
- `2026-05-21/Current-Learning-Status.md`
- `2026-05-21/DI-Logging-MinHost/DI-Logging-MinHost.csproj`
- `2026-05-21/DI-Logging-MinHost/Program.cs`
- `2026-05-21/DI-Logging-MinHost/TemperatureSensorService.cs`
- `dailyread/dailyread-2026-05-21.html`（由收尾流程生成）
