using System;


// ============================================================
// Practice exercise — write the event system from scratch
// See EXERCISE.md in this folder for the full spec.
// ============================================================
//
// Suggested order to fill in:
//   1. ThresholdExceededEventArgs   — the payload (sealed, get-only)
//   2. TemperatureSensor            — publisher with event + OnXxx
//   3. Buzzer                       — subscriber with handler method
//   4. Logger                       — subscriber with handler method
//   5. Main                         — wire up, fire readings, unsubscribe, fire again
//
// Reminders:
//   - event is `public event EventHandler<TEventArgs>? EventName;`
//   - Raising method is `protected virtual void OnEventName(TEventArgs e)`.
//   - Raising method body is `EventName?.Invoke(this, e);`
//   - Handler signature is `void Handler(object? sender, TEventArgs e)`.
//   - Subscribe with `+=`, unsubscribe with `-=`.
// ============================================================

namespace PracticeExercise;

// TODO 1: ThresholdExceededEventArgs (sealed, : EventArgs, get-only Reading/Threshold/Timestamp)

public sealed class ThresholdExceededEventArgs:EventArgs
{
	//private readonly string reading;
	//private readonly int threhold;
	//private readonly date timestamp;

	public double Reading {get;}
	public double Threshold {get;}
	public DateTime Timestamp {get;}
	
	public ThresholdExceededEventArgs(double reading, double threshold, DateTime timestamp)
	{
		Reading = reading;
		Threshold = threshold;
		Timestamp= timestamp;
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
         Console.WriteLine($"[Sensor] reading {celsius}°C");
		if (celsius > _threshold)
		{
			Console.WriteLine("Temperature too high");
			OnThresholdExceeded(new ThresholdExceededEventArgs(celsius,this._threshold,DateTime.Now));
		}

	}
	
	protected virtual void OnThresholdExceeded( ThresholdExceededEventArgs e)
	{
		ThresholdExceeded?.Invoke(this, e );
	}
	
}

public class Buzzer
{
	public void OnThresholdExceeded(object? sender, ThresholdExceededEventArgs e)
	{
		Console.WriteLine($"[Buzzer] BEEP BEEP! Temp {e.Reading}°C exceeded threshold {e.Threshold}°C");
	}
	
}

public class Logger
{
	public  void OnThresholdExceeded(object? sender, ThresholdExceededEventArgs e)
	{
		Console.WriteLine($"[Logger] {e.Timestamp:HH:mm:ss} temp={e.Reading} threshold={e.Threshold}");
		
    }
	
}



public class Program
{
	static void Test_AboveThreshold_RaisesEvent()
{
	TemperatureSensor sensor = new TemperatureSensor(100.0);
	bool eventFired = false;
	sensor.ThresholdExceeded += (sender, e) => { eventFired = true; };
	sensor.RecordReading(101);


	if (eventFired) { Console.WriteLine("PASS: event fired when above threshold"); }
	else { Console.WriteLine("FAIL: event did not fire"); }
}


static void Test_AtOrBelowThreshold_DoesNotRaiseEvent()
{
	TemperatureSensor sensor = new TemperatureSensor(100.0);
	bool eventFired = false;
	sensor.ThresholdExceeded += (sender, e) => { eventFired = true; };
	sensor.RecordReading(99);


	if (!eventFired) { 
		Console.WriteLine("PASS: event did not fire when at or below threshold"); 
		}
	else { 
		Console.WriteLine("FAIL: event fired but should not have");
		 }
}

	public static void Main()
	{
		Test_AboveThreshold_RaisesEvent();
		Test_AtOrBelowThreshold_DoesNotRaiseEvent();

        //TemperatureSensor sensor = new TemperatureSensor(100.0);
		//bool eventFired = false;
	    //sensor.ThresholdExceeded += (sender, e) => {eventFired = true;};
	   // sensor.RecordReading(101);


		//if (eventFired) 
		//{
		//	Console.WriteLine("PASS: event fired when above threshold");
		//	}
		//else {
		//	Console.WriteLine("FAIL: event did not fire");
		//	}
       // Buzzer buzzer = new Buzzer();
       // Logger logger = new Logger();

		//sensor.ThresholdExceeded += buzzer.OnThresholdExceeded;
		//sensor.ThresholdExceeded += logger.OnThresholdExceeded;
		
		//sensor.RecordReading(80);
		//sensor.RecordReading(95);
		//sensor.RecordReading(101);
		//sensor.RecordReading(99);
		//sensor.RecordReading(150);
		//
		//sensor.ThresholdExceeded -= buzzer.OnThresholdExceeded;
		//sensor.RecordReading(120);

	}
}
