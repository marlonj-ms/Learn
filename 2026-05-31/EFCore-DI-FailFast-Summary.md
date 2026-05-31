# Day 55 (PM) — EF Core DI 快速失败（fail-fast）实锤 + DI 词汇债务暴露

> **Day 55 · PM 段** · 2026-05-31（上午 Day 54 已收尾过 ChangeTracking）
> 主题：把 Day 54 留下的"DI 版 DbContext"接好；途中触发了一次教学失误，反而暴露出 DI 基础词汇没建好，明天回炉重学。
> 状态：实验 A/B 全跑通 ✅ · 4 概念全 lock ✅ · Add/Build/Resolve 词汇 ❌ → Day 56 重学 DI

---

## 🎯 Day 55 (PM) 一句话总结

> **「EF Core 的 `AddDbContext<T>` 是 fail-fast：在『注册阶段』用反射偷看 DbContext 的 ctor 签名，发现没 `(DbContextOptions<T>)` 这个 ctor 就立刻炸。普通 `AddSingleton<T>` 不做这件事 —— 坏 ctor 要等到 GetRequiredService 真造对象那一刻才暴露。」**

但这条结论的暴露过程也暴露了一个**教学债**：mentor 一直用「Add / Build / Resolve」三个词描述 DI 三阶段，从来没正式定义这三个词分别对应哪一行代码。学习者今天据此填空填反 —— 不是不懂概念，是没建好词汇。Day 56 回炉 DI 基础。

---

## 🧩 今天讲清的 5 件事

### 1. DbContext + DbSet + Options 三件套（DI 版的样子）

```csharp
public class SensorDbContext : DbContext
{
    // ① 构造器接 options —— DI 容器从外面塞进来
    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options) { }

    // ② DbSet<T> = 表的入口（Day 53 学的）
    public DbSet<Reading> Readings => Set<Reading>();
}
```

| 部件 | 角色 |
|---|---|
| `DbContext` 基类 | EF Core DLL 里的抽象类 |
| `DbSet<TEntity>` | EF Core DLL 里的泛型类，实现 `IQueryable<T>` |
| `=> Set<TEntity>()` | 调基类的 `protected` 泛型方法（Day 54 锁住） |
| `DbContextOptions<T>` | **配置载体**（连接串 / provider / 日志），从外面塞进来 |
| `<SensorDbContext>` | **幻影类型参数（phantom type parameter / marker generic）** —— 容器靠它区分"这是给 SensorDbContext 的 options，不是给 OrderDbContext 的" |

---

### 2. EF Core 反射体检（fail-fast at registration）

**实验 A — `ExpA-LazyDI/Program.cs`**（证明普通 DI 是懒的）：

```csharp
services.AddSingleton<Bomb>();                  // line 6  → ✅ 通过
var provider = services.BuildServiceProvider(); // line 10 → ✅ 通过
var b = provider.GetRequiredService<Bomb>();    // line 15 → 💥 Bomb ctor 抛异常
```

**结论**：普通 DI 的 `AddSingleton<T>` 不查 ctor、Build 也不查。**坏 ctor 要到 line 15 真造对象时才暴露**。

---

**实验 B — `ExpB-EFCoreFastFail/Program.cs`**（证明 EF Core 是反射体检的 fail-fast）：

```csharp
services.AddDbContext<SensorDbContext>(opts =>     // line 7
    opts.UseSqlite("Data Source=expB.db"));
// 输出顺序证明：
// [1] AddDbContext returned. Reflection check passed. ✅
// [2] BuildServiceProvider returned. Still no ctor ran. ✅
// [3] About to call GetRequiredService<SensorDbContext>() ...
//     >>> SensorDbContext ctor is running NOW <<<       ← 在两个 [3] 之间！
// [3] Got SensorDbContext. NOW the ctor really ran. ✅
```

**关键观察**：`>>> ctor running NOW <<<` 出现在 **两个 `[3]` 中间** —— 说明 EF Core 的体检只是**反射看 ctor 签名**，没真的 new；ctor 真正跑还是在 `GetRequiredService` 那一刻。

**对照 `temp/Program.cs`**（无 ctor 的坏 DbContext）：`AddDbContext` 直接抛 `ArgumentException`，line 11 的 `GetRequiredService` 根本跑不到 —— 这就是 fail-fast。

---

### 3. 三个项目同框对照

