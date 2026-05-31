// ============================================================================
// Day 53 — EF Core 第一条查询：亲眼看 IQueryable 链子被翻译成 SQL
// ============================================================================
// 目标：定义 Reading 实体 + SensorDbContext → SQLite 建库 → 插入 → LINQ 查询
//       → 把 EF Core 生成的真实 SQL 打印到控制台。
// 呼应昨天 #71/#73 口诀：DbSet<T> : IQueryable<T>，链子被翻译成 SQL。
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// ─────────────────────────────────────────────────────────────────────────
// STEP 1 · 建库 + 确保是干净的（学习用：每次跑都重建，结果可复现）
// ─────────────────────────────────────────────────────────────────────────
using (var db = new SensorDbContext())
{
    db.Database.EnsureDeleted();   // 删掉上次的 sensor.db
    db.Database.EnsureCreated();   // 按实体定义重新建表（看控制台会有 CREATE TABLE 的 SQL）

    // ─── STEP 2 · 插入几行（INSERT）───
    db.Readings.AddRange(
        new Reading { Celsius = 22.5m },
        new Reading { Celsius = 31.0m },
        new Reading { Celsius = 18.0m },
        new Reading { Celsius = 35.5m });

    db.SaveChanges();   // ← "按按钮" 之一：真正把 INSERT 发到数据库
    Console.WriteLine("\n=== 插入完成，下面开始查询 ===\n");
}

// ─────────────────────────────────────────────────────────────────────────
// STEP 3 · 查询：只要 Celsius > 30 的行
// ─────────────────────────────────────────────────────────────────────────
using (var db = new SensorDbContext())
{
    // 这里的 db.Readings 是 DbSet<Reading>，它实现 IQueryable<Reading>。
    // 所以 .Where(...) 里的 lambda 被编译成【表达式树】，不是普通委托。
    IQueryable<Reading> query = db.Readings
        .Where(r => r.Celsius > 30m)      // ← 还没执行！只是搭管子（#70）
        .OrderByDescending(r => r.Celsius);

    Console.WriteLine("--- 此刻还没有发任何 SQL（query 只是表达式树）---\n");

    // .ToList() = "按按钮" → EF 现在才把整条链翻译成 SQL，发给数据库
    List<Reading> hot = query.ToList();

    Console.WriteLine($"\n=== 查到 {hot.Count} 行（Celsius > 30）===");
    foreach (var r in hot)
        Console.WriteLine($"  Id={r.Id}  Celsius={r.Celsius}");
}

// ============================================================================
// 实体（entity）：一行的形状。每个属性 = 一列。
// ============================================================================
public class Reading
{
    public int Id { get; set; }         // 约定：名为 Id → 自动成为主键（PK），自增
    public decimal Celsius { get; set; }
}

// ============================================================================
// DbContext：一次数据库会话的总管。
//   - DbSet<Reading> Readings  → 整张 Readings 表的入口
//   - OnConfiguring            → 连哪个数据库（这里 SQLite 本地文件）
//   - LogTo(...)               → 把 EF 生成的 SQL 打印到控制台（学习关键！）
// ============================================================================
public class SensorDbContext : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options
            .UseSqlite("Data Source=sensor.db")          // 本地文件数据库
            .LogTo(Console.WriteLine, LogLevel.Information) // 打印 EF 生成的 SQL
            .EnableSensitiveDataLogging();                // 学习用：连参数值也打印
    }
}
