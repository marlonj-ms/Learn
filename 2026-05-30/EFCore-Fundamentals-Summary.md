# Day 53 — EF Core 基础（第一条查询：LINQ → SQL 亲眼可见）

> **Day 53** · 2026-05-30
> 主题：EF Core 入门 —— ORM 是什么、`DbContext` / `DbSet<T>`、亲眼看 `IQueryable` 链子被翻译成真实 SQL
> 状态：核心概念全收 ✅ · demo 跑通 ✅ · 变更追踪机制讲到一半（明天继续验证 + `AsNoTracking`）

---

## 🎯 Day 53 一句话总结

> **"`DbSet<T> : IQueryable<T>` 这一行继承关系，就是 LINQ 能变成 SQL 的全部秘密。你写 `.Where()`，EF 把表达式树翻译成 `SELECT ... WHERE`，过滤发生在数据库；`.ToList()` 才是按按钮。"**

承接 Day 52 的 `IQueryable` 理论 → Day 53 第一次见到真的 `DbSet<T>` 把链子翻成 SQL。

---

## 🧩 今天学的核心概念

### 1. ORM（Object-Relational Mapping，对象关系映射）
- 在「关系型数据库」和「C# 对象」之间架桥，消灭手工的「字段 ↔ 属性」搬运。
- `实体框架（Entity Framework Core）` = .NET 官方 ORM。

| 数据库世界 | C# 世界 |
|---|---|
| 表 `Readings` | `DbSet<Reading>` |
| 行 (row) | `Reading` 对象实例 |
| 列 `Celsius` | 属性 `Celsius` |
| `SELECT ... WHERE` | `dbSet.Where(...)` LINQ |

### 2. `DbContext` —— 一次数据库会话的总管
- 你**继承** EF Core 的 `DbContext` 基类。
- 内部装着一个/多个 `DbSet<T>`，每个 = 一张表入口。
- 负责三件大事：**连接管理** / **变更追踪（change tracking）** / **一次性提交（`SaveChanges()`）**。
- `OnConfiguring` → 告诉 EF 连哪个数据库（本 demo 用 `SQLite`）。

### 3. 关键代码行拆解
```csharp
public DbSet<Reading> Readings => Set<Reading>();
```
| 片段 | 身份 |
|---|---|
| `DbSet<Reading>` | EF Core **DLL 里**定义的表入口类（实现 `IQueryable<T>`），不是你的代码 |
| `Readings` | 属性名；`db.Readings` 访问；也是**表名来源**（复数约定）|
| `=>` | **表达式体属性（expression-bodied property）** ≡ `get { return ...; }` |
| `Set<Reading>()` | **`DbContext` 基类上的方法**（不是 `DbSet` 的初始化方法！），把缓存好的 `DbSet<Reading>` 取出来 |

> 两种写 DbSet 的方式：
> - `public DbSet<Reading> Readings { get; set; }` —— 经典自动属性，EF 反射注入（有可空性警告）
> - `public DbSet<Reading> Readings => Set<Reading>();` —— 现代推荐，避开可空性警告，永不为 null

---

## 🔬 demo 输出解码（`EFCore-FirstQuery/Program.cs` 实测）

跑 `dotnet run`，`LogTo(Console.WriteLine, ...)` 把 EF 生成的 SQL 全打印出来：

1. **建表** —— 没写一行 SQL，EF 看 `Reading` 类生成：
   ```sql
   CREATE TABLE "Readings" (
       "Id" INTEGER NOT NULL CONSTRAINT "PK_Readings" PRIMARY KEY AUTOINCREMENT,
       "Celsius" TEXT NOT NULL );
   ```
   - `Id` → 约定自动成主键 + 自增
   - 表名 `Readings`（复数 = `DbSet` 属性名）
   - `decimal` 在 SQLite 存成 `TEXT`（无原生 decimal，文本保精度）

2. **插入** —— 参数化 `INSERT ... VALUES (@p0)` → 天然防 **SQL 注入**（值走参数通道，不拼字符串）。

