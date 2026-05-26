# Current C# Learning Status — May 24, 2026 (End of Day)

## Today's Theme

```text
让 DI 从“概念”变成“可观察的过程”
    ↓
Lifetime Drill：Transient / Singleton / Scoped 三块对照实验
    ↓
4-Phase Trace：把 REGISTRATION / BUILD / RESOLVE / USE 用 [TAG] 打到控制台
    ↓
Session N+1（DI + ILogger + Lifetimes）正式收尾 ✅
```

今天的核心成就：把昨天还略显抽象的“依赖注入”，今天用两个小项目变成了**眼睛能看见的过程**。

## Files

```text
2026-05-24/
├── Current-Learning-Status.md              ← 本文件
├── DI-Lifetime-Tracing-Summary.md          ← 上午：DI lifetimes + 4 阶段 trace
├── API-Concepts-Foundations-Summary.md     ← 下午：API / HTTP / REST 概念铺垫
├── Lifetime-Drill/                         ← 三种生命周期对照
└── DI-Logging-MinHost/                     ← 4 阶段 trace 版（昨日项目的 traced clone）
```

## 3-Session Arc Status

| Session | Topic | Status |
|---|---|---|
| **N+1** | DI + ILogger + Lifetimes | ✅ **DONE** (closed today) |
| **N+2** | Minimal REST API (ASP.NET Core) hosting `TemperatureSensor.Core` | 🟡 **概念已铺垫**，明天动手 scaffold |
| **N+3** | Integration tests (`WebApplicationFactory<T>`) + Dockerfile | ⬜ |

## 下午加场：API / HTTP / REST 概念铺垫

本来要直接动手造 API，临时叫停先把概念吃透 — 因为"造一个跑得起来的 API"和"知道它为什么这么造"是两回事。
下午这一段把三层概念串起来了：

```text
API   = 契约（signature + semantics）
HTTP  = 远程传输 API 的协议（信件 + 状态码）
REST  = 用 HTTP 时的"资源 + 动词"设计风格
```

也清掉了 agent 之前演示版的 `TemperatureSensor.Api/`（要自己重建）。
完整内容见 `API-Concepts-Foundations-Summary.md`。

## Cumulative Concept Map (到今天为止)

### 基础语法
- Core syntax (loops/conditions/brackets/arrows)
- LINQ (deferred vs immediate)
- Lambdas + delegates (Func / Action)
- Extension methods (this keyword)
- Events (standard pattern, EventHandler<T>)
- Virtual / Sealed (polymorphism vs lockdown)
- async / await + `SemaphoreSlim` throttling

### 单元测试
- xUnit `[Fact]` / `[Theory] + [InlineData] / [MemberData]`
- AAA pattern, red/green discipline
- Object-spy with captured args
- `Assert.Throws<T>` for defensive ctor / `ArgumentException` + `nameof`
- `.slnx` + multi-project (Core / Tests)

### 打包与发布
- `dotnet pack` 3-pass: bare → metadata-rich → multi-targeting
- `<TargetFrameworks>` vs `<TargetFramework>`
- SemVer (PATCH / MINOR / MAJOR)
- `.nuspec` 4 required fields (id / version / authors / description)
- IL preview (deferred — too dense for now)

### DI / Logging
- `ILogger<T>` 中 T = log category，不是消息类型
- 结构化日志 vs 字符串插值（后者丢字段）
- ServiceCollection → BuildServiceProvider → GetRequiredService 三步走
- Constructor injection + 私有只读字段 + 接口抽象（`IInventory`）
- **三种生命周期完整对照（today）**
- **DI 的 4 个阶段 + 用 [TAG] 看到 ctor 何时跑（today）**
- **"23 个 services"现象解释（today）**

### API / HTTP / REST 概念（today 下午）
- **API = 契约**（signature + semantics）；编译器只查前者
- **API 在每一层都成立**：method / class / library / IPC / network
- **HTTP 比方法调用多一种结局**：partial failure（送达但响应丢失）
- **幂等性（idempotency）** + `Idempotency-Key` 头的存在意义
- **状态码三段**：2xx 我做了 / 4xx 你错了 / 5xx 我错了；4xx 不重试，5xx 看幂等
- **`200 / 201 / 202`** 的细分（同步完成 / 创建 / 已收下还在处理）
- **REST = URL 名词 + HTTP 动词**；stateless 让服务横向扩展

## Next Up (N+2 实战 — 明天)

**目录**：`d:\AITriage\2026-05-25\`（新一天，新文件夹）

**Step 0** ✅ 已完成：清场（agent 演示版的 `TemperatureSensor.Api/` 已删）

**Step 1** ⬜ 自己 scaffold：
```powershell
cd d:\AITriage\2026-05-25
dotnet new web -n TemperatureSensor.Api --framework net9.0 -o TemperatureSensor.Api
```

后续按 **7-step 配方**（Scaffold → Wire deps → Register → Build → Configure → Run → Test），每步完成 ping mentor 后再开下一步。

**API 项目要实现**：
- 资源：`/readings`（POST 提交温度读数）+ `/health`（GET 探活）
- 把 `TemperatureSensor.Core` 当库引用
- 接入 `ILogger<T>` 记录阈值告警事件
- 返回 `IResult`：成功 → `202 Accepted`，NaN/Infinity → `400 Bad Request`
- 后续接 `appsettings.json` + `IOptions<T>` 取阈值（不要 hardcode）

## Anchor Sentences（要记住的几句）

1. **Registration ≠ construction.** `BuildServiceProvider` only freezes the rule book.
2. **Constructors fire only in PHASE 3 (RESOLVE).** Phase 1 + 2 print zero ctors.
3. **Transient = vending machine, Singleton = office coffee pot, Scoped = meeting whiteboard.**
4. **In ASP.NET Core, scope = one HTTP request.**
5. **Never inject Scoped into Singleton** (otherwise the singleton freezes the first scope's object forever).
6. **API = signature + semantics.** Compiler checks the first, humans + tests check the second.
7. **HTTP = three outcomes, not two.** The third is "delivered but response lost" — partial failure.
8. **2xx 我做了。 4xx 你错了。 5xx 我错了。 4xx 不重试，5xx 看幂等。**
9. **REST = URL noun + HTTP verb.** Stateless between requests.
