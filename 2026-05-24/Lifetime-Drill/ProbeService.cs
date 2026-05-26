namespace Lifetime_Drill;

/// <summary>
/// Tiny probe used to observe DI lifetime behavior.
/// Each instance gets a unique Guid Id so we can tell instances apart.
/// The constructor logs to console so you can see exactly when "new" happens.
/// </summary>
public class ProbeService
{
    public Guid Id { get; } = Guid.NewGuid();

    public ProbeService()
    {
        Console.WriteLine($"    [ctor] new ProbeService -- Id = {Id}");
    }
}
