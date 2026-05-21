# 2026-05-18 — Today's Learning Plan

## Phase 1: Review Quiz (from 2026-05-17) ✅ DONE

Locked in:
- `EventHandler<TEventArgs>`, `sender`, `e` mechanics
- `+=` semantics: stored vs executed, multicast delegate, invocation list
- Event vs delegate object vs invocation list (3-layer model)
- `OnXxx` pattern: why split, why `protected virtual`, why `On` prefix
- `base.OnXxx(e)` — calling the parent raising method
- Virtual dispatch picks override based on real object type
- Default events are synchronous
- Throwing handler breaks the chain
- EventArgs sealed + get-only = immutable payload

Reference: [Event-Flow-Reference.md](./Event-Flow-Reference.md)

## Phase 2: Hands-On Practice ⏭ MOVED TO TOMORROW (2026-05-19)

- `Practice-Exercise/EXERCISE.md` — TemperatureSensor + Buzzer + Logger from scratch
- Starter file already compiles; just fill the TODOs

## Phase 3: Async / Await — Mental Model 🔄 IN PROGRESS

Goals:
1. 同步执行 (synchronous) vs 异步执行 (asynchronous) — the problem
2. 阻塞线程 (blocking the thread) — what does it actually mean?
3. 线程 (thread) vs 任务 (task) — different abstractions
4. `Task` = 未来完成的承诺 (a promise of future completion)
5. `Task<T>` = 未来的返回值 (a future result value)
6. `await` = 暂停当前方法 (pause this method)
7. Why async helps the system stay responsive

Covered so far:
- `Task` vs `Task<T>`
- `async` return type rules: `Task`, `Task<T>`, `void`
- `await` pauses the method and releases the thread
- Calling an async method starts the work immediately
- Sequential await vs starting tasks first and awaiting later
- `Task.WhenAll(...)` for independent parallel async work
- LINQ `Select` laziness and the `foreach` + `await` trap
- Forgotten Task / fire-and-forget risks
- Shared mutable state risk across multiple in-flight async calls
- Async exceptions live inside the `Task` until `await` rethrows them
- `CancellationTokenSource` vs `CancellationToken`
- Cancellation is cooperative: source sends, token carries, method observes
- Cancellable methods must choose safe cancellation points and consider cleanup/rollback

Runnable demo: [Async-Demo/Program.cs](./Async-Demo/Program.cs)
Saved summary: [Events-and-AsyncAwait-Progress-Summary.md](./Events-and-AsyncAwait-Progress-Summary.md)

## Next Session

1. Finish `Practice-Exercise/` from scratch.
2. Do a LINQ syntax-reading drill: `Select`, lambda, `ToList`, `List<Task<T>>`, and `Task.WhenAll(...)` type flow.
3. Review `CancellationToken`: token observation, callback registration, and safe cancellation points.
4. Re-run `Async-Demo/` and explain the timestamps.
5. Continue Async/Await with throttling parallel work using `SemaphoreSlim`.
