# Day 54 — EF Core 变更追踪（change tracking）实战 + DI 反衬

> **Day 54** · 2026-05-31
> 主题：把 Day 53 留下的「快照对比」从理论 → 实验台验证；顺带答清「`Set<Reading>()` 是哪个类上的方法」、「`OnConfiguring` 是不是 DI」两个开放问题。
> 状态：核心机制三场景全跑通 ✅ · 检查题两题全对 ✅ · DI vs 自配置反衬清楚 ✅ · Day 55 接入 `TemperatureSensor.Api` 已铺路

---

## 🎯 Day 54 一句话总结

> **「快照对比（snapshot comparison）= EF Core 自动 diff 的全部秘密。`SaveChanges` 不是无脑写库，而是查询时拍照、提交时逐字段 diff，**只为变了的字段发 UPDATE**；`AsNoTracking` = 关闭快照，性能换掉自动 diff。」**

回答了 Day 53 遗留的核心机制问题；同时反衬出「`OnConfiguring` 自配置 ≠ DI」。

---

## 🧩 今天讲清的 3 件事

### 1. Day 53 遗留问题：`Set<Reading>()` 到底是哪个类上的方法？

**答案**：是 **`DbContext` 基类**上的一个 `protected` 泛型方法，**不是** `DbSet<T>` 的方法。

```csharp
// DbContext 基类源码（伪代码）
public abstract class DbContext : IDisposable
{
    private readonly Dictionary<Type, object> _setCache = new();   // 实例字段，每个 DbContext 一份

    public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class
    {
        if (!_setCache.TryGetValue(typeof(TEntity), out var set))
        {
            set = new InternalDbSet<TEntity>(this);
            _setCache[typeof(TEntity)] = set;
        }
        return (DbSet<TEntity>)set;
    }
}
```

四步小阶梯解锁：
1. `List<T>` / `OfType<T>()` —— 已熟（泛型方法）
2. 自造 `Pick<T>()` 泛型方法 —— 巩固 `T` 是占位符
3. 映射到 `Set<TEntity>()` —— `TEntity` 就是 `T`
4. `_setCache` —— **实例字段（per-instance scope）**，所以两个不同 `SensorDbContext` 实例的缓存**完全无关**

**用户答对的关键检查题**：
> 两个不同的 `SensorDbContext` 实例分别 `Set<Reading>()`，返回同一个 `DbSet<Reading>` 吗？
> 答：**不同**。因为是两个完全不同的 `DbContext` 实例，各自缓存独立。

### 2. 变更追踪三场景实测

| 场景 | 操作 | 控制台 SQL | 结论 |
|---|---|---|---|
| **A** | 查 Id=1（22.5）→ `r.Celsius = 99.9` → SaveChanges | `UPDATE "Readings" SET "Celsius" = @p0 WHERE "Id" = @p1` 参数 `99.9, 1` | diff 发现变化 → 发 UPDATE |
| **B** | 查 Id=2（31.0）→ `r.Celsius = 31.0` → SaveChanges | **只有 SELECT，无 UPDATE** | diff 发现啥也没变 → 零 SQL |
| **C** | `AsNoTracking()` 查 Id=3 → `r.Celsius = 77.7` → SaveChanges | **只有 SELECT，无 UPDATE** | 没追踪 = 没快照 = 没 diff → 改了白改 |

**生产口径锚句**：
- 只读路径 → 加 `AsNoTracking()` 省内存 + 加速
- 要改要存的路径 → 默认追踪，让 EF 自动 diff
- 用了 `AsNoTracking()` 还想存 → 自己 `db.Update(r)` 显式标记

### 3. `OnConfiguring` ≠ DI（反衬 DI 概念）

| 维度 | 当前代码（OnConfiguring） | DI 版本（AddDbContext） |
|---|---|---|
| 配置位置 | DbContext 内部**硬编码** | 外部组合根（Program.cs） |
| 谁决定连哪个库 | DbContext 自己 | 容器 / 配置文件 |
| 谁负责造 DbContext | 你 `new SensorDbContext()` | 容器按配方造 |
| 测试时换内存库 | ❌ 改不了，得改源码 | ✅ `UseInMemoryDatabase` 一行 |
| 生命周期 | 你自己 Dispose | 容器管，默认 **Scoped** |
| 适用场景 | 学习 demo / 控制台脚本 | 生产 Web/API |

**锚句**：`OnConfiguring` = "DbContext 你自己想办法连库"；`AddDbContext` = "容器，请按这个配方造 DbContext，谁要谁拿"。后者才是 DI。

---

## 🔬 demo 输出验证（`EFCore-ChangeTracking/Program.cs`）

跑 `dotnet run`，控制台关键证据：

