using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("[1] About to call AddDbContext<SensorDbContext>(...) ...");
var services = new ServiceCollection();

services.AddDbContext<SensorDbContext>(opts =>            // ← line 7
    opts.UseSqlite("Data Source=expB.db"));

Console.WriteLine("[1] AddDbContext returned. Reflection check passed. ✅");
Console.WriteLine("    (EF Core peeked at SensorDbContext's ctors and found ctor(DbContextOptions<SensorDbContext>))");

Console.WriteLine();
Console.WriteLine("[2] About to call BuildServiceProvider() ...");
var provider = services.BuildServiceProvider();
Console.WriteLine("[2] BuildServiceProvider returned. Still no ctor ran. ✅");

Console.WriteLine();
Console.WriteLine("[3] About to call GetRequiredService<SensorDbContext>() ...");
var db = provider.GetRequiredService<SensorDbContext>();
Console.WriteLine("[3] Got SensorDbContext. NOW the ctor really ran. ✅");

db.Database.EnsureCreated();
db.Readings.Add(new Reading { Celsius = 25.0 });
db.SaveChanges();

Console.WriteLine();
Console.WriteLine("Saved! All three phases passed. 🎯");

// ===================== 模型 =====================

public class SensorDbContext : DbContext
{
    // ★ 关键差异：这里加了 ctor —— EF Core 的反射体检看的就是这个签名
    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options)
    {
        Console.WriteLine("    >>> SensorDbContext ctor is running NOW <<<");
    }

    public DbSet<Reading> Readings => Set<Reading>();
}

public class Reading
{
    public int Id { get; set; }
    public double Celsius { get; set; }
}
