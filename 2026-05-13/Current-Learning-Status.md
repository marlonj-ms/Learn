# Current C# Learning Status — May 15, 2026

## Where We Are Now

Your latest formal learning checkpoint is now May 15.

Today you completed a deep delegate lesson and started event fundamentals. Tomorrow should begin with an event quiz before continuing the event lesson.

## Completed Topics

### April 14 — Linked Lists, Operators, and Add Two Numbers

- Singly linked list structure with `ListNode`
- Self-referential classes
- `%` modulo operator
- Integer overflow risks
- Carry-based linked-list addition
- Ternary operator and null checks

### April 15 — `IList<IList<string>>` Deep Dive

- Stack vs heap mental model
- Reference types vs value types
- String immutability
- Arrays vs `List<T>`
- How `List<T>` grows internally
- Generics
- Interfaces and `IList<T>`
- Nested generic type rules

### April 16 — LINQ Fundamentals

- Array initialization syntax
- `.Where()`
- `.Select()`
- `.OrderBy()` / `.OrderByDescending()`
- `.FirstOrDefault()`
- `.GroupBy()`
- Lazy evaluation / deferred execution
- Streaming vs buffering LINQ operators
- `IGrouping<TKey, TElement>` vs `Dictionary<TKey, TValue>`

## In Progress

### April 17 — Lambda Fundamentals

You started a runnable lambda practice file covering:

- `Func<T>`
- `Action<T>`
- Expression lambdas
- Statement lambdas
- Lambdas inside LINQ
- Deferred execution
- Closures and captured variables

### May 14 — Extension Methods + Lambda Deep Dive

- Extension method rules: `static class` + `static method` + `this` first parameter
- Extension method lookup with `using` and receiver type
- Function factories returning lambdas
- Closure capture behavior
- Function composition with `AndThen`

### May 15 — Delegates Deep Dive

- `delegate` as a method-signature type
- `Func<>`, `Action<>`, and `Predicate<>` as built-in delegate types
- Delegate object internals: `target` + `method`
- Static method target vs instance method target
- Closure object mental model
- Custom delegate vs `Func<>`
- Nominal typing: same signature does not mean same delegate type
- Lambda wrapper pattern
- Multicast delegates and invocation lists
- `+=` / `-=` semantics
- Return value behavior for multicast delegates
- Callback execution order
- Null-safe delegate invocation with `?.Invoke()`

### May 15 — Event Fundamentals Started

- `public delegate field` is too open
- `private delegate field` is too closed
- `public event` gives a controlled subscription boundary
- Event member vs field vs local variable vs property
- `event` cannot be declared inside a method
- `+=` stores handlers in the event invocation list; it does not execute them immediately
- `Invoke` triggers the stored handlers
- Anonymous lambda unsubscribe pitfall
- `EventHandler<TEventArgs>` introduced slowly
- `sender` means the object that triggered the event
- `e` means the event data object

## Current Position

You are now at the transition point from:

`LINQ + Lambdas` → `Delegates` → `Events` → `Async/Await`

Current position:

`✅ LINQ + Lambdas` → `✅ Delegates` → `🔶 Events` → `⬜ Async/Await`

This is the right next sequence because:

- LINQ uses lambdas heavily.
- Lambdas are often stored in delegate types like `Func<>` and `Action<>`.
- Events are built on delegates.
- Async APIs often accept callbacks, cancellation delegates, and task continuations.

## Recommended Next Lesson

Start tomorrow with an **Event Fundamentals Quiz**, then continue **EventHandler<TEventArgs>** and the standard event pattern.

Tomorrow's first quiz should cover:

1. Why is `public Action<string>? OrderCompleted;` dangerous?
2. Why is `private Action<string>? orderCompleted;` too closed?
3. What does `event` allow external code to do?
4. What does `event` prevent external code from doing?
5. What is the difference between local variable, field, and event member?
6. What does `+=` do for an event?
7. When does a subscribed lambda actually execute?
8. In `EventHandler<TEventArgs>`, what are `sender` and `e`?

## Next Files

- `2026-05-15/Delegates-Deep-Dive.cs` — runnable delegate deep-dive file
- `2026-05-15/Delegates-Deep-Dive-Summary.md` — delegate session summary and review questions
- `2026-05-15/Events-Fundamentals.cs` — runnable event fundamentals file
- `2026-05-15/Events-Fundamentals-Progress-Summary.md` — event progress summary and tomorrow quiz questions
