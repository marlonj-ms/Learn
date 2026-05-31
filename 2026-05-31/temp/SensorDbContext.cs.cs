using Microsoft.EntityFrameworkCore;

public class SensorDbContext : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();
}

public class Reading
{
    public int Id { get; set; }
    public double Celsius { get; set; }
}