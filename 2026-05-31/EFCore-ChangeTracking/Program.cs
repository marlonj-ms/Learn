// ============================================================================
// Day 54 — EF Core 变更追踪（change tracking）三场景
// ============================================================================
// 目的：亲眼看 EF 的「快照对比（snapshot comparison）」机制
//   场景 A：改成新值        → 期待看到 UPDATE SQL
//   场景 B：set 同值        → 期待【没有】UPDATE SQL（值没变，diff = 空）
//   场景 C：AsNoTracking 后改属性 → 期待【没有】UPDATE SQL（没追踪 = 没快照 = 没 diff）
//
// 关键看点：控制台里 "Executed DbCommand" 后面跟的 SQL —— UPDATE 出现没？
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// ─────────────────────────────────────────────────────────────────────────
// STEP 0 · 重建库 + 种 4 行数据（学习用：每次跑都干净起步）
// ─────────────────────────────────────────────────────────────────────────
using (var db = new SensorDbContext())
{
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    db.Readings.AddRange(
        new Reading { Celsius = 22.5m },   // Id=1
        new Reading { Celsius = 31.0m },   // Id=2
        new Reading { Celsius = 18.0m },   // Id=3
        new Reading { Celsius = 35.5m });  // Id=4

    db.SaveChanges();
}

// ─────────────────────────────────────────────────────────────────────────
// 场景 A · 查出来 → 改成新值 → SaveChanges
//   期待：看到 UPDATE "Readings" SET "Celsius" = @p0 WHERE "Id" = @p1
// ─────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n========== 场景 A：改成新值 ==========");
using (var db = new SensorDbContext())
{
    Reading r = db.Readings.First();                    // Id=1, Celsius=22.5
    Console.WriteLine($"\n[A] 查出来：Id={r.Id}, Celsius={r.Celsius}");
    Console.WriteLine("[A] 此刻 EF 已偷偷给 r 拍了快照（snapshot = 22.5）");

    r.Celsius = 99.9m;                                   // ← 只改内存，没调任何 db.XXX
    Console.WriteLine($"[A] 改内存对象：r.Celsius = 99.9（快照仍是 22.5）");

    Console.WriteLine("[A] 调 SaveChanges() — 看下面有没有 UPDATE SQL ↓");
    db.SaveChanges();
}

// ─────────────────────────────────────────────────────────────────────────
// 场景 B · 查出来 → set 同值 → SaveChanges
//   期待：【没有】UPDATE —— 因为 EF diff 出来"啥也没变"
// ─────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n========== 场景 B：set 同值 ==========");
using (var db = new SensorDbContext())
{
    Reading r = db.Readings.Skip(1).First();             // Id=2, Celsius=31.0
    Console.WriteLine($"\n[B] 查出来：Id={r.Id}, Celsius={r.Celsius}");

    r.Celsius = 31.0m;                                    // ← set 成完全相同的值
    Console.WriteLine($"[B] set 同值：r.Celsius = 31.0（快照也是 31.0）");

    Console.WriteLine("[B] 调 SaveChanges() — 期待 SQL 区域是空的 ↓");
    db.SaveChanges();
    Console.WriteLine("[B] ↑ 如果上面没看到 UPDATE，说明 EF 真的会 diff，不是无脑写库。");
}

// ─────────────────────────────────────────────────────────────────────────
// 场景 C · AsNoTracking() 后改属性 → SaveChanges
//   期待：【没有】UPDATE —— 没追踪 = 没快照 = 没 diff（性能优化 & 反面教材）
// ─────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n========== 场景 C：AsNoTracking 后改属性 ==========");
using (var db = new SensorDbContext())
{
    Reading r = db.Readings
        .AsNoTracking()                                   // ← 关键：告诉 EF 「这次查我不打算改，别给我占内存做快照」
        .Skip(2).First();                                 // Id=3, Celsius=18.0
    Console.WriteLine($"\n[C] AsNoTracking 查出来：Id={r.Id}, Celsius={r.Celsius}");
    Console.WriteLine("[C] EF 这次【没拍快照】，r 是个游离对象（detached entity）");

    r.Celsius = 77.7m;                                    // 改了，但 EF 不知道也不关心
    Console.WriteLine($"[C] 改内存：r.Celsius = 77.7");

    Console.WriteLine("[C] 调 SaveChanges() — 期待 SQL 区域是空的 ↓");
    db.SaveChanges();
    Console.WriteLine("[C] ↑ 没 UPDATE = 数据库里 Id=3 还是 18.0。这就是 AsNoTracking 的代价：");
    Console.WriteLine("    ✔ 优点：省内存（不存快照）+ 查询快");
    Console.WriteLine("    ✘ 代价：你的改动【不会被自动 diff 出来】，得自己 db.Update(r)");
}

// ─────────────────────────────────────────────────────────────────────────
// 最后再查一次，确认数据库实际状态
// ─────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n========== 最终库里的真实状态 ==========");
using (var db = new SensorDbContext())
{
    foreach (var r in db.Readings.AsNoTracking())
        Console.WriteLine($"  Id={r.Id}  Celsius={r.Celsius}");
    Console.WriteLine("\n期待：Id=1→99.9（A改了）  Id=2→31.0（B同值）  Id=3→18.0（C没追踪）  Id=4→35.5（没动）");
}

// ============================================================================
// 实体 + DbContext（跟 Day 53 一样的套路）
// ============================================================================
public class Reading
{
    public int Id { get; set; }
    public decimal Celsius { get; set; }
}

public class SensorDbContext : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options
            .UseSqlite("Data Source=sensor.db")
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging();
    }
}
