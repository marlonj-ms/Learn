namespace TemperatureSensor.Core;

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
		if (double.IsNaN(celsius)|| double.IsInfinity(celsius))
		{
			throw new ArgumentException("Reading must be a finite number.",nameof(celsius));
		}
		if (celsius > _threshold )
		{
			ThresholdExceeded?.Invoke(this, new ThresholdExceededEventArgs(celsius, _threshold, DateTime.Now));
		}	
	}
  
}



