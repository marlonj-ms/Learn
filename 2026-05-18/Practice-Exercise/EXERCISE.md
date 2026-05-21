# Practice Exercise — Temperature Sensor + Buzzer + Logger

**Goal**: Write a complete event system from a blank file, without copying from the Good-Design / Your-Design code.

You can refer to the reference docs only **after** you have a first draft.

---

## The Domain

You are building part of an IoT device:

- A `TemperatureSensor` reads temperatures (as `double`).
- When a reading exceeds a configured threshold, the sensor must **announce** the event to any listeners.
- A `Buzzer` listens and sounds an alarm.
- A `Logger` listens and writes a log line.
- The same sensor might have **0, 1, or many listeners** — the sensor must not care who is listening.

---

## Functional Requirements

### Sensor `TemperatureSensor`

1. Constructor takes a `double threshold` (e.g. `100.0`).
2. Public method `RecordReading(double celsius)`:
   - Print: `[Sensor] reading <celsius>°C`
   - If `celsius > threshold`, raise the `ThresholdExceeded` event.
   - If `celsius <= threshold`, do NOT raise the event.
3. Public event `ThresholdExceeded` — fired only when threshold is exceeded.
4. Use the **production OnXxx pattern** (`protected virtual OnThresholdExceeded(...)`).

### Event payload `ThresholdExceededEventArgs`

Must carry:

- `Reading` (the actual temperature value, `double`)
- `Threshold` (the threshold that was exceeded, `double`)
- `Timestamp` (when it happened, `DateTime`)

Requirements:

- `sealed` class
- All properties `get`-only (immutable)
- Inherits from `EventArgs`

### Subscriber `Buzzer`

- Public method `OnThresholdExceeded(object? sender, ThresholdExceededEventArgs e)`
- Prints: `[Buzzer] BEEP BEEP! Temp <reading>°C exceeded threshold <threshold>°C`

### Subscriber `Logger`

- Public method `OnThresholdExceeded(object? sender, ThresholdExceededEventArgs e)`
- Prints: `[Logger] <timestamp HH:mm:ss> temp=<reading> threshold=<threshold>`

### `Main`

1. Create a sensor with threshold `100.0`.
2. Create a buzzer and a logger.
3. Subscribe both to the sensor's event.
4. Record readings: `80, 95, 101, 99, 150`.
5. Then **unsubscribe the buzzer** and record one more reading: `120`.

### Expected output shape (timestamps will differ)

```text
[Sensor] reading 80°C
[Sensor] reading 95°C
[Sensor] reading 101°C
[Buzzer] BEEP BEEP! Temp 101°C exceeded threshold 100°C
[Logger] 14:23:01 temp=101 threshold=100
[Sensor] reading 99°C
[Sensor] reading 150°C
[Buzzer] BEEP BEEP! Temp 150°C exceeded threshold 100°C
[Logger] 14:23:01 temp=150 threshold=100
[Sensor] reading 120°C
[Logger] 14:23:01 temp=120 threshold=100
```

Notice:

- Readings of 80, 95, 99 should NOT trigger the event.
- Readings of 101 and 150 should trigger both subscribers.
- After unsubscribing the buzzer, 120 should trigger ONLY the logger.

---

## Acceptance Checklist

When you think you're done, your code must satisfy:

- [ ] `ThresholdExceededEventArgs` is `sealed`, inherits `EventArgs`, properties are `get;` only.
- [ ] `TemperatureSensor` has `public event EventHandler<ThresholdExceededEventArgs>? ThresholdExceeded;`
- [ ] `TemperatureSensor` has `protected virtual void OnThresholdExceeded(...)` that calls `ThresholdExceeded?.Invoke(this, e)`.
- [ ] `RecordReading` only calls `OnThresholdExceeded(...)` when the threshold is exceeded.
- [ ] Both subscriber methods match the signature `(object? sender, ThresholdExceededEventArgs e)`.
- [ ] `Main` subscribes with `+=`, unsubscribes with `-=`, and triggers via `RecordReading`.
- [ ] `?.Invoke(...)` is used to avoid `NullReferenceException` when no one is subscribed.
- [ ] Code compiles and runs successfully via `dotnet run --project ...`.

---

## Hints (use only if stuck)

<details>
<summary>Hint 1 — The 7 ingredients</summary>

```text
1. ThresholdExceededEventArgs    (payload, sealed, get-only)
2. TemperatureSensor              (publisher)
3. ThresholdExceeded              (event member)
4. RecordReading                  (business method)
5. OnThresholdExceeded            (raising method, protected virtual)
6. Buzzer.OnThresholdExceeded
   Logger.OnThresholdExceeded     (handler methods)
7. += / -= in Main                (subscription / unsubscription)
```
</details>

<details>
<summary>Hint 2 — The shape of the publisher</summary>

```csharp
public class TemperatureSensor
{
    private readonly double _threshold;
    public event EventHandler<ThresholdExceededEventArgs>? ThresholdExceeded;

    public TemperatureSensor(double threshold) { _threshold = threshold; }

    public void RecordReading(double celsius)
    {
        Console.WriteLine($"[Sensor] reading {celsius}°C");

        if (celsius > _threshold)
        {
            var args = new ThresholdExceededEventArgs(celsius, _threshold, DateTime.Now);
            OnThresholdExceeded(args);
        }
    }

    protected virtual void OnThresholdExceeded(ThresholdExceededEventArgs e)
    {
        ThresholdExceeded?.Invoke(this, e);
    }
}
```
</details>

<details>
<summary>Hint 3 — Running the project</summary>

```powershell
dotnet run --project d:\AITriage\2026-05-18\Practice-Exercise\Practice-Exercise.csproj
```
</details>

---

## When You're Done

Tell me you're done. I'll:

1. Read your `Program.cs`.
2. Check it against the acceptance checklist.
3. Run it and verify the output.
4. Point out anything to improve (and praise what you got right).
