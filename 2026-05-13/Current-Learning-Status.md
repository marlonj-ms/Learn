# Current C# Learning Status — May 17, 2026

## Where We Are Now

Your latest formal learning checkpoint is now May 17.

Today you completed the standard .NET event pattern and added the inheritance concepts needed to understand production-style event raising methods.

Tomorrow should begin with a short quiz from `2026-05-17/Events-Virtual-Sealed-Summary.md`, then move into `Async/Await` fundamentals.

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

### May 17 — Standard Event Pattern + Inheritance Basics

- `EventHandler<TEventArgs>` as the standard .NET event delegate shape
- Custom `EventArgs` classes such as `OrderCompletedEventArgs`
- `sender` as the event sender and `e` as event data
- Event handler methods such as `HandleOrderCompleted`
- `+=` subscribes handler methods into an event invocation list
- `OnOrderCompleted(...)` as an event raising method
- Separation between business method and event notification method
- Inheritance syntax with `DerivedClass : BaseClass`
- Base class vs derived class vocabulary
- Variable type vs real object type
- Polymorphism with `virtual` and `override`
- `sealed class` as class-level inheritance blocking
- `sealed override` as method-level override blocking
- Nested class vs inheritance
- `base.SomeMethod(...)` calls the parent class version of a method
- Why inheritable event publishers often use `protected virtual OnSomethingHappened(...)`

## Current Position

You are now at the transition point from:

`LINQ + Lambdas` → `Delegates` → `Events` → `Inheritance basics` → `Async/Await`

Current position:

`✅ LINQ + Lambdas` → `✅ Delegates` → `✅ Events` → `✅ Inheritance basics` → `⬜ Async/Await`

This is the right next sequence because:

- LINQ uses lambdas heavily.
- Lambdas are often stored in delegate types like `Func<>` and `Action<>`.
- Events are built on delegates.
- Production event patterns often use inheritance hooks such as `protected virtual OnSomethingHappened(...)`.
- Async APIs often accept callbacks, cancellation delegates, and task continuations.

## Recommended Next Lesson

Start tomorrow with a **May 17 review quiz**, then begin **Async/Await Fundamentals**.

Tomorrow's first quiz should cover:

1. What is `EventHandler<OrderCompletedEventArgs>`?
2. What are `sender` and `e`?
3. What does `+=` do when used with an event?
4. Why is `HandleOrderCompleted` a handler method, not an event?
5. Why do production classes often use `OnOrderCompleted(...)`?
6. What does `virtual` mean?
7. What does `override` mean?
8. What is the difference between `sealed class` and `sealed override`?
9. What does `base.OnOrderCompleted(e)` do?
10. Why does `protected virtual` make sense only when a class can be inherited?

Then start async with:

1. Synchronous vs asynchronous execution
2. `Task` as a promise of future completion
3. `Task<T>` as a future result
4. `await` as pause-this-method-until-the-task-completes
5. Why async code helps avoid blocking threads

## Next Files

- `2026-05-15/Delegates-Deep-Dive.cs` — runnable delegate deep-dive file
- `2026-05-15/Delegates-Deep-Dive-Summary.md` — delegate session summary and review questions
- `2026-05-15/Events-Fundamentals.cs` — runnable event fundamentals file
- `2026-05-15/Events-Fundamentals-Progress-Summary.md` — event progress summary and tomorrow quiz questions
- `2026-05-17/Events-Standard-Pattern.cs` — runnable standard event pattern file
- `2026-05-17/Events-Standard-Pattern.csproj` — project file for the standard event pattern lesson
- `2026-05-17/Virtual-Sealed-Basics/Program.cs` — runnable inheritance / virtual / sealed lesson
- `2026-05-17/Virtual-Sealed-Basics/Virtual-Sealed-Basics.csproj` — project file for the virtual/sealed lesson
- `2026-05-17/Events-Virtual-Sealed-Summary.md` — full May 17 summary and next-day quiz questions
- `dailyread/dailyread-2026-05-17.html` — 5-minute review page for May 17