| 项目 | `SensorDbContext` 有 ctor? | line 6 (Add 阶段) | line 10 (Build 阶段) | line 15 (Resolve 阶段) |
|---|---|---|---|---|
| **temp/**（坏 DbContext） | ❌ 没有 | 💥 反射体检失败 | 跑不到 | 跑不到 |
| **ExpA-LazyDI**（普通 Bomb） | n/a | ✅ 不查 | ✅ 不查 | 💥 ctor 真 new 时抛 |
| **ExpB-EFCoreFastFail**（好 DbContext） | ✅ 有 | ✅ 体检通过 | ✅ 懒 | ✅ ctor 此刻才跑 |

**口诀**：
- **EF Core 早死**（fail-fast at registration，靠反射）
- **普通 DI 晚死**（fail-lazy at resolution，靠真造对象）

---

### 4. `ctor` vs `OnConfiguring` 优先级（修正后的正确版）

> 之前我贴过一段误导性的伪代码，被学习者抓到。下面是正确版：

```csharp
// DbContext 基类伪代码
internal void OnConfiguringInternal(DbContextOptionsBuilder builder)
{
    // ① builder 先被「ctor 传进来的 options」预填
    builder.UseExistingOptionsFromConstructor(this._optionsFromCtor);
    // ② 然后调你重写的 OnConfiguring（每次 new 都跑）
    this.OnConfiguring(builder);
    // ③ builder.Build() = ctor 设置 + OnConfiguring 设置，后者覆盖前者
}
```

**规则**：
- `OnConfiguring` **永远会被调** —— 不管你用不用 DI
- ctor 传进来的 options 只是给 builder 的**初值**
- `OnConfiguring` 里再 `UseXxx(...)` 就**覆盖** ctor 的设置

**安全锚句**：
- 用 DI 时 → 删掉 `OnConfiguring`，让 ctor 的 options 不被覆盖
- 用 demo 自配置时 → 不写 ctor，靠 `OnConfiguring`，二选一

---

### 5. 暴露的 DI 词汇债 ❗（明天补）

我从头到尾用了三个词：**Add / Build / Resolve**。但从来没正经讲过它们分别是哪一行代码：

| 我说的 | 真正对应的代码 | 真正做的事 |
|---|---|---|
| **Add 阶段** | `services.AddXxx<T>(...)` | 往 `ServiceCollection` 里追加一条 `ServiceDescriptor`（**配方**），不造对象 |
| **Build 阶段** | `services.BuildServiceProvider()` | 把 `ServiceCollection` 冻成只读的 `ServiceProvider`（**搭建解析图**），还是不造对象 |
| **Resolve 阶段** | `provider.GetRequiredService<T>()` | 真正按配方造对象（按 Singleton/Scoped/Transient 决定造一次还是每次造） |

**学习者的填空错误**：把"普通 DI 坏 ctor 在哪暴露"填成了 **Build**，正确答案是 **Resolve**。**原因不是不懂 DI，而是这三个词从来没被正式定义。** Day 56 第一件事就是补这块。

---

## 🔬 demo 输出验证

### ExpA-LazyDI 跑下来的关键证据

```
[1] After AddSingleton<Bomb>      ← line 6  通过
[2] After BuildServiceProvider     ← line 10 通过
[3] Before GetRequiredService<Bomb>
💥 Bomb ctor 抛异常               ← line 15 才暴露
```

### ExpB-EFCoreFastFail 跑下来的关键证据

```
[1] About to call AddDbContext<SensorDbContext>(...) ...
[1] AddDbContext returned. Reflection check passed. ✅
[2] About to call BuildServiceProvider() ...
[2] BuildServiceProvider returned. Still no ctor ran. ✅
[3] About to call GetRequiredService<SensorDbContext>() ...
    >>> SensorDbContext ctor is running NOW <<<
[3] Got SensorDbContext. NOW the ctor really ran. ✅
Saved!
```

`>>> ctor running NOW <<<` 出现在两个 `[3]` 中间 = 反射体检 ≠ 真造对象。

### temp/（坏 DbContext，作为反衬）

```
[1] About to call AddDbContext<SensorDbContext>(...) ...
💥 ArgumentException: No suitable constructor was found for entity type 'SensorDbContext'.
```

line 6 直接炸，line 11 的 Resolve 根本没机会跑。

---

## 🧠 今天产生的 5 条新口诀（#86–90）

| # | 口诀 |
|---|---|
| 86 | **EF Core `AddDbContext<T>` 是 fail-fast**：注册时用反射偷看 `T` 的 ctor，没合规 ctor 就立刻抛 `ArgumentException`，不用等到 Resolve。 |
| 87 | **普通 DI 是 fail-lazy**：`AddXxx` 只追加配方、`BuildServiceProvider` 只搭图，ctor 真正跑（也才有可能抛）在 `GetRequiredService` 那一刻。 |
| 88 | `DbContextOptions<TContext>` 的泛型参数是 **幻影类型参数（phantom type parameter）**，纯粹做"哪个 DbContext 的 options"的类型区分。 |
| 89 | `OnConfiguring` **永远会被调**（不管你用不用 DI），它在 ctor options 之**后**跑，会覆盖前面的设置 —— 所以 DI 路径上要**删掉它**，避免无声覆盖。 |
| 90 | "反射偷看签名" **比** "真造对象" **便宜得多**，所以 EF Core 把它放在最早的注册阶段当门卫，宁可早死也不让坏 DbContext 进入运行时。 |

**累计口诀 90 条。**

---

## 💀 今天反复踩的坑（追加到 cheatsheet 77–78）

### 坑 77：把"反射体检"和"真造对象"混为一谈

把"反射"听成"晚一点 / 慢一点"，于是误以为反射检查发生在 Resolve 阶段。
**真相反过来**：反射只是读 metadata，便宜到可以放在最早的 Add 阶段当门卫。EF Core 就是这么干的。

### 坑 78：用未定义的词汇教学 = 看起来懂了，其实只是跟着说

mentor 用「Add / Build / Resolve」三个词描述 DI 三阶段，但从来没把它们 map 到 `services.AddXxx<T>()` / `services.BuildServiceProvider()` / `provider.GetRequiredService<T>()` 三行代码。学习者据此填空 → 当然填反。
**修复原则**：**任何抽象阶段词，必须先 map 到一行可点的代码**。

---

## 📂 今天产出文件

```
2026-05-31/
  DI-DbContext-Drill/                   ← 4 阶段 drill（含正确 ctor 的参考实现）
  ExpA-LazyDI/
    Program.cs                          ← 普通 Bomb，证明 fail-lazy
    ExpA-LazyDI.csproj                  ← 仅 DI（无 EF Core）
  ExpB-EFCoreFastFail/
    Program.cs                          ← 好 DbContext，证明反射体检后还是懒 new
    ExpB-EFCoreFastFail.csproj
  temp/                                 ← 坏 DbContext（无 ctor）反衬 fail-fast
  EFCore-DI-FailFast-Summary.md         ← 本文件
```

> 注：`Scratch-DbContext-Direct/` 保留不动；上午段的 `EFCore-ChangeTracking/` + `ChangeTracking-Summary.md` 也保留。

---

## 🅿️ Day 56 Resume Point —— DI 从零回炉

明天**不**继续 EF Core 进度，先把 DI 词汇债还掉。计划：

1. **`ServiceCollection` 到底是什么？** —— 拆开看它是 `IList<ServiceDescriptor>`，"注册"就是 `Add` 一条 descriptor。
2. **`ServiceDescriptor` 是什么？** —— `(ServiceType, ImplementationType, Lifetime, Factory?)` 四元组。lifetime ∈ {Singleton, Scoped, Transient}。
3. **`BuildServiceProvider()` 在做什么？** —— 把 collection 冻结成只读 `ServiceProvider`；它会"分析依赖图"但不 new 对象。
4. **`GetRequiredService<T>()` 在做什么？** —— 按 descriptor 真造对象；Singleton 缓存第一次结果，Scoped 在 scope 内缓存，Transient 每次新建。
5. **回到今天**：再回看 ExpA / ExpB，用新词汇重新表述"为什么 EF Core 是 fail-fast 而普通 DI 是 fail-lazy"。
6. **如果还有时间**：Path 2 = `TemperatureSensor.Api` 接入 `AddDbContext`，跑通昨天的集成测试。

**预期学完之后能正确填的话**：

> EF Core 的 `AddDbContext<T>` 在 **services.AddXxx**（= `ServiceCollection.Add`）这一行就靠反射偷看 ctor；
> 普通 `AddSingleton<T>` 不做这件事 —— 它在 Add 只追加配方，Build 只搭图，真正炸要等到 **provider.GetRequiredService**（= 按配方造对象）那一刻。

---

## 🏁 Today's One-Line Win

> **「跑通了 fail-fast vs fail-lazy 的对照实验，但更大的收获是：发现自己在用一套没被定义清楚的 DI 词汇 —— 这种『看起来跟得上、其实在猜』的状态被一道填空题诚实地暴露了。明天补它。」**
