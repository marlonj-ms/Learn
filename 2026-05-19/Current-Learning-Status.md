# Current C# Learning Status - May 19, 2026

## Current Position

The learner has completed the core mental model for:

- LINQ and lambda expressions
- Delegates and callback execution
- Events and the standard .NET event pattern
- Inheritance basics needed for event publishers
- Async/await fundamentals

Current sequence:

```text
LINQ + Lambdas -> Delegates -> Events -> Inheritance basics -> Async/Await -> Async throttling -> Core Syntax -> Testing
```

## New Core Course Added

### C# Core Syntax Foundations

Reason: the learner understands many concepts but wants more fluency writing basic C# code correctly.

Course file:

- `2026-05-19/CSharp-Core-Syntax-Foundations.md`

Focus areas:

- Code placement: class member vs method body vs statement
- Method shape: return type, method name, parameters, method body
- When methods need parameters
- How methods call each other
- Static vs instance basics
- Variables, fields, and parameters
- `List<T>` declaration and common usage
- Small daily syntax drills before larger projects

## Today's Active Topic

### Async Throttling with SemaphoreSlim

Concepts introduced:

- `SemaphoreSlim` as a concurrency throttle
- `WaitAsync()` acquires a permit
- `Release()` returns a permit
- `try/finally` guarantees permit release after a successful acquire
- A gate only controls code that explicitly uses the same `SemaphoreSlim` instance
- Different `SemaphoreSlim` instances are independent gates
- Gate lifetime should match the protected resource lifetime
- A runnable throttling demo was added to `2026-05-18/Async-Demo/Program.cs`

Key mental model:

```text
SemaphoreSlim instance = shared permit pool
same instance = same concurrency limit
different instances = different limits
WaitAsync/Release boundary = throttled region
```

## Today's Second Active Topic

### Events Practice: Doorbell to TemperatureSensor

The original TemperatureSensor event exercise was too large at first, so the learner stepped down to a smaller Doorbell event model and then returned to TemperatureSensor.

Completed:

- `2026-05-19/Mini1-Doorbell/Program.cs`
  - plain event subscription and unsubscription
  - custom `DoorbellEventArgs`
  - `EventHandler<DoorbellEventArgs>`
  - `Press(string presserName)` payload flow
  - `protected virtual OnRang(...)`
  - `LoudDoorbell` / `NoisyDoorbell` overrides and `base.OnRang(e)`
- `2026-05-18/Practice-Exercise/Program.cs`
  - `ThresholdExceededEventArgs`
  - `TemperatureSensor.ThresholdExceeded`
  - `protected virtual OnThresholdExceeded(...)`
  - `Buzzer` and `Logger` subscribers
  - unsubscribe verified: 120 triggers logger only after buzzer is removed

Key event mental model:

```text
event = list of handler methods
+= adds a handler
-= removes a handler
?.Invoke(this, e) notifies all handlers if any exist
```

Key inheritance/event mental model:

```text
business method -> virtual OnXxx -> optional child override -> base.OnXxx -> event subscribers
```

Important corrections:

- A method only needs parameters when it needs information from the caller.
- `EventHandler<TArgs>` and handler signature must agree on the same `TArgs`.
- Direct `Rang?.Invoke(...)` inside `Press(...)` bypasses the virtual `OnRang(...)` hook.
- `Buzzer` and `Logger` are subscribers, not subclasses of `TemperatureSensor`.
- `public double Threshold { get; }` is a get-only property; `public double Threshold;` is a public field.
- Sensor threshold should be `private readonly double _threshold`.

Summary file:

- `2026-05-19/Events-Practice-UnitTest-Prep-Summary.md`

## New Future Topic Added

### Dependency Injection and Service Lifetimes

The learner identified that shared gates depend on shared service instances. Add a future lesson on dependency injection after async basics/testing starts.

Topics to cover later:

- What dependency injection means
- Object instance vs reference sharing
- Constructor injection
- ASP.NET Core service container
- `AddSingleton`, `AddScoped`, `AddTransient`
- How service lifetime affects shared fields such as `SemaphoreSlim`
- When a throttling gate should be method-local, service-scoped, singleton, or avoided

## Next Steps

1. Start next session with a focused lambda review before unit testing.
2. Polish `TemperatureSensor.RecordReading` output to match the exercise exactly.
3. Start unit testing from zero, before xUnit:
   - manual PASS/FAIL console test
   - Arrange / Act / Assert
   - then `[Fact]` and `Assert.True(...)`
4. Add first real test: above threshold raises event.
5. Add second real test: below threshold does not raise event.
6. Later: study dependency injection and service lifetimes as its own topic.
