# Current C# Learning Status — May 25, 2026 (End of Day)

## Today's arc (actual)

Morning hesitation about jumping back into Web → did `Factory-DI-Delegate-Drill/` to re-anchor
Core + DI + ILogger fundamentals → returned to the Web track in the afternoon and finished
the minimal API end-to-end, plus the three production-pattern enrichment stages.

Result: the "0→1 production C# software" milestone is essentially met for this learning track.

## What landed (2026-05-25)

### TemperatureSensor.Api — base minimal API ✅
- `dotnet new web -n TemperatureSensor.Api --framework net9.0`
- References `TemperatureSensor.Core` from 2026-05-20
- `POST /readings` accepts a `ReadingDto`, calls `Sensor.RecordReading`, returns 200/400
- 3 probes verified: 25°C → 200, NaN → 400, 100°C → 200

### Stage 1 — ILogger<Program> injection ✅
- Open-generic registration `ILogger<>` → `Logger<>` understood
- Structured logging with `{Celsius}` named placeholders
- Replaced `Console.WriteLine` everywhere

### Stage 2 — Startup event subscription (composition root) ✅
- `sensor.ThresholdExceeded += handler;` wired ONCE between Build() and Run()
- Captured `startupLogger` in closure
- Added second handler to show multicast registration-order firing

### Bonus — In-endpoint subscription anti-pattern ✅
- Showed the linear memory-leak math (1+2+3+4=10 warn lines)
- Locked in: "where you write `+=` decides whether it's a wire or a leak"

### Stage 3 — appsettings.json + IConfiguration ✅
- JSON file holds default `Sensor:Threshold = 30.0`
- `builder.Configuration.GetValue<double>(...)` walks the provider chain
- Env var override demo proved layered config: `$env:Sensor__Threshold = "50"`
- Cleaned up env var at end of day

## 3-Session Arc Status

- **Session N+1: DI + ILogger + Lifetimes** — ✅ done (closed 2026-05-24)
- **Session N+2: Minimal REST API** — ✅ **DONE TODAY** (base + 3 enrichment stages)
- **Session N+3: Integration tests + Docker** — ⬜ next

## Key concepts now solid

- 7-step API recipe (Scaffold → Wire → Register → Build → Configure → Run → Test)
- Composition root pattern
- Open generic DI (`ILogger<>` → `Logger<>`)
- Structured logging vs string interpolation
- Multicast delegate accumulation math
- 12-factor configuration (Rule III): same binary, config from environment
- Provider chain walk order (later-registered wins)
- Maturity ladder for config: hardcoded → GetValue → IOptions → IOptionsMonitor → feature flag service
- HTTP semantics: signature + semantics, partial failure, idempotency table, 2xx/4xx/5xx mental model

## Files of record

- `Minimal-API-Summary.md` — full daily summary with diagrams + 13 review questions
- `TemperatureSensor.Api/Program.cs` — final state, build green
- `TemperatureSensor.Api/appsettings.json` — holds `Sensor:Threshold = 30.0`
- `Factory-DI-Delegate-Drill/` — foundation drill from earlier today

## Next session opens with

```
Session N+3 — Integration tests + Docker
  1. dotnet new xunit -n TemperatureSensor.Api.Tests
  2. Add Microsoft.AspNetCore.Mvc.Testing package
  3. Make Program.cs class-accessible (partial Program {})
  4. WebApplicationFactory<Program> spins up the API in-process
  5. HttpClient → /readings → assert status code + log assertions
  6. Then: multi-stage Dockerfile
```

Or alternatively, "quiz me" against `Minimal-API-Summary.md` review questions before moving on.

## Important reminder

`Current-Learning-Status.md` was reset this morning with a "rebuild the foundation first"
plan. The foundation work happened (Factory-DI-Delegate-Drill) AND the Web track resumed
successfully. Today's actual outcome exceeded the morning reset's goal.
