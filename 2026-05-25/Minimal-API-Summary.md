# Minimal API + Production Patterns — Daily Summary (2026-05-25)

> Built `TemperatureSensor.Api` end-to-end: a real ASP.NET Core minimal API that hosts
> `TemperatureSensor.Core` as a library, then layered on three production patterns —
> structured logging, startup event subscriptions, and externalized configuration.

---

## 1. What landed today

### 1.1 The base API (Steps 1–7 recipe)

```text
1. Scaffold        dotnet new web -n TemperatureSensor.Api --framework net9.0
2. Wire deps       dotnet add reference ..\TemperatureSensor.Core\TemperatureSensor.Core.csproj
3. Register        builder.Services.AddSingleton<Sensor>(...)
4. Build           var app = builder.Build();
5. Configure       app.MapGet / app.MapPost
6. Run             app.Run();
7. Test            curl POST /readings
```

3 probes verified: 25°C → 200, NaN → 400, 100°C → 200.

### 1.2 Stage 1 — `ILogger<T>` injection

- Added `ILogger<Program>` as a parameter to the endpoint lambda.
- DI resolved `ILogger<T>` via the **开放泛型注册（open generic registration）**: the framework
  registered `ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>))`. The container
  closes the open generic at resolve time for any `T` you ask for.
- Switched from `Console.WriteLine` to **结构化日志（structured logging）**:
  ```csharp
  logger.LogInformation("Recorded reading: {Celsius}°C", dto.Celsius);
  ```
- Observed: `{Celsius}` is a **named placeholder（named placeholder）**, not `string.Format`. It survives
  as a queryable field in JSON sinks (App Insights, Seq, Datadog).

### 1.3 Stage 2 — Startup event subscription (composition root)

- Subscribed `sensor.ThresholdExceeded` ONCE between `builder.Build()` and `app.Run()`.
- This is the **组合根（composition root）**: the one place where the app wires
  long-lived dependencies before requests start.
- Pulled the sensor with `app.Services.GetRequiredService<Sensor>()` (strict throw if missing) and
  captured `startupLogger` in the lambda's closure so logging keeps working long after the
  enclosing scope exits.
- Added a second startup handler to show multicast delegates fire in registration order.
  Predictable: **2 startup subscriptions = 2 warn lines per above-threshold reading**, forever.

### 1.4 Bonus — In-endpoint subscription anti-pattern

Demonstrated the memory leak:

```csharp
app.MapPost("/readings", (Sensor sensor, ILogger<Program> logger, ReadingDto dto) =>
{
    // ❌ NEVER do this:
    sensor.ThresholdExceeded += (s, e) => logger.LogWarning("Handler fired");
    sensor.RecordReading(dto.Celsius);
    return Results.Ok();
});
```

Math (locked-in):

| Above-threshold request # | Handlers attached now | Warn lines emitted |
|---|---|---|
| 1 | 1 | 1 |
| 2 | 2 | 2 |
| 3 | 3 | 3 |
| 4 | 4 | 4 |
| **Total** | — | **1 + 2 + 3 + 4 = 10** |

`+=` always appends. Multicast delegate compares by reference identity, not IL body, so even
identical-looking lambdas don't deduplicate. Sum-of-handlers grows linearly per request → memory leak.

### 1.5 Stage 3 — `appsettings.json` + `IConfiguration`

- Moved the hardcoded `threshold: 30.0` to `appsettings.json`:
  ```json
  { "Sensor": { "Threshold": 30.0 } }
  ```
- Read it in `Program.cs`:
  ```csharp
  double threshold = builder.Configuration.GetValue<double>("Sensor:Threshold");
  ```
- Key syntax: `:` is the **层级分隔符（hierarchy separator）** in config keys. JSON nesting
  `Sensor → Threshold` maps to `"Sensor:Threshold"`.
- **`builder.Configuration` walks all registered providers**, last-registered-wins. Default chain:
  ```
  appsettings.json → appsettings.{Env}.json → env vars → CLI args
  ```
- Proved the chain by setting `$env:Sensor__Threshold = "50"` and restarting:
  startup log printed `loaded from config: 50°C`. POST `celsius=35` produced ZERO warnings
  because `35 < 50`. Same binary, different threshold, no recompile.

---

## 2. Memory diagrams

### 2.1 DI lifecycle (what fires when)

```
PHASE 1: REGISTRATION
  builder.Services.AddSingleton<Sensor>(sp => new Sensor(threshold));
  ↳ stores a ServiceDescriptor in a list. No ctor fires.

PHASE 2: BUILD
  var app = builder.Build();
  ↳ creates the ServiceProvider. Still no ctor fires for transients / scoped / non-resolved.

PHASE 3: RESOLVE
  app.Services.GetRequiredService<Sensor>()  ← THIS triggers ctor.
  (Singleton: built once, cached. Scoped: per request. Transient: every call.)

PHASE 4: USE
  sensor.RecordReading(...)
```

### 2.2 Configuration provider chain (later wins)

```
┌─ JSON file ──┐    ┌─ JSON {Env} ──┐    ┌─ Env vars ──┐    ┌─ CLI args ──┐
│ Threshold:30 │ →  │ (none)        │ →  │ Threshold:50│ →  │ (none)      │
└──────────────┘    └───────────────┘    └─────────────┘    └─────────────┘
                                                ↑
                                  GetValue<double>("Sensor:Threshold")
                                  walks RIGHT-TO-LEFT, returns first hit = 50
```

