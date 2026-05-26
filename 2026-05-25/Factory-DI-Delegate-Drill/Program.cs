// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;


Console.WriteLine("Hello, World!");

Console.WriteLine("=== 1. Manual immediate: new AWithB(new B()) ===");
AWithB manualImmediate = new AWithB(new B());

Console.WriteLine();

Console.WriteLine("=== 2. Manual delayed: new AWithFactory(() => new B()) ===");
AWithFactory manualDelayed = new AWithFactory(() => new B());
Console.WriteLine("AWithFactory has been created. B has not been created yet.");
manualDelayed.UseB();

Console.WriteLine();

Console.WriteLine("=== 3. DI automatic factory: AddTransient<AWithB>() ===");
ServiceCollection autoServices = new ServiceCollection();

autoServices.AddTransient<B>();
//autoServices.AddSingleton<B>();
//autoServices.AddTransient<AWithB>();
autoServices.AddSingleton<AWithB>();

Console.WriteLine("Registrations finished.");

IServiceProvider autoProvider = autoServices.BuildServiceProvider();

Console.WriteLine("Provider built.");

///AWithB autoResolved = autoProvider.GetRequiredService<AWithB>();
AWithB autoResolved1 = autoProvider.GetRequiredService<AWithB>();
AWithB autoResolved2 = autoProvider.GetRequiredService<AWithB>();

Console.WriteLine();

Console.WriteLine("=== 4. DI manual factory: AddTransient<AWithB>(p => new AWithB(...)) ===");
ServiceCollection manualServices = new ServiceCollection();

manualServices.AddTransient<B>();
manualServices.AddTransient<AWithB>(p => new AWithB(p.GetRequiredService<B>()));

Console.WriteLine("Registrations finished.");

IServiceProvider manualProvider = manualServices.BuildServiceProvider();

Console.WriteLine("Provider built.");

AWithB manualResolved = manualProvider.GetRequiredService<AWithB>();

Console.WriteLine();

Console.WriteLine("=== 5. DI delayed factory: new AWithFactory(() => p.GetRequiredService<B>()) ===");
ServiceCollection delayedServices = new ServiceCollection();

delayedServices.AddTransient<B>();
delayedServices.AddTransient<AWithFactory>(p =>
    new AWithFactory(() => p.GetRequiredService<B>()));

Console.WriteLine("Registrations finished.");

IServiceProvider delayedProvider = delayedServices.BuildServiceProvider();

Console.WriteLine("Provider built.");

AWithFactory delayedResolved = delayedProvider.GetRequiredService<AWithFactory>();

Console.WriteLine("AWithFactory resolved. B has not been resolved yet.");

delayedResolved.UseB();


Console.WriteLine();

Console.WriteLine("=== 6. DI scoped lifetime: AddScoped<ScopedB>() ===");
ServiceCollection scopedServices = new ServiceCollection();

scopedServices.AddScoped<ScopedB>();

Console.WriteLine("Registrations finished.");

IServiceProvider scopedProvider = scopedServices.BuildServiceProvider();

Console.WriteLine("Provider built.");

Guid scope1Id;
Guid scope2Id;

using (IServiceScope scope1 = scopedProvider.CreateScope())
{
    Console.WriteLine("Scope 1 created.");

    ScopedB scopedB1 = scope1.ServiceProvider.GetRequiredService<ScopedB>();
    ScopedB scopedB2 = scope1.ServiceProvider.GetRequiredService<ScopedB>();

    scope1Id = scopedB1.Id;

    Console.WriteLine($"Scope 1 same instance: {ReferenceEquals(scopedB1, scopedB2)}");
}

using (IServiceScope scope2 = scopedProvider.CreateScope())
{
    Console.WriteLine("Scope 2 created.");

    ScopedB scopedB1 = scope2.ServiceProvider.GetRequiredService<ScopedB>();
    ScopedB scopedB2 = scope2.ServiceProvider.GetRequiredService<ScopedB>();

    scope2Id = scopedB1.Id;

    Console.WriteLine($"Scope 2 same instance: {ReferenceEquals(scopedB1, scopedB2)}");
}

Console.WriteLine($"Scope 1 and Scope 2 same id: {scope1Id == scope2Id}");


public sealed class ScopedB
{
    public Guid Id { get; } = Guid.NewGuid();

    public ScopedB()
    {
        Console.WriteLine($"ScopedB constructor: {Id}");
    }
}














public sealed class B
{
    public B()
    {
        Console.WriteLine("B constructor");
    }
}

public sealed class AWithB
{
    public AWithB(B b)
    {
        Console.WriteLine("AWithB constructor with B");
    }
}

public sealed class AWithFactory
{
    private readonly Func<B> _createB;

    public AWithFactory(Func<B> createB)
    {
        _createB = createB;
        Console.WriteLine("AWithFactory constructor with Func<B>");
    }

    public void UseB()
    {
        B b = _createB();
        Console.WriteLine("AWithFactory used B");
    }
}