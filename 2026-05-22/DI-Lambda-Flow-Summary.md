# DI-Lambda Learning Summary (2026-05-22)

## 1) Concepts Covered

### A. Lambda fundamentals
- lambda expression is an anonymous function passed as a delegate value.
- expression lambda vs statement lambda:
  - expression lambda: `x => x * 2`
  - statement lambda: `x => { Console.WriteLine(x); return x * 2; }`
- `Action<T>` returns `void`; `Func<TIn, TOut>` returns value.
- parameter parentheses rules:
  - 0 params: `()` required
  - 1 param: parentheses optional
  - 2+ params: parentheses required
- `clock` vs `clock()`:
  - `clock` is delegate variable
  - `clock()` is invocation result

### B. Delegate/callback mental model
- passing a value vs passing an action:
  - value pass: `int n`
  - action pass: `Func<int, int> transform`
- callback parameter (e.g., `builder` in `AddLogging`) is defined by lambda and invoked by callee.
- lambda does not run at declaration; it runs when invoked.

### C. LINQ and deferred execution
- `Where` takes `Func<T, bool>`.
- `Select` takes `Func<T, TResult>`.
- `ToList()` materializes query immediately.
- without `ToList()`, query is deferred and runs during enumeration.

### D. DI core flow
- registration phase: write rules only, no object creation.
- resolution phase: container creates objects and injects constructor dependencies.
- container creates object graph recursively from constructor signatures.

### E. Service lifetimes
- Transient lifetime: new instance per resolve.
- Singleton lifetime: one instance per provider.
- Scoped lifetime: one instance per scope, different across scopes.

### F. Interface mapping and dependency graph
- `OrderService` depends on abstraction `IInventory` (not concrete class).
- required registration mapping example:
  - `services.AddTransient<IInventory, FakeInventory>();`
- missing mapping makes dependency graph incomplete and causes unable to resolve at runtime.

### G. Class/property syntax clarifications
- auto-property initializer is valid:
  - `public List<string> Items { get; } = new();`
- this is not setter assignment; it initializes backing field during object construction.
- `new()` target-typed syntax requires parentheses.

## 2) Key Takeaways
- Registering is not constructing; resolving is constructing.
- Lifetime controls reuse policy, not call count.
- In Transient, `order1` and `order2` differ only because there are two resolves.
- Interface mapping is the bridge between abstraction and concrete implementation.
- DI is not magic; it is deterministic constructor-chain resolution.

## 3) Memory Diagrams

### A. DI end-to-end flow

```text
ServiceCollection (rules only)
  -> AddLogging / AddTransient / AddScoped / AddSingleton
  -> BuildServiceProvider()
  -> GetRequiredService<T>()
  -> resolve constructor parameters recursively
  -> create object graph
  -> return requested root service
```

### B. OrderService resolution graph

```text
GetRequiredService<OrderService>()
  -> needs ILogger<OrderService>
  -> needs IInventory
       -> mapped to FakeInventory (or RealInventory)
  -> construct OrderService(logger, inventory)
```

### C. Lifetime behavior

```text
Transient: Resolve A, Resolve B => A != B
Singleton: Resolve A, Resolve B => A == B
Scoped: (same scope) A == B; (different scopes) A != B
```

## 4) Code Examples From Today

```csharp
// Lambda as callback parameter
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

```csharp
// Interface mapping registration
services.AddTransient<OrderService>();
services.AddTransient<IInventory, FakeInventory>();
```

```csharp
// Constructor injection
public sealed class OrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IInventory _inventory;

    public OrderService(ILogger<OrderService> logger, IInventory inventory)
    {
        _logger = logger;
        _inventory = inventory;
    }
}
```

```csharp
// Delegate variable vs invocation result
Func<DateTime> clock = () => DateTime.Now;
var fn = clock;   // delegate variable
var now = clock(); // invocation result
```

## 5) Next-Day Review Questions (organized by topic)

### Topic A: Lambda and delegates
1. What is the difference between expression lambda and statement lambda?
2. Why can `Action<T>` lambda omit return value, while `Func<T, TResult>` cannot?
3. What is the difference between `clock` and `clock()`?
4. In `ApplyTo100(x => x * x)`, when does `x` receive `100`?

### Topic B: Callback model
5. In `AddLogging(builder => { ... })`, who defines `builder`, and who invokes the lambda?
6. Why is this called passing action instead of passing value?

### Topic C: LINQ
7. Why does `.ToList()` change execution timing?
8. What happens if you enumerate the same deferred query twice?

### Topic D: DI flow
9. Which line is registration phase and which line is resolution phase?
10. Why does container fail with "Unable to resolve service for type IInventory"?
11. Explain object graph creation for resolving `OrderService`.

### Topic E: Lifetimes
12. Why are `order1` and `order2` different in Transient?
13. In Singleton, why do repeated resolves return the same instance?
14. How do you verify Scoped behavior in a console app?

### Topic F: Design choices
15. Why should `OrderService` depend on `IInventory` instead of `FakeInventory` directly?
16. Why are `IInventory`, `FakeInventory`, and `RealInventory` usually separated into different files?

## 6) Suggested Warm-up For Tomorrow (10 minutes)
1. Say the 4 anchor sentences aloud:
   - registration is not construction.
   - resolution is construction.
   - lifetime defines reuse policy.
   - abstraction + mapping defines implementation choice.
2. Run current project once and explain each log block with DI flow.
3. Switch mapping `IInventory` between Fake/Real and predict output before run.
