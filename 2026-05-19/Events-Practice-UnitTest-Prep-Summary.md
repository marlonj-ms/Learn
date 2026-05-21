# 2026-05-19 — Events Practice + Unit Test Prep Summary

Today's second session: backed up from the full TemperatureSensor exercise, built a smaller Doorbell event model, then returned to TemperatureSensor and completed the core event architecture.

## Main Path

```text
Too hard TemperatureSensor exercise
-> Mini Doorbell event
-> Subscribe/unsubscribe
-> EventArgs payload
-> protected virtual OnXxx
-> override/base mental model
-> TemperatureSensor event system
-> Unit testing from zero next
```

## Completed Code

- `2026-05-19/Mini1-Doorbell/Program.cs`
  - `DoorbellEventArgs : EventArgs`
  - `Doorbell.Rang` using `EventHandler<DoorbellEventArgs>`
  - `Doorbell.Press(string presserName)`
  - `protected virtual OnRang(...)`
  - `LoudDoorbell` and `NoisyDoorbell` overriding `OnRang(...)`
  - subscriber wiring with `+=` and unsubscribe with `-=`

- `2026-05-18/Practice-Exercise/Program.cs`
  - `ThresholdExceededEventArgs : EventArgs`
  - immutable event payload with `Reading`, `Threshold`, `Timestamp`
  - `TemperatureSensor` publisher
  - `ThresholdExceeded` event
  - `protected virtual OnThresholdExceeded(...)`
  - `Buzzer` and `Logger` subscriber classes
  - unsubscribe behavior verified: after removing buzzer, only logger fires

## Key Concepts That Clicked

| Concept | Mental Model |
| --- | --- |
| Method parameters | A method needs parameters only when it needs information from the caller. `Press()` needs none; `Press(string presserName)` does. |
| Event | A protected list of handler methods. `+=` adds, `-=` removes, `?.Invoke` calls everyone if the list is not empty. |
| `EventHandler` | Event with plain `EventArgs`; no custom payload properties. |
| `EventHandler<TArgs>` | Event with a custom payload type; handler receives `TArgs` directly. |
| `EventArgs` subclass | The object that carries event data from publisher to subscriber. |
| `protected virtual OnXxx` | Internal raising hook. Outsiders cannot call it, subclasses can override it. |
| `override` | Child class takes over a virtual step from the parent. |
| `base.OnXxx(e)` | After child behavior, run the parent's default event notification. |
| publisher vs subscriber | `TemperatureSensor` publishes; `Buzzer` and `Logger` listen. They should not inherit from the sensor. |

## Event Flow Picture

```text
RecordReading(101)
  -> reading is above threshold
  -> create ThresholdExceededEventArgs
  -> OnThresholdExceeded(args)
  -> ThresholdExceeded?.Invoke(this, args)
  -> Buzzer.OnThresholdExceeded(this, args)
  -> Logger.OnThresholdExceeded(this, args)
```

With inheritance from the Doorbell mini exercise:

```text
ndb.Press("ndb1")
  -> Doorbell.Press(...)
  -> OnRang(args)
  -> real object is NoisyDoorbell, so NoisyDoorbell.OnRang(args)
  -> base.OnRang(args)
  -> Doorbell.OnRang(args)
  -> Rang?.Invoke(this, args)
  -> subscribers run
```

## Mistakes Corrected

- `Press()` does not always need parameters. Parameters exist only when caller must provide data.
- Custom event data requires all three pieces to agree:
  - `DoorbellEventArgs : EventArgs`
  - `event EventHandler<DoorbellEventArgs>? Rang`
  - handler signature `(object? sender, DoorbellEventArgs e)`
- Calling `Rang?.Invoke(...)` directly inside `Press(...)` bypasses `OnRang(...)`, so subclass overrides do not run.
- `Buzzer : TemperatureSensor` and `Logger : TemperatureSensor` were the wrong relationship. They are subscribers, not sensors.
- `OnThresholdExceeded` should not be public and does not need a `sender` parameter. Correct shape:

```csharp
protected virtual void OnThresholdExceeded(ThresholdExceededEventArgs e)
{
    ThresholdExceeded?.Invoke(this, e);
}
```

- `public double Threshold { get; }` is a get-only property and cannot be overwritten externally after construction.
- `public double Threshold;` is a public field and can be overwritten externally, so sensor threshold should be `private readonly double _threshold`.

## Current TemperatureSensor Status

The code builds and the core event behavior works:

```text
101 -> Buzzer + Logger
150 -> Buzzer + Logger
120 after unsubscribe -> Logger only
```

Remaining polish:

- Make `RecordReading` print `[Sensor] reading <celsius>°C` for every reading.
- Remove the final placeholder `Console.WriteLine`.
- Remove old commented-out fields.
- Optionally rename locals: `sensor`, `buzzer`, `logger`.

## Testing Next Time

The learner said they have no idea what `[Fact]`, `Assert`, or unit testing are, so start from zero.

Teaching order for next session:

1. Manual test in a console app: set `bool eventFired = false`, subscribe with a lambda, call `RecordReading(101)`, print PASS/FAIL.
2. Explain Arrange / Act / Assert.
3. Convert manual check to xUnit:
   - `[Fact]` means xUnit should run this method as a test.
   - `Assert.True(eventFired)` replaces manual `if/else`.
4. Add first real test: above threshold raises event.
5. Add second real test: below threshold does not raise event.
