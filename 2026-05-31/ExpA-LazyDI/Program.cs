using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("[1] About to call AddSingleton<Bomb>() ...");
var services = new ServiceCollection();
services.AddSingleton<Bomb>();          // ❶ 注册 — Bomb 的 ctor 还没跑
Console.WriteLine("[1] AddSingleton<Bomb>() returned. No ctor ran. ✅");

Console.WriteLine();
Console.WriteLine("[2] About to call BuildServiceProvider() ...");
var provider = services.BuildServiceProvider();
Console.WriteLine("[2] BuildServiceProvider() returned. Still no ctor ran. ✅");

Console.WriteLine();
Console.WriteLine("[3] About to call GetRequiredService<Bomb>() ...");
var b = provider.GetRequiredService<Bomb>();   // ❷ 这一行才会炸 — Bomb 的 ctor 真的跑
Console.WriteLine("[3] You should NEVER see this line.");

// ----------- 普通的服务类，构造函数里直接抛异常 -----------
public sealed class Bomb
{
    public Bomb()
    {
        Console.WriteLine("    >>> Bomb ctor is running NOW <<<");
        throw new InvalidOperationException("BOOM — Bomb ctor exploded.");
    }
}
