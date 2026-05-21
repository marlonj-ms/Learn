using System;

// ============================================================
// Mini Exercise 1 — Doorbell
// ------------------------------------------------------------
// Goal: write the SMALLEST possible event from scratch.
// No payload class. No threshold. No unsubscribe. Just += and fire.
//
// THE STORY:
//   - A `Doorbell` has an event called `Rang`.
//   - When somebody calls `Press()`, the doorbell raises `Rang`.
//   - A `Person` has a method `OnDoorbellRang` that prints "Coming!".
//   - In Main: build one Doorbell, one Person, subscribe, press twice.
//
// EXPECTED OUTPUT:
//   [Doorbell] pressed
//   Coming!
//   [Doorbell] pressed
//   Coming!
//
// ============================================================
//
// Use the simplest event type — no generic args, no payload:
//
//     public event EventHandler? Rang;
//
// `EventHandler` is a built-in delegate with signature:
//     void (object? sender, EventArgs e)
//
// So the handler method must look like:
//     void OnDoorbellRang(object? sender, EventArgs e) { ... }
//
// To raise the event from inside Doorbell:
//     Rang?.Invoke(this, EventArgs.Empty);
//
// ============================================================

namespace Mini1Doorbell;


public class DoorbellEventArgs:EventArgs
{
    public string PresserName{get;}
    public DoorbellEventArgs(string presserName)
    {
        PresserName = presserName;
    }

}
public class Doorbell
{
	public event EventHandler<DoorbellEventArgs>? Rang;

	public void Press(string presserName)
	{
		Console.WriteLine("[Doorbell] pressed");
		OnRang( new DoorbellEventArgs(presserName));
	}

    protected virtual void OnRang(DoorbellEventArgs e)
    {
        Rang?.Invoke(this, e);
    }
    
}

public class LoudDoorbell : Doorbell
{
    protected override void OnRang(DoorbellEventArgs e)
    {
        Console.WriteLine($"[LoudDoorbell] Extra loud ring for {e.PresserName}");

        base.OnRang(e);
    }
}

public class NoisyDoorbell : Doorbell
{
    protected override void OnRang(DoorbellEventArgs e)
    {
        Console.WriteLine($"[NoisyDoorbell] Extra noisy ring for {e.PresserName}");

        base.OnRang(e);
    }
}

public class Person
{
	public void OnDoorbellRang(object? sender, DoorbellEventArgs e)
	{
		Console.WriteLine($"Coming (someone pressed: {e.PresserName})!");
	}

    
}



public class Program
{
	public static void Main()
	{
		Doorbell db  = new Doorbell();
        NoisyDoorbell ndb = new NoisyDoorbell();
        LoudDoorbell ldb = new LoudDoorbell();

		Person alice = new Person();
		Person Jim = new Person();
        Person Tom = new Person();
		
		db.Rang+=alice.OnDoorbellRang;
		ndb.Rang+=Jim.OnDoorbellRang;
        ldb.Rang+=Tom.OnDoorbellRang;

		db.Press("db1");
		db.Press("db2");

		Console.WriteLine("=========================================");

        ndb.Press("ndb1");
        ndb.Press("ndb2");

	Console.WriteLine("=========================================");

        ldb.Press("ldb1");
        ldb.Press("ldb2");

	}
}
