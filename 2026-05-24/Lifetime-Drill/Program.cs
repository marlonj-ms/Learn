using Lifetime_Drill;
using Microsoft.Extensions.DependencyInjection;

// =====================================================================
// LIFETIME DRILL -- Transient / Singleton / Scoped side-by-side
// Each block builds its OWN ServiceProvider so the lifetime is isolated.
// Watch the [ctor] lines: that's when "new ProbeService()" actually runs.
// =====================================================================

Console.WriteLine("============================================================");
Console.WriteLine(" LIFETIME DRILL");
Console.WriteLine("============================================================");

// ---- A) TRANSIENT -- expect: 2 ctor calls, a.Id != b.Id ----
{
    Console.WriteLine();
    Console.WriteLine("---- A) Transient (new instance per resolve) ----");
    var sc = new ServiceCollection();
    sc.AddTransient<ProbeService>();
    using var sp = sc.BuildServiceProvider();
    var a = sp.GetRequiredService<ProbeService>();
    var b = sp.GetRequiredService<ProbeService>();
    Console.WriteLine($"  a.Id == b.Id ? {a.Id == b.Id}    (expect: False)");
}

// ---- B) SINGLETON -- expect: 1 ctor call only, a.Id == b.Id ----
{
    Console.WriteLine();
    Console.WriteLine("---- B) Singleton (one instance for the provider's life) ----");
    var sc = new ServiceCollection();
    sc.AddSingleton<ProbeService>();
    using var sp = sc.BuildServiceProvider();
    var a = sp.GetRequiredService<ProbeService>();
    var b = sp.GetRequiredService<ProbeService>();
    Console.WriteLine($"  a.Id == b.Id ? {a.Id == b.Id}    (expect: True)");
}

// ---- C) SCOPED -- expect: same WITHIN a scope, different ACROSS scopes ----
{
    Console.WriteLine();
    Console.WriteLine("---- C) Scoped (one instance per scope) ----");
    var sc = new ServiceCollection();
    sc.AddScoped<ProbeService>();
    using var sp = sc.BuildServiceProvider(
        new ServiceProviderOptions { ValidateScopes = true });

    Guid idFromScope1;
    using (var scope1 = sp.CreateScope())
    {
        var a = scope1.ServiceProvider.GetRequiredService<ProbeService>();
        var b = scope1.ServiceProvider.GetRequiredService<ProbeService>();
        Console.WriteLine($"  scope1: a.Id == b.Id ? {a.Id == b.Id}    (expect: True)");
        idFromScope1 = a.Id;
    }

    using (var scope2 = sp.CreateScope())
    {
        var c = scope2.ServiceProvider.GetRequiredService<ProbeService>();
        Console.WriteLine($"  scope2: c.Id == scope1.a.Id ? {c.Id == idFromScope1}    (expect: False)");
    }
}
 