using Microsoft.EntityFrameworkCore;

// 直接 new 一个基类 DbContext，不继承、不声明任何 DbSet 属性
var opts = new DbContextOptionsBuilder<DbContext>()
    .UseSqlite("Data Source=naive.db")
    .Options;

var db = new DbContext(opts);

Console.WriteLine("=== 实验 1：看看模型（model）里有几个 entity ===");
var entityCount = db.Model.GetEntityTypes().Count();
Console.WriteLine($"Entity types in model: {entityCount}");
Console.WriteLine();

Console.WriteLine("=== 实验 2：直接调用 db.Set<Reading>() ===");
try
{
    var dbset = db.Set<Reading>();
    Console.WriteLine($"成功拿到 DbSet：{dbset}");
}
catch (Exception ex)
{
    Console.WriteLine($"💥 {ex.GetType().Name}");
    Console.WriteLine($"   {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("=== 实验 3：拿到 DbSet 后，真正用它 ===");
try
{
    var dbset = db.Set<Reading>();
    var list = dbset.ToList();   // ← 真正去查数据库
    Console.WriteLine($"查到 {list.Count} 条记录");
}
catch (Exception ex)
{
    Console.WriteLine($"💥 {ex.GetType().Name}");
    Console.WriteLine($"   {ex.Message}");
}

public class Reading
{
    public int Id { get; set; }
    public double Celsius { get; set; }
}

