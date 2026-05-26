using DI_Logging_MinHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// =====================================================================
//  PHASE 1: REGISTRATION -- writing the rule book.
//  Nothing is constructed here. We are filling a list.
// =====================================================================
Console.WriteLine("==================== PHASE 1: REGISTRATION ====================");

var services = new ServiceCollection();
Console.WriteLine("[REG] new ServiceCollection() -- empty rule book created");

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
Console.WriteLine("[REG] + Logging infrastructure (Console)");

services.AddTransient<TemperatureSensorService>();
Console.WriteLine("[REG] + TemperatureSensorService (Transient)");

services.AddTransient<OrderService>();
Console.WriteLine("[REG] + OrderService (Transient)");

services.AddTransient<IInventory, FakeInventory>();
Console.WriteLine("[REG] + IInventory -> FakeInventory (Transient)");

Console.WriteLine($"[REG] Rule book ready. Registrations recorded: {services.Count}");
Console.WriteLine("[REG] NOTE: zero constructors have run yet.");

// =====================================================================
//  PHASE 2: BUILD -- freeze the rule book into a resolver.
//  Still nothing is constructed.
// =====================================================================
Console.WriteLine();
Console.WriteLine("==================== PHASE 2: BUILD ====================");
Console.WriteLine("[BUILD] Calling services.BuildServiceProvider() ...");

using var provider = services.BuildServiceProvider();

Console.WriteLine("[BUILD] ServiceProvider is alive. Still no constructors.");

// =====================================================================
//  PHASE 3a: RESOLVE #1 -- container builds the dependency graph on demand.
//  Watch the [TRACE] lines below (those come from _logger inside ctors).
// =====================================================================
Console.WriteLine();
Console.WriteLine("==================== PHASE 3a: RESOLVE #1 ====================");
Console.WriteLine("[RESOLVE] Asking provider for OrderService #1 ...");

var order1 = provider.GetRequiredService<OrderService>();

Console.WriteLine("[RESOLVE] Got OrderService. Dependencies built: ILogger, IInventory.");

// =====================================================================
//  PHASE 4a: USE #1
// =====================================================================
Console.WriteLine();
Console.WriteLine("==================== PHASE 4a: USE #1 ====================");
Console.WriteLine("[USE] order1.PlaceOrder(\"Car\", 100)");
order1.PlaceOrder("Car", 100);

Console.WriteLine("[USE] order1.PlaceOrder(\"Pen\", 5)");
order1.PlaceOrder("Pen", 5);

// =====================================================================
//  PHASE 3b: RESOLVE #2 -- Transient, so expect a fresh OrderService.
//  IInventory is also Transient -> expect a NEW FakeInventory too.
// =====================================================================
Console.WriteLine();
Console.WriteLine("==================== PHASE 3b: RESOLVE #2 ====================");
Console.WriteLine("[RESOLVE] Asking provider for OrderService #2 ...");

var order2 = provider.GetRequiredService<OrderService>();

Console.WriteLine($"[RESOLVE] ReferenceEquals(order1, order2) = {ReferenceEquals(order1, order2)}    (expect: False)");

// =====================================================================
//  PHASE 4b: USE #2
// =====================================================================
Console.WriteLine();
Console.WriteLine("==================== PHASE 4b: USE #2 ====================");
Console.WriteLine("[USE] order2.PlaceOrder(\"Notebook\", 500)");
order2.PlaceOrder("Notebook", 500);