```
========== 场景 A：改成新值 ==========
[A] 查出来：Id=1, Celsius=22.5
[A] 调 SaveChanges()
Executed DbCommand [Parameters=[@p1='1', @p0='99.9']]
UPDATE "Readings" SET "Celsius" = @p0

========== 场景 B：set 同值 ==========
[B] 查出来：Id=2, Celsius=31.0
[B] 调 SaveChanges()
（没有 UPDATE）

========== 场景 C：AsNoTracking 后改属性 ==========
[C] AsNoTracking 查出来：Id=3, Celsius=18.0
[C] 调 SaveChanges()
（没有 UPDATE）

========== 最终库里的真实状态 ==========
Id=1  Celsius=99.9   ← A 改了
Id=2  Celsius=31.0   ← B 同值
Id=3  Celsius=18.0   ← C 没追踪，数据库里还是 18.0
Id=4  Celsius=35.5   ← 没动
```

---

## 🎓 两道综合检查题（学习者全对）

**Q1**：4 行全查出来，改 1 行 / 同值 1 行 / 改 1 行 / 改 1 行 → SaveChanges 发几条 UPDATE？
- **答**：3 条。EF 只为 diff 出有差异的行发 UPDATE，同值行被跳过。

**Q2**：`AsNoTracking()` 查 4 行，改其中 2 行 → SaveChanges 发几条 UPDATE？
- **答**：0 条。没追踪 = 没快照 = ChangeTracker 里啥也没有 → SaveChanges 找不到要 diff 的东西。

---

## 🧠 Day 54 新口诀（编号 #79–#85）

| # | 口诀 |
|---|---|
| **79** | `DbSet<T>` 是 EF Core **DLL 里**定义的泛型类（实现 `IQueryable<T>`），表的入口。 |
| **80** | `Set<TEntity>()` 是 `DbContext` **基类**上的泛型方法。子类继承得来 → 不加前缀裸调。 |
| **81** | `Set<T>()` 的缓存（`Dictionary<Type, object>`）是 **`DbContext` 的实例字段** —— 每个 DbContext 实例一份。 |
| **82** | **快照对比（snapshot comparison）实锤**：set 同值 = 零 SQL。不是 EF 偷懒，是 diff 真的为空。 |
| **83** | `AsNoTracking` = 没快照 → 改属性 EF 完全不知道，`SaveChanges` 不会发 UPDATE。只读必加。 |
| **84** | 追踪 = 内存代价：每个追踪实体在 ChangeTracker 里多占一份快照副本。表大时这开销可观。 |
| **85** | `OnConfiguring` 自配置 ≠ DI。**DI 版本**：删 `OnConfiguring`，构造器接收 `DbContextOptions<T>`，组合根 `AddDbContext` 注册。 |

累计口诀 **85** 条。

---

## 📂 今天产出文件

```
2026-05-31/
  EFCore-ChangeTracking/
    Program.cs                       ← 三场景实测 demo（A/B/C + 最终校验）
    EFCore-ChangeTracking.csproj     ← 引用 Microsoft.EntityFrameworkCore.Sqlite 10.0.8
    sensor.db                        ← 运行生成的 SQLite 文件
  ChangeTracking-Summary.md          ← this file
```

包：`Microsoft.EntityFrameworkCore.Sqlite` 10.0.8。SDK：.NET 10.0.108。

---

## 🅿️ Day 55 Resume Point

**目标**：把 EF Core 接入现有 `TemperatureSensor.Api`（Day 50–52 的 minimal API），替换掉假数据。

**3 步落地路**：
1. **改 DbContext**：删 `OnConfiguring`，加构造器 `public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }`
2. **改 Program.cs（组合根）**：
   ```csharp
   builder.Services.AddDbContext<SensorDbContext>(opts =>
       opts.UseSqlite(builder.Configuration.GetConnectionString("Db"))
           .LogTo(Console.WriteLine, LogLevel.Information));
   ```
3. **改路由签名（DI 注入）**：
   ```csharp
   app.MapGet("/readings", (SensorDbContext db) =>
       db.Readings.AsNoTracking().ToList());     // ← 今天学的 AsNoTracking 用上了！
   ```

**生命周期会自动是 Scoped**（默认）—— 每个 HTTP 请求一个新 `DbContext`，请求结束自动 Dispose。这正是 Day 24 学的「scope = HTTP request」。

**之后**：集成测试用 `UseInMemoryDatabase` 换内存库（DI 的最大红利之一）。

---

## 🏁 Today's One-Line Win

> **「亲眼看 set 同值 = 0 条 UPDATE、AsNoTracking 改属性 = 0 条 UPDATE，Day 53 留下的『快照对比』理论今天全部落地成可重现的控制台证据。」**
