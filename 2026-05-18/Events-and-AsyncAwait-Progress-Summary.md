# Events and Async/Await Progress Summary

Date: 2026-05-18

## What We Covered

Today started with a review of the previous Events + virtual/sealed lesson, then moved into the first Async/Await mental model chapter. The hands-on event practice was intentionally moved to tomorrow.

## Files Created or Used Today

- `Bad-Design/Program.cs` — event anti-pattern with repeated `Invoke(...)` calls.
- `Good-Design/Program.cs` — production event pattern using `protected virtual OnOrderCompleted(...)`.
- `Your-Design/Program.cs` — runnable version of your own event pseudocode.
- `Event-Flow-Reference.md` — ASCII and Mermaid diagrams for event flow.
- `Practice-Exercise/` — tomorrow's TemperatureSensor event exercise scaffold.
- `Async-Demo/Program.cs` — runnable async demo showing sequential vs parallel execution.

## Events Review

### Concepts Locked In

- `EventHandler<TEventArgs>` is a delegate type used for standard .NET events.
- An event member stores a multicast delegate reference internally.
- `+=` subscribes a handler; it does not execute the handler immediately.
- The invocation list runs synchronously by default.
- If one event handler throws, later handlers may not run unless the publisher handles exceptions explicitly.
- `OnXxx(...)` is the standard protected event-raising method pattern.
- `protected virtual OnXxx(...)` lets derived classes extend event behavior.
- `base.OnXxx(e)` calls the parent raising method and actually notifies subscribers.
- Event payload classes should usually be immutable: `sealed`, get-only properties, inherits from `EventArgs`.

### Event Mental Model

```text
Publisher object
  owns event member
      -> holds multicast delegate
          -> holds invocation list
              -> handler #1
              -> handler #2
              -> handler #3
```

### Event Raising Flow

```text
Business method starts
  -> creates EventArgs payload
  -> calls OnXxx(e)
      -> OnXxx invokes the event
          -> subscribers run synchronously, one by one
  -> business method continues after handlers finish
```

### Good Event Pattern

```csharp
public sealed class OrderCompletedEventArgs : EventArgs
{
    public string OrderId { get; }

    public OrderCompletedEventArgs(string orderId)
    {
        OrderId = orderId;
    }
}

public class OrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        var args = new OrderCompletedEventArgs(orderId);
        OnOrderCompleted(args);
    }

    protected virtual void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        OrderCompleted?.Invoke(this, e);
    }
}
```

## Async/Await Mental Model

### Why Async Exists

Async exists because threads are expensive and limited. If a thread waits for I/O, such as disk, network, or database work, that thread is alive but doing no useful work.

```text
Sync  = thread stands and waits.
Async = method pauses; thread is released to do other work.
```

### Thread vs Task

```text
Thread
  - Real OS worker
  - Expensive
  - Has its own stack
  - Executes code

Task
  - Small managed object
  - Represents ongoing or completed work
  - Stores status, result, exception, continuations
  - Not a thread
```

Important rule:

```text
Calling an async method starts the work immediately.
await does not start work.
await waits for work that has already started.
```

### Task and Task<T>

```csharp
Task saveTask = SaveAsync();              // operation with no result value
Task<int> countTask = CountUsersAsync();  // operation that produces an int
```

- `Task` means the operation completes later but gives no value.
- `Task<T>` means the operation completes later and gives a `T`.
- `.Result` blocks the calling thread if the task is not finished.
- Prefer `await` instead of `.Result` or `.Wait()`.

### What await Does

```text
await task
  -> if task already completed: continue immediately
  -> if task not completed:
       pause this method
       register the rest of the method as a continuation
       return the thread to the thread pool
       resume later when the task completes
```

Memory phrase:

```text
await = pause method, release thread.
await != start a thread.
await != block the thread.
```

### async Return Type Rules

Only these are valid async method return types:

```text
Task
Task<T>
void  // only for event handlers
```

Inside an `async Task<T>` method, return the inner value, not `Task<T>`:

```csharp
public async Task<int> CountAsync()
{
    int count = await GetCountFromDatabaseAsync();
    return count; // return int, compiler wraps it in Task<int>
}
```

Use `Task.FromResult(...)` only when the method is not marked `async` but still returns a Task:

```csharp
public Task<int> CountAsync()
{
    return Task.FromResult(42);
}
```

