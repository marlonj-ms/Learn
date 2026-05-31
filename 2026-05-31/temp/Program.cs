using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDbContext<SensorDbContext>(opts =>
    opts.UseSqlite("Data Source=sensor.db"));

var provider = services.BuildServiceProvider();

var db = provider.GetRequiredService<SensorDbContext>();   // ← 嫌疑点之一
db.Database.EnsureCreated();
db.Readings.Add(new Reading { Celsius = 25.0 });
db.SaveChanges();

Console.WriteLine("Saved!");