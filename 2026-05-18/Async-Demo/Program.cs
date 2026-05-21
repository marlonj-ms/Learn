// Async-Demo: Sequential vs Parallel
// Run with: dotnet run --project d:\AITriage\2026-05-18\Async-Demo
//
// Goal: SEE the timestamps. Two versions doing the same 3 fake DB calls.
// Each fake call takes 200ms.
//   - Sequential version  → 3 × 200ms ≈ 600ms total
//   - Parallel version    → max(200ms) ≈ 200ms total
//
// Press Enter at the end to close the window.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    // We use a Stopwatch to print "millis since start" so you can see
    // exactly when each step happens.
    static readonly Stopwatch sw = new();

    static int activeThrottledLoads;

    // Pretend this is a slow database call. Takes 200ms.
    // Returns a string telling you which user "loaded".
    static async Task<string> FakeLoadUserAsync(int userId)
    {
        Log($"  FakeLoadUserAsync({userId}) START");
        await Task.Delay(200);                          // pretend DB I/O
        Log($"  FakeLoadUserAsync({userId}) END");
        return $"User#{userId}";
    }

    // ─────────────────────────────────────────────────────────────────
    // Version 1: SEQUENTIAL
    //   await each call before starting the next.
    //   Total time: 3 × 200ms = 600ms.
    // ─────────────────────────────────────────────────────────────────
    static async Task RunSequentialAsync()
    {
        Log("Sequential: BEGIN");

        string a = await FakeLoadUserAsync(1);
        string b = await FakeLoadUserAsync(2);
        string c = await FakeLoadUserAsync(3);

        Log($"Sequential: got {a}, {b}, {c}");
        Log("Sequential: END");
    }

    // ─────────────────────────────────────────────────────────────────
    // Version 2: PARALLEL — manual style
    //   Start all 3 tasks first (no await), then await each.
    //   All 3 disks/DBs work at the same time.
    //   Total time: ~200ms.
    // ─────────────────────────────────────────────────────────────────
    static async Task RunParallelManualAsync()
    {
        Log("ParallelManual: BEGIN");

        // Step 1: kick off all 3 tasks — they start RIGHT NOW
        Task<string> taskA = FakeLoadUserAsync(1);
        Task<string> taskB = FakeLoadUserAsync(2);
        Task<string> taskC = FakeLoadUserAsync(3);

        // Step 2: await each (they're already running)
        string a = await taskA;
        string b = await taskB;
        string c = await taskC;

        Log($"ParallelManual: got {a}, {b}, {c}");
        Log("ParallelManual: END");
    }

    // ─────────────────────────────────────────────────────────────────
    // Version 3: PARALLEL — idiomatic with LINQ + Task.WhenAll
    //   The production pattern. Scales from 3 to 300.
    //   Total time: ~200ms.
    // ─────────────────────────────────────────────────────────────────
    static async Task RunParallelLinqAsync()
    {
        Log("ParallelLinq: BEGIN");

        int[] userIds = { 1, 2, 3 };

        // .ToList() forces all 3 lambdas to run NOW, kicking off 3 Tasks.
        List<Task<string>> tasks =
            userIds.Select(id => FakeLoadUserAsync(id)).ToList();

        // WhenAll waits for the slowest one, gives back an array of results.
        string[] results = await Task.WhenAll(tasks);

        Log($"ParallelLinq: got {string.Join(", ", results)}");
        Log("ParallelLinq: END");
    }

    // ─────────────────────────────────────────────────────────────────
    // Version 4: ANTI-PATTERN — foreach + await on a lazy Select
    //   Same code looks parallel, but is sequential because Select is lazy.
    //   Total time: 3 × 200ms = 600ms (just like Sequential).
    // ─────────────────────────────────────────────────────────────────
    static async Task RunForeachTrapAsync()
    {
        Log("ForeachTrap: BEGIN");

        int[] userIds = { 1, 2, 3 };

        // NOTE: no .ToList() — sequence is LAZY.
        var lazyTasks = userIds.Select(id => FakeLoadUserAsync(id));

        // foreach + await → each iteration starts AND finishes a Task
        // before moving on. Sequential!
        foreach (var t in lazyTasks)
        {
            string r = await t;
            Log($"ForeachTrap: got {r}");
        }

        Log("ForeachTrap: END");
    }

    // ─────────────────────────────────────────────────────────────────
    // Version 5: THROTTLED PARALLEL — SemaphoreSlim
    //   Start 10 tasks, but allow only 3 into the fake I/O section at once.
    //   This protects a resource without making the whole batch sequential.
    // ─────────────────────────────────────────────────────────────────
    static async Task RunThrottledParallelAsync()
    {
        Log("ThrottledParallel: BEGIN");

        activeThrottledLoads = 0;
        int[] userIds = Enumerable.Range(1, 10).ToArray();

        using var gate = new SemaphoreSlim(3);

        List<Task<string>> tasks = userIds
            .Select(id => FakeLoadUserWithLimitAsync(id, gate))
            .ToList();

        string[] results = await Task.WhenAll(tasks);

        Log($"ThrottledParallel: got {string.Join(", ", results)}");
        Log("ThrottledParallel: END");
    }

    static async Task<string> FakeLoadUserWithLimitAsync(int userId, SemaphoreSlim gate)
    {
        Log($"  User {userId}: waiting for permit");
        await gate.WaitAsync();

        int active = Interlocked.Increment(ref activeThrottledLoads);
        Log($"  User {userId}: ACQUIRED permit (active={active})");

        try
        {
            await Task.Delay(200);
            Log($"  User {userId}: finished fake I/O");
            return $"User#{userId}";
        }
        finally
        {
            active = Interlocked.Decrement(ref activeThrottledLoads);
            Log($"  User {userId}: RELEASING permit (active={active})");
            gate.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper: log with timestamp
    // ─────────────────────────────────────────────────────────────────
    static void Log(string msg)
    {
        Console.WriteLine($"[{sw.ElapsedMilliseconds,4}ms] {msg}");
    }

    static async Task Main()
    {
        Console.WriteLine("=== Async-Demo: Sequential vs Parallel ===\n");

        // ── Run #1: Sequential ──
        sw.Restart();
        await RunSequentialAsync();
        Console.WriteLine($"   → wall time: {sw.ElapsedMilliseconds}ms\n");

        // ── Run #2: Parallel (manual) ──
        sw.Restart();
        await RunParallelManualAsync();
        Console.WriteLine($"   → wall time: {sw.ElapsedMilliseconds}ms\n");

        // ── Run #3: Parallel (LINQ + WhenAll) ──
        sw.Restart();
        await RunParallelLinqAsync();
        Console.WriteLine($"   → wall time: {sw.ElapsedMilliseconds}ms\n");

        // ── Run #4: Foreach trap ──
        sw.Restart();
        await RunForeachTrapAsync();
        Console.WriteLine($"   → wall time: {sw.ElapsedMilliseconds}ms\n");

        // ── Run #5: Throttled parallel (SemaphoreSlim) ──
        sw.Restart();
        await RunThrottledParallelAsync();
        Console.WriteLine($"   → wall time: {sw.ElapsedMilliseconds}ms\n");

        Console.WriteLine("=== Done. Press Enter to exit. ===");
        Console.ReadLine();
    }
}