### async Without await

```csharp
public async Task<string> GetNameAsync()
{
    return "Alice";
}
```

This compiles, but it runs synchronously and produces warning CS1998. `async` alone does not make work asynchronous. `await` is what creates a suspension point.

## Sequential vs Parallel Async

### Sequential Pattern

```csharp
string a = await FakeLoadUserAsync(1);
string b = await FakeLoadUserAsync(2);
string c = await FakeLoadUserAsync(3);
```

Flow:

```text
start task 1 -> wait -> finish task 1
start task 2 -> wait -> finish task 2
start task 3 -> wait -> finish task 3
```

Total time for three 200ms operations: about 600ms.

### Parallel Manual Pattern

```csharp
Task<string> taskA = FakeLoadUserAsync(1);
Task<string> taskB = FakeLoadUserAsync(2);
Task<string> taskC = FakeLoadUserAsync(3);

string a = await taskA;
string b = await taskB;
string c = await taskC;
```

Flow:

```text
start task 1
start task 2
start task 3
wait for taskA result
wait for taskB result
wait for taskC result
```

All tasks are already running before the first await. Total time for three 200ms operations: about 200ms.

### Completion Order vs Collection Order

```text
Task completion order: runtime decides; may look random.
Result collection order: your await statements decide.
```

Example:

```text
taskC may finish first,
but this code still assigns:
  taskA result -> a
  taskB result -> b
  taskC result -> c
```

### Task.WhenAll Pattern

```csharp
List<Task<User>> tasks = userIds
    .Select(id => db.LoadUserAsync(id))
    .ToList();

User[] users = await Task.WhenAll(tasks);
```

Meaning:

```text
Select transforms each id into a Task<User>.
ToList forces the lazy LINQ query to run now.
All DB calls start.
Task.WhenAll waits until all tasks finish.
await unwraps Task<User[]> into User[].
```

Type flow:

```text
List<int>
  -> Select(id => LoadUserAsync(id))
IEnumerable<Task<User>>
  -> ToList()
List<Task<User>>
  -> Task.WhenAll(...)
Task<User[]>
  -> await
User[]
```

## LINQ Laziness and the foreach Trap

LINQ `Select` is lazy. This line alone does not start anything:

```csharp
var lazyTasks = userIds.Select(id => FakeLoadUserAsync(id));
```

This is a trap:

```csharp
foreach (var task in lazyTasks)
{
    string result = await task;
}
```

Because each iteration starts one task and immediately awaits it, the whole thing becomes sequential.

Correct parallel version:

```csharp
List<Task<string>> tasks = userIds
    .Select(id => FakeLoadUserAsync(id))
    .ToList();

string[] results = await Task.WhenAll(tasks);
```

## Exception and Fire-and-Forget Notes

### await Handles More Than Return Values

Even when a Task has no result value, `await` still matters:

```csharp
await SaveAsync();
```

This means:

```text
wait until SaveAsync is complete
if SaveAsync throws, rethrow the exception here
only continue if SaveAsync succeeded
```

### Forgotten Task Bug

```csharp
SaveAsync(); // bad: task starts, but result and exception are ignored
```

Problems:

- The method continues immediately.
- You do not know when the work finishes.
- Exceptions may become unobserved.
- The compiler usually warns with CS4014.

If fire-and-forget is intentional, discard explicitly and handle exceptions inside the async method:

```csharp
_ = LogSafelyAsync(message);

static async Task LogSafelyAsync(string message)
{
    try
    {
        await WriteLogAsync(message);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

## Shared State Warning

Within one async method call, lines execute sequentially. But multiple calls can be in flight at the same time.

```csharp
int _counter = 0;

public async Task IncrementAsync()
{
    int temp = _counter;
    await Task.Delay(1);
    _counter = temp + 1;
}
```

Race condition flow:

```text
Call #1 reads _counter = 0, then awaits.
Call #2 reads _counter = 0, then awaits.
Call #1 writes 1.
Call #2 writes 1.
Expected 2, actual 1.
```

The problem is shared mutable state across multiple in-flight calls.

## Async Exception Handling

Async exceptions are stored in the returned `Task` until the task is awaited.

```csharp
Task task = FailAsync(); // exception usually does not throw here
await task;              // exception is rethrown here
```

Mental model:

```text
Async exception lives inside the Task.
await opens the Task and rethrows the exception.
```

If a task is ignored, the exception may become unobserved:

```csharp
FailAsync(); // bad: Task is created, but result/exception is ignored
```

With independent parallel tasks, `Task.WhenAll(...)` gives a clean all-finished boundary:

```csharp
Task<string> a = LoadAsync("A");
Task<string> b = LoadAsync("B");
Task<string> c = LoadAsync("C");

