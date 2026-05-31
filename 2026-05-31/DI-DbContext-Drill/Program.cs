using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ============================================================
// Step 4：正确版本 —— SensorDbContext 有 : base(options) ctor
// 预期：注册通过 ✅，resolve 成功 ✅，查询能跑 ✅
// ============================================================

var services = new ServiceCollection();

// 注册：告诉 DI 容器"怎么配置 options"
services.AddDbContext<SensorDbContext>(opts =>
    opts.UseSqlite("Data Source=drill.db"));

// 构建 DI 容器（这一行还不会 new SensorDbContext）
var provider = services.BuildServiceProvider();

Console.WriteLine("[Phase 1] Container built. SensorDbContext NOT yet created.");

// 关键时刻：resolve —— DI 容器现在真的去 new SensorDbContext
Console.WriteLine("[Phase 2] About to resolve SensorDbContext...");
//using var scope = provider.CreateScope();
//var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
var db = provider.GetRequiredService<SensorDbContext>();
Console.WriteLine("[Phase 3] Resolved! DI 把配置好的 options 注入进来了。");

// 真正用一下，证明配置是真的传到 base 了
db.Database.EnsureCreated();
db.Readings.Add(new Reading { Celsius = 21.5 });
db.SaveChanges();
Console.WriteLine($"[Phase 4] Saved. Total readings now: {db.Readings.Count()}");

// ============================================================
// 实体 + DbContext
// ============================================================
public class Reading
{
    public int Id { get; set; }
    public double Celsius { get; set; }
}

public class SensorDbContext : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();

    // ✅ 接收 DI 给的配置包裹，并用 : base(options) 转交给基类 DbContext
    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options)
    {
    }
}

