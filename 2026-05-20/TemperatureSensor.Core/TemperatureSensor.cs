namespace TemperatureSensor.Core;

// ----------------------------------------------------------------------
// Production code: nothing to do with tests.
// Lives in TemperatureSensor.Core.dll and is the only thing shipped to
// production. No Console.WriteLine here — production code should not
// decide how to talk to a user / log / display.
// ----------------------------------------------------------------------

public sealed class ThresholdExceededEventArgs : EventArgs
{
    public double Reading { get; }
    public double Threshold { get; }
    public DateTime Timestamp { get; }

    public ThresholdExceededEventArgs(double reading, double threshold, DateTime timestamp)
    {
        Reading = reading;
        Threshold = threshold;
        Timestamp = timestamp;
    }
}

public class TemperatureSensor
{
    private readonly double _threshold;

    public TemperatureSensor(double threshold)
    {
        _threshold = threshold;
    }

    public event EventHandler<ThresholdExceededEventArgs>? ThresholdExceeded;

    public void RecordReading(double celsius)
    {
        if (double.IsNaN(celsius) || double.IsInfinity(celsius))
        {
            throw new ArgumentException(
                "Reading must be a finite number.",
                nameof(celsius));
        }

        if (celsius > _threshold)
        {
            OnThresholdExceeded(
                new ThresholdExceededEventArgs(celsius, _threshold, DateTime.Now));
        }
    }

    protected virtual void OnThresholdExceeded(ThresholdExceededEventArgs e)
    {
        ThresholdExceeded?.Invoke(this, e);
    }
}