try
{
  string[] results = await Task.WhenAll(a, b, c);
}
catch
{
  // At this point, all tasks are finished.
  // Inspect individual task.Exception values if all failures matter.
  throw;
}
```

By contrast, this can stop at the first failure and leave later task results/exceptions unobserved:

```csharp
string aResult = await a;
string bResult = await b;
string cResult = await c;
```

## CancellationToken

Cancellation lets a caller request that async work stop early. It is cooperative, not forced.

```text
CancellationTokenSource = cancel authority
CancellationToken       = read-only cancellation signal
```

Roles:

```text
Source sends.
Token carries.
Method observes.
```

`CancellationTokenSource` can call `Cancel()` or `CancelAfter(...)`:

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(1000);

await DoWorkAsync(cts.Token);
```

The async method only cancels if it observes the token:

```csharp
static async Task DoWorkAsync(CancellationToken token)
{
  token.ThrowIfCancellationRequested();
  await Task.Delay(500, token);
}
```

Passing a token is not enough. The method or API must handle it.

Ignored token example:

```csharp
static async Task MyWorkAsync(CancellationToken token)
{
  await Task.Delay(5000); // token ignored, so cancellation will not stop this delay
}
```

Handled token example:

```csharp
static async Task MyWorkAsync(CancellationToken token)
{
  await Task.Delay(5000, token); // Task.Delay observes the token
}
```

## How Cancellation Is Noticed

Tasks do not constantly watch the token. Cancellation is efficient because it is noticed in two ways.

### Manual Checkpoints

```csharp
token.ThrowIfCancellationRequested();
```

This is like reading a shared boolean flag. Code chooses where to check.

### Callback Registration

APIs can register a callback with the token:

```csharp
CancellationTokenRegistration registration = token.Register(() =>
{
  Console.WriteLine("Cancellation was requested");
});
```

Conceptual flow:

```text
CancellationTokenSource
  shared state: canceled or not canceled
  callback list: registered cancellation callbacks

cts.Cancel()
  sets canceled = true
  invokes registered callbacks
```

This is event-like internally:

```text
token.Register(...) is like subscribing to a cancellation event.
cts.Cancel() is like raising that event.
```

But passing a token into a method does not automatically register a task. The receiving method/API must check, register, or pass the token deeper.

## Safe Cancellation and Cleanup

A cancellable async method must decide where it is safe to stop.

```text
Cancellation is not "stop immediately".
Cancellation is "please stop at the next safe place".
```

If the method has side effects, it may need cleanup or rollback:

```csharp
public async Task ProcessOrderAsync(Order order, CancellationToken token)
{
  bool inventoryReserved = false;

  try
  {
    token.ThrowIfCancellationRequested();

    await ReserveInventoryAsync(order, token);
    inventoryReserved = true;

    await ChargePaymentAsync(order, token);
    await SendConfirmationEmailAsync(order, token);
  }
  catch (OperationCanceledException)
  {
    if (inventoryReserved)
    {
      await ReleaseInventoryAsync(order, CancellationToken.None);
    }

    throw;
  }
}
```

Cleanup often should not use the already-canceled token, because cleanup must still complete:

```csharp
await RollbackAsync(CancellationToken.None);
```

After a commit point, it may be safer to finish or compensate instead of honoring cancellation immediately.

## Async Demo Results

The demo at `Async-Demo/Program.cs` was created and run successfully.

Observed timings:

```text
Sequential:        about 659ms
ParallelManual:    about 213ms
ParallelLinq:      about 212ms
ForeachTrap:       about 620ms
```

The results proved:

- Inline await makes operations sequential.
- Starting tasks first, then awaiting them, makes independent operations parallel.
- LINQ + `Task.WhenAll` is the production pattern.
- Lazy `Select` plus `foreach await` is sequential.

## Key Takeaways