### 2.3 Event subscription placement

```
sensor.ThresholdExceeded += handler

  Placed in COMPOSITION ROOT (startup)     →  ✅ wired once, lifetime of app
  Placed in ENDPOINT BODY                  →  ❌ multicast list grows per request → leak
```

### 2.4 Open generic resolution

```
Registry:   ILogger<>  →  Logger<>      (open generic, T not yet bound)

Resolve:    sp.GetService<ILogger<Program>>()
              ↓
            Container closes the open generic at T = Program
              ↓
            Returns new Logger<Program>(loggerFactory)
              ↓
            category = "Program" (so logs are tagged this way)
```

---

## 3. Key takeaways (locked in)

1. **API = signature + semantics.** The compiler only checks signature; the deployment contract
   (idempotency, status codes) lives in semantics. HTTP just adds *partial failure* as a third
   possible outcome between "called" and "not called."
2. **Composition root** is THE place for startup wiring. Endpoints handle requests; they don't
   wire dependencies.
3. **`+=` on events always appends.** No dedup. Where you write `+=` decides whether you've made
   a one-time wire or a leak.
4. **`builder.Configuration` is a provider chain.** One read, many sources, last-registered wins.
   Env vars override JSON because they're loaded LATER, not because they're "stronger."
5. **Config externalization saves the rebuild, not the restart.** Use `IOptionsMonitor<T>` if you
   need hot-reload without restart. Maturity ladder:
   ```
   hardcoded → GetValue → IOptions<T> → IOptionsMonitor<T> → feature flag service
   ```
6. **Structured logging vs string interpolation.** `LogInformation("x={X}", x)` keeps `X` as a
   queryable field downstream. `LogInformation($"x={x}")` loses the structure forever.
7. **`GetRequiredService<T>` throws if missing; `GetService<T>` returns null.** Prefer strict for
   known-registered services so failures surface at startup, not deep in a request.

---

## 4. Final `Program.cs` (reference)

```csharp
using Sensor = TemperatureSensor.Core.TemperatureSensor;

var builder = WebApplication.CreateBuilder(args);

double threshold = builder.Configuration.GetValue<double>("Sensor:Threshold");
builder.Services.AddSingleton<Sensor>(sp => new Sensor(threshold: threshold));

var app = builder.Build();

var sensor = app.Services.GetRequiredService<Sensor>();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Sensor threshold loaded from config: {Threshold}°C", threshold);

sensor.ThresholdExceeded += (sender, args) =>
    startupLogger.LogWarning(
        "🔥 THRESHOLD EXCEEDED at {Timestamp:HH:mm:ss}: reading={Reading}°C threshold={Threshold}°C",
        args.Timestamp, args.Reading, args.Threshold);

app.MapGet("/", () => "Hello World!");

app.MapPost("/readings", (Sensor sensor, ILogger<Program> logger, ReadingDto dto) =>
{
    try
    {
        sensor.RecordReading(dto.Celsius);
        logger.LogInformation("Recorded reading: {Celsius}°C", dto.Celsius);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning("Rejected reading {Celsius}: {Reason}", dto.Celsius, ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

record ReadingDto(double Celsius);
```

---

## 5. Review questions (use these for "quiz me" next session)

### DI & open generics

1. In what phase does `new TemperatureSensor(...)` actually run — REGISTRATION, BUILD, RESOLVE, or USE?
2. Why can you ask the container for `ILogger<Program>` even though no one registered that exact
   closed type?
3. What's the practical difference between `GetService<T>()` and `GetRequiredService<T>()`? Pick
   which one for a known-registered service and explain why.

### Events & composition root

4. You have `sensor.ThresholdExceeded += handler;` written twice at startup. How many warn lines
   will fire for one above-threshold reading?
5. Same line written inside the POST handler instead. After 5 above-threshold requests, how many
   warn lines fire on the 6th request?
6. Why is the composition root the right home for `+= handler`?

### Configuration

7. What is the **default** provider order in `WebApplication.CreateBuilder(args)`?
8. JSON has `Sensor:Threshold = 30`. Env var has `Sensor__Threshold = 50`. CLI has nothing.
   What does `GetValue<double>("Sensor:Threshold")` return, and why?
9. You change `appsettings.json` while the app is running. Does the running app pick up the new
   value? What would you need to use instead if you wanted hot-reload?
10. Why is moving config out of source code valuable even when restart is still required?

### HTTP semantics (from API foundations session)

11. Three buckets: what does each digit family of HTTP status code *mean* in plain English?
12. POST is not idempotent. PUT is. Give one consequence for client retry behavior.

### Structured logging

13. `LogInformation("Recorded reading: {Celsius}°C", 25.0)` — why is this materially better than
    `LogInformation($"Recorded reading: {25.0}°C")` for production?

---

## 6. State of the project at end of day

- `TemperatureSensor.Core/` — sensor + event args (unchanged from 2026-05-20)
- `TemperatureSensor.Api/` — minimal API with all 3 production patterns wired, build green
- `Factory-DI-Delegate-Drill/` — earlier foundation drill (Core/DI/ILogger rebuild)
- `appsettings.json` holds the active threshold (30.0)
- No env var left set (verified `Remove-Item Env:Sensor__Threshold`)

## 7. What's next (Session N+3)

- `WebApplicationFactory<TProgram>` for in-process integration tests against the API
- Multi-stage `Dockerfile` for the API container
- Then: GitHub Actions CI, deploy to Azure (App Service or Container Apps)