3. **查询** —— 高光时刻 🌟：
   ```csharp
   db.Readings.Where(r => r.Celsius > 30m).OrderByDescending(r => r.Celsius)
   ```
   翻译成：
   ```sql
   SELECT "r"."Id", "r"."Celsius" FROM "Readings" AS "r"
   WHERE ef_compare("r"."Celsius", '30.0') > 0
   ORDER BY "r"."Celsius" ... DESC
   ```
   只回 2 行（35.5 / 31.0）；18.0 / 22.5 **根本没离开数据库** = 远程过滤省带宽。

---

## 🧠 Day 53 新口诀（编号续 #73）

| # | 口诀 |
|---|---|
| **74** | **`DbSet<T> : IQueryable<T>` —— 这一行继承关系，就是 LINQ 能变成 SQL 的全部秘密。** |
| **75** | `DbSet<T>` 是 EF 在 **DLL 里**定义的表入口类；`Set<T>()` 是 **`DbContext` 基类**上「把表入口取出来」的方法（不是 DbSet 的构造）。 |
| **76** | **类 = 行的模板，`DbSet<类>` = 整张表。复数属性名 → 表名。** （`DbSet<Reading>` 之于 `Reading` ≈ `List<Reading>` 之于 `Reading`） |
| **77** | **`AsEnumerable()` 是一道「翻译截止线」** —— 线之前的链子能进 SQL，线之后全在内存。误用 = 全表拉回 + 内存过滤（高频性能 bug）。 |
| **78** | EF 靠 **「快照对比（snapshot comparison）」** 知道你改了什么 —— 查询时拍快照，`SaveChanges` 时逐字段 diff，只为变了的实体生成 SQL。（明天验证）|

累计口诀 **78** 条。

---

## 📂 今天产出文件

```
2026-05-30/
  EFCore-FirstQuery/
    Program.cs            ← 完整可跑 demo（建库 + 插入 + 查询 + 打印 SQL）
    EFCore-FirstQuery.csproj  ← 引用 Microsoft.EntityFrameworkCore.Sqlite 10.0.8
    sensor.db             ← 运行生成的 SQLite 文件
  EFCore-Fundamentals-Summary.md  ← this file
  Current-Learning-Status.md
```

包：`Microsoft.EntityFrameworkCore.Sqlite` 10.0.8（拉入 EF Core core）。SDK：.NET 10.0.108。

---

## 🅿️ 明天 Resume Point（Day 54）

**讲到一半、明天接着做的：变更追踪（change tracking）实操验证**

1. **验证「只改属性不调 `Update`」也能存进库** —— 跑这段看 EF 是否自动 diff 出 `UPDATE`：
   ```csharp
   using var db = new SensorDbContext();
   var r = db.Readings.First();   // 查出来，EF 拍快照
   r.Celsius = 99.9m;             // 只改内存对象，没调 db.Update(r)
   db.SaveChanges();              // EF 快照对比 → 自动生成 UPDATE？
   ```
   （陷阱点：很多人以为不调 `Update` 就不会存，其实追踪实体会自动 diff。）

2. **对比 `AsNoTracking()`** —— 只读场景关掉追踪省内存：追踪有代价（每个实体存快照 + 每次 diff）。

3. **未答的检查题**（明天先回答）：
   > `Set<Reading>()` 定义在 `DbSet<Reading>` 还是 `DbContext` 上？为什么 `SensorDbContext` 能不加前缀直接调用它？
   > （答案方向：定义在 `DbContext` 基类上；因为 `SensorDbContext : DbContext` 继承了它，是继承来的实例方法，所以裸调。）

**之后的落地路（Day 55+）**：把这套 EF Core 接到现有 `TemperatureSensor.Api`（用 `AddDbContext` DI 注册），让 minimal API 从「假数据」升级成「真 SQLite 数据库」。

---

## 🏁 Today's One-Line Win

> **"第一次亲眼看到 `.Where(r => r.Celsius > 30m)` 在控制台变成 `SELECT ... WHERE ...` —— Day 52 的 `IQueryable` 理论今天落地成真实 SQL。"**