1. Calling an async method starts work immediately.
2. `await` waits for an already-started Task.
3. `await` pauses the method, not the thread.
4. `Task` is not a thread; it is a handle to work.
5. Use `Task<T>` when the async operation returns a value.
6. Inside `async Task<T>`, return `T`, not `Task<T>`.
7. Multiple independent async operations should usually use `Task.WhenAll`.
8. LINQ `Select` is lazy; use `.ToList()` or pass directly to `Task.WhenAll` to start all tasks.
9. Forgotten Tasks are dangerous because exceptions may be lost.
10. Shared mutable state can race across multiple in-flight async calls.
11. Async exceptions live inside the `Task` until `await` rethrows them.
12. Cancellation is cooperative: source sends, token carries, method observes.
13. Passing a token is not enough; the receiving method/API must check, register, or pass it deeper.
14. Cancellable methods with side effects must choose safe cancellation points and consider cleanup/rollback.

## Review Questions for Next Day

### Events

1. What does `+=` do to an event? Does it execute the handler immediately?
2. What is the difference between an event member, a delegate object, and an invocation list?
3. Why do we create a `protected virtual OnXxx(...)` method instead of calling `Invoke(...)` everywhere?
4. What does `base.OnXxx(e)` do in a derived class override?
5. Are default event handlers synchronous or asynchronous?
6. What happens if one event handler throws an exception?

### Async Basics

1. What problem does async solve in server applications?
2. What is the difference between a Thread and a Task?
3. What is the difference between `Task` and `Task<T>`?
4. What does `await` do to the method?
5. What does `await` do to the thread?
6. Does `await` start a new thread?
7. Does calling an async method start the work immediately?

### async Method Rules

1. What are the three valid return types for an async method?
2. In `async Task<int>`, should you return `int` or `Task<int>`?
3. When should you use `Task.FromResult(...)`?
4. Does an `async` method without `await` compile?
5. Does an `async` method without `await` actually do asynchronous work?

### Sequential vs Parallel

1. Why is this sequential?
   ```csharp
   string a = await LoadAsync(1);
   string b = await LoadAsync(2);
   ```
2. Why is this parallel?
   ```csharp
   Task<string> aTask = LoadAsync(1);
   Task<string> bTask = LoadAsync(2);
   string a = await aTask;
   string b = await bTask;
   ```
3. If three independent tasks each take 200ms and use `Task.WhenAll`, what is the total time?
4. What is the difference between task completion order and result collection order?
5. Why does `Task.WhenAll` return `Task<T[]>` when given many `Task<T>` objects?

### LINQ + Async

1. What does it mean that LINQ `Select` is lazy?
2. Why does `.ToList()` start all the async operations in this pattern?
3. Why is `foreach` + `await` over a lazy Select usually sequential?
4. What is the production pattern for loading many independent items asynchronously?

### Bugs and Production Safety

1. What happens if you call an async method and forget to await or store the Task?
2. Why does `await SaveAsync()` matter even if `SaveAsync` returns no value?
3. Why can shared mutable state be dangerous in async methods?
4. How can two calls to the same async method race with each other?

### Exceptions and Cancellation

1. Where does an async exception live before you `await` the Task?
2. Why does `try { FailAsync(); } catch { ... }` usually fail to catch the async exception?
3. Why is `Task.WhenAll(a, b, c)` safer than `await a; await b; await c;` for independent tasks?
4. Which object can call `Cancel()`: `CancellationTokenSource` or `CancellationToken`?
5. If `cts.Cancel()` is called but the method never checks or passes the token, what happens?
6. Why do worker methods receive `CancellationToken` instead of `CancellationTokenSource`?
7. What is the difference between passing a token and observing/handling a token?
8. Why can `await Task.Delay(5000, token)` stop early, while `await Task.Delay(5000)` cannot?
9. Why might cleanup/rollback use `CancellationToken.None`?
10. What is a safe cancellation point?

## Next Session Plan

1. Finish the TemperatureSensor event practice exercise.
2. Do a LINQ syntax-reading drill: `Select`, lambda expressions, `ToList`, `List<Task<T>>`, and `Task.WhenAll(...)` type flow.
3. Review `CancellationToken`: token observation, callback registration, and safe cancellation points.
4. Re-run the Async-Demo and explain each timestamp.
5. Continue Async/Await with throttling parallel work using `SemaphoreSlim`.
