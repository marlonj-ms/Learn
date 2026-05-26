# DI Lifetime + Tracing Summary — May 24, 2026

## TL;DR

Today closed out **Session N+1** of the 3-session arc by making the DI lifecycle *visible*.
Two hands-on artifacts produced:

1. **Lifetime-Drill** — proved Transient / Singleton / Scoped behavior side-by-side using a tiny `ProbeService` with a unique `Guid Id`.
2. **DI-Logging-MinHost (traced version)** — same project as yesterday, but `Program.cs` now prints labeled phase headers `[REG]` / `[BUILD]` / `[RESOLVE]` / `[USE]` so the user can SEE that constructors only fire in Phase 3.

## Files

```text
2026-05-24/
├── Current-Learning-Status.md
├── DI-Lifetime-Tracing-Summary.md          ← 本文件
├── Lifetime-Drill/
│   ├── Lifetime-Drill.csproj               ← net9.0, 仅 DependencyInjection 包
│   ├── ProbeService.cs                     ← Guid Id + ctor 打印
│   └── Program.cs                          ← Transient / Singleton / Scoped 三块对照
└── DI-Logging-MinHost/                     ← 从 2026-05-21 复制 + Program.cs 重写
    ├── DI-Logging-MinHost.csproj
    ├── Program.cs                          ← 4 阶段 trace 版本
    ├── OrderService.cs / IInventory.cs
    ├── FakeInventory.cs / RealInventory.cs
    └── TemperatureSensorService.cs
```

---

## Concepts Locked-In Today

### 1. Service Lifetimes (生命周期)

| Lifetime | 含义 | 验证方式 |
|---|---|---|
| **Transient** | 每次 resolve 都 new | resolve 两次 → 两个不同 `Guid Id` |
| **Singleton** | 整个 provider 共享一个 | resolve 两次 → 同一个 `Guid Id` |
| **Scoped** | 每个 scope 一个 | 同一 scope 两次 → 相同；跨 scope → 不同 |

**Web 应用对应关系**：scope ≈ 一个 HTTP request。`DbContext` 默认 Scoped 就是这个原因。

**反模式**：把 Scoped 注入 Singleton 会让 Singleton 永远持有第一次 scope 的对象 → 数据陈旧 / 内存泄漏。`ValidateScopes = true`（ASP.NET Core 在 Development 自动开启）会在运行时抓出这种错误。

### 2. DI 的 4 个阶段（用 trace 看到的）

| 阶段 | 代码 | 做了什么 | 构造函数有没有跑 |
|---|---|---|---|
| **1. REGISTRATION** | `services.Add...` | 把 (ServiceType → ImplementationType, Lifetime) 三元组塞进列表 | ❌ 没有 |
| **2. BUILD** | `BuildServiceProvider()` | 把注册列表"冻结"成可解析的容器 | ❌ 没有 |
| **3. RESOLVE** | `GetRequiredService<T>()` | 按需构造依赖图（bottom-up） | ✅ 这里才跑 |
| **4. USE** | 业务调用 | 调方法处理数据 | 已经构造完，无关 |

**Anchor sentence**：
> Registration ≠ construction. BuildServiceProvider only freezes the rule book.

### 3. "23 个 services" 的解释

调用 `AddLogging(...)` + `AddConsole()` 之后 `services.Count` 跳到 23，看似神秘。原因：

- `AddLogging` 是扩展方法，内部会偷偷追加：`ILoggerFactory` / `ILogger<>` / Logger filter 配置 / Options 管线（`IOptions<>`、`IOptionsMonitor<>`、`IConfigureOptions<>`）等
- `AddConsole` 再追加：`ConsoleLoggerProvider` + 格式化器（formatter）相关项
- 这些都是从外部 DLL 来的注册，不在你自己的项目源码里

**心智模型**：把每个 `AddXyz` 当成"导入一个 service descriptor 的包"。Count 突然变大是正常的，不是 bug。

### 4. 为什么 Lifetime-Drill 每块都新建 ServiceCollection

教学性的"实验隔离"，不是生产模式：

- **原因 1（lifetime 隔离）**：同一 `ServiceCollection` 里若注册同一个类型 3 次（Transient / Singleton / Scoped），最后一次注册胜出 → 三块实验全部退化成 Scoped。
- **原因 2（provider 隔离）**：`using var sp = ...` 让 provider 在 `{ }` 离开时 dispose，杀掉 Singleton 实例，下一块从零开始。

**生产模式**：一个 `ServiceCollection`，一个 `ServiceProvider`，整个应用生命周期共用。

### 5. DI 的演进史（背景）

- 1980–90s：紧耦合代码（到处 `new`）导致难测试 / 难替换
- 2000 前后：Inversion of Control（IoC）概念出现
- 2004 Martin Fowler 文章定型 "Dependency Injection" 术语
- Java 阵营（Spring）把 DI 推向企业级
- .NET 早期靠 Autofac / Ninject / Castle Windsor 等第三方容器
- ASP.NET Core 起 .NET 自带 `Microsoft.Extensions.DependencyInjection`，DI 成为默认 

DI 看起来"很成熟"是因为它真的演进了 30 多年。表面 API 简洁，底下是大量 battle-tested 架构。

---

## Trace Output（DI-Logging-MinHost traced 版本运行结果摘要）

```text
==================== PHASE 1: REGISTRATION ====================
[REG] new ServiceCollection() -- empty rule book created
[REG] + Logging infrastructure (Console)
[REG] + TemperatureSensorService (Transient)
[REG] + OrderService (Transient)
[REG] + IInventory -> FakeInventory (Transient)
[REG] Rule book ready. Registrations recorded: 23
[REG] NOTE: zero constructors have run yet.            ← KEY

==================== PHASE 2: BUILD ====================
[BUILD] Calling services.BuildServiceProvider() ...
[BUILD] ServiceProvider is alive. Still no constructors.   ← KEY

==================== PHASE 3a: RESOLVE #1 ====================
[RESOLVE] Asking provider for OrderService #1 ...
info: ... FakeInventory created. Instance=...              ← ctors fire HERE
info: ... OrderService  created. Instance=...
[RESOLVE] Got OrderService. Dependencies built.

==================== PHASE 3b: RESOLVE #2 ====================
info: ... FakeInventory created. Instance=...              ← NEW instance (Transient)
info: ... OrderService  created. Instance=...
[RESOLVE] ReferenceEquals(order1, order2) = False
```

---

## Status of the 3-Session Arc

| Session | Topic | Status |
|---|---|---|
| **N+1** | DI + ILogger + Lifetimes | ✅ **DONE** (today) |
| **N+2** | Minimal REST API hosting `TemperatureSensor.Core` | ⬜ Next |
| **N+3** | Integration tests (`WebApplicationFactory<T>`) + Dockerfile | ⬜ |

---

## Open Questions Resolved Today

- ✅ Why does `BuildServiceProvider()` not crash when `IInventory` is missing?
  → Lazy validation by default. Failure surfaces at first `GetRequiredService` that needs it. Opt in to fail-fast with `ValidateOnBuild = true`.
- ✅ Why doesn't the container auto-`new FakeInventory()` if it's the only impl?
  → Container is a dumb dictionary, not a code scanner. Explicit-over-implicit. Multiple impls would cause ambiguity.
- ✅ Why do we need a NEW `ServiceCollection` per drill block?
  → Lifetime isolation (last registration wins) + Singleton instance isolation.
- ✅ Why does `services.Count` jump to 23?
  → `AddLogging` + `AddConsole` are "registration macros" that push many framework services in one call.
