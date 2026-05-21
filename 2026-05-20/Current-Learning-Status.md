# Current C# Learning Status — May 20, 2026

## Current Position

The learner has completed the **full end-to-end unit testing fundamentals path**:

- LINQ + lambdas
- Delegates, events, inheritance
- Async/await + throttling (`SemaphoreSlim`)
- Core syntax foundations (brackets, scoping, member placement)
- LINQ drills (Where / Select / OrderBy / Aggregate / Dictionary counting)
- Events practice (Doorbell + TemperatureSensor)
- **Unit testing fundamentals (today)**
- **xUnit + project structure + .NET build internals (today)**
- **DLL / API / SDK ecosystem + NuGet packaging + SemVer (today, evening)**
- **IL preview (today, evening) — deferred for deeper coverage another day**

Sequence:

```text
LINQ + Lambdas -> Delegates -> Events -> Inheritance ->
Async/Await -> Throttling -> Core Syntax -> LINQ Drills ->
Events Practice -> [Unit Testing + xUnit + .NET internals] ✅
```

## Today's Active Topic

### Morning: Unit Testing Fundamentals + xUnit

Built a full testing skill stack from scratch in one session:

1. **Concept**: automated assertion vs manual verification.
2. **AAA pattern**: Arrange / Act / Assert.
3. **Test spy / flag variable**: testing void event side-effects via lambda closure.
4. **Red→green discipline**: deliberately FAIL once before trusting a test.
5. **One-test-per-method**: extracted manual `Main` runner into named test methods.
6. **Pain points of manual runner**: forgotten registration, hidden failures in noise, zero exit code, no filter.
7. **xUnit framework**: `[Fact]`, `Assert.True/False`, instance-per-test, naming convention.
8. **Solution/project/project reference**: built `.slnx` + `Core.csproj` + `Tests.csproj`.
9. **dotnet CLI**: `sln`, `classlib`, `xunit`, `sln add`, `add reference`, `test`, `test --filter`.
10. **Regression demo**: changed `>` to `<` in production code, both tests caught it with FAIL output, non-zero exit code, stack trace.

### Bonus topics covered (driven by learner questions)

- **Library vs host project**: a class library cannot run on its own; needs a host (web app / console / Functions / xUnit runner).
- **Production code should be pure logic**: no `Console.WriteLine` in `Core.dll`.
- **Compile-time vs runtime**:
  - References are for the **compiler**
  - **Reflection** is how xUnit discovers and invokes user code at runtime
  - Metadata + attributes (`[Fact]`) are the interface between user code and frameworks
- **NuGet vs dll**: `.nupkg` is the distribution format; `.dll` is the runtime artifact; build copies all transitive `.dll`s into `bin/{Config}/{TFM}/`.
- **The output folder is a self-contained runnable unit**: deployment unit = entire `bin/Release/{TFM}/publish/` directory.
- **Production debugging != IDE debugging**: production uses observability (logs / metrics / traces).

### Afternoon: Parameterized Tests + Payload Assertion + Diagnostic Workflow

Continued the same morning session with three deep dives:

1. **`[Theory]` + `[InlineData]`**: 3 equivalence classes (above/boundary/below) covered with 1 method instead of 3 `[Fact]`s. Boundary coverage gap (`>` vs `>=`) now plugged.
2. **Nullable reference types + object spy**: `ThresholdExceededEventArgs? captured = null` + lambda closure to retain the event payload, then assert all 3 fields (`Reading`, `Threshold`, `Timestamp`).
3. **Diagnostic workflow**: stack trace points to the Assert line, not the production bug line, because Assert is just a value comparator and the production code has already returned. Exception throws are the only case where stack trace pierces through to production code.

### Two more red-green cycles done this afternoon

- Cycle 2: Changed `>` to `>=` → only the new `[InlineData(100.0, false)]` boundary case failed. Other tests blindly passed because they used 99 / 50 / 200 which behave identically under both operators.
- Cycle 3: Changed `new ThresholdExceededEventArgs(celsius, _threshold, ...)` to `(_threshold, celsius, ...)` → only `AboveThreshold_RaisesEvent` failed (the only test that asserted the payload). The other 4 either didn't fire the event or only checked `eventFired`.

### Late afternoon: B1 + B2 + B3 paths completed

All three planned paths landed in one session — 16 tests total, all passing.

- **B1 — `[Theory]` + payload assertion**: New `RecordReading_FullVerification(double celsius, bool shouldRaise, double? expectedReading)` method. `double?` carries the optional expected payload. `if (shouldRaise)` branch checks `captured.Reading` and `captured.Threshold`; `else` branch asserts `Assert.Null(captured)`. 3 `[InlineData]` rows.
- **B2 — `Assert.Throws<T>`**: Added defensive `NaN` / `Infinity` validation to production code (`throw new ArgumentException("...", nameof(celsius));`). Three exception tests: `RecordReading_NaN_ThrowsArgumentException`, `RecordReading_PositiveInfinity_...`, `RecordReading_NegativeInfinity_...`. Introduced exception type hierarchy, `nameof`, generic `<T>` syntax, fail-fast vs silent-failure model.
- **B3 — `[MemberData]`**: Refactored 3 `[InlineData]` rows into `[MemberData(nameof(ThresholdTestData))]` referencing a `public static IEnumerable<object[]> ThresholdTestData` property with 8 rows including float boundary microshifts (`100.001` / `99.999`) and negative temperature (`-40.0`).

Final command output: `Test summary: total: 16, failed: 0, succeeded: 16, skipped: 0, duration: 1.3s`.

### Three deep architectural questions answered

- Exception capture pattern: `var ex = Assert.Throws<T>(...); Assert.Contains("...", ex.Message);`
- BCL exception hierarchy: `Exception → SystemException → ArgumentException → ArgumentNullException / ArgumentOutOfRangeException` — `Assert.Throws<T>` is precise, `Assert.ThrowsAny<T>` accepts subclasses.
- Real-world project structure: src/tests separation, 1:1 mirror mapping, Clean Architecture 4 layers (Domain / Application / Infrastructure / Presentation), 5-15 projects in mid-size solution, testing pyramid (lots of unit / some integration / few E2E).

## Completed Files

- `2026-05-18/Practice-Exercise/Program.cs`
  - Manual mini test runner (`Test_AboveThreshold_RaisesEvent`, `Test_AtOrBelowThreshold_DoesNotRaiseEvent`)
- `2026-05-20/TemperatureSensor.slnx`
- `2026-05-20/TemperatureSensor.Core/`
  - `TemperatureSensor.cs` — production code with sensor + event args, no IO
- `2026-05-20/TemperatureSensor.Tests/`
  - `TemperatureSensorTests.cs` — two passing `[Fact]` tests
- `2026-05-20/Unit-Testing-Fundamentals-Summary.md` — full topic summary + review questions

## Important Corrections Logged Today

- `Test_` prefix is from manual-runner era; `[Fact]` makes it redundant.
- `Assert.True(!x)` should be `Assert.False(x)` — intent-revealing.
- `static void` is not idiomatic for xUnit tests; use `public void`.
- `DoesNotRaise` not `DoesNotRaises` — `does` carries third-person singular.
- Production code (`Core`) should not have `Console.WriteLine` — that's the host's responsibility (separation of concerns).
- Boundary precision in test naming: condition is `>` not `>=`, so 100 is "not fired" — name it `AtOrBelowThreshold`, not `BelowThreshold`.
- "Cross-project reference" is not "remote call"; both projects run in the same process, just linked via project reference.
- The `.nupkg` file is never loaded at runtime; only its extracted `.dll` files are.
- xUnit discovers tests via reflection on the test assembly, not via any reference graph.
- **Afternoon additions:**
  - C# naming convention: local variables and parameters must be **camelCase** (`celsius`, `shouldRaise`, `eventFired`), not all-lowercase concatenation (`sensortemp`, `shouldraise`, `eventfired`).
  - `var x = null;` does **not** compile — `null` has no type for the compiler to infer; you must declare an explicit type like `ThresholdExceededEventArgs? captured = null`.
  - `Assert.Equal(expected, actual)` arg order is sacrosanct — reversing it makes the failure message misleading ("Expected: 100, Actual: 101" when the test wrote `Assert.Equal(101, ...)`).
  - Tests report symptoms (WHAT is wrong), not causes (WHERE is wrong). The stack trace points at the Assert line, never at the production bug line — unless the production code throws.
- **Late afternoon additions:**
  - Test methods **cannot be overloaded** — `xUnit1024` rule from xUnit's Roslyn analyzer (not the C# compiler). Reason: CI uses method name as the unique test ID, so duplicates collide. Production code can still overload normally.
  - **Roslyn analyzer** = compile-time plugin that inspects source code and emits diagnostics (warning / error / info / hidden). Each rule has an ID like `xUnit1024` / `CA1822` / `IDE0001`.
  - `Assert.Throws<T>` is **precise type matching** — subclasses do **not** count. Use `Assert.ThrowsAny<T>` to accept subclasses. `Assert.Throws<T>` also **returns** the captured exception so you can `Assert.Contains("...", ex.Message)` afterwards.
  - `nameof(celsius)` is a compile-time operator that produces the string `"celsius"`. Refactor-safe — renaming the symbol updates the string too. Always prefer over string literals when referring to a symbol.
  - The standard `ArgumentException(message, paramName)` constructor expects the parameter name as the second argument — pass it via `nameof(...)`.
  - `double.IsNaN(x)` is the correct NaN check; `x == double.NaN` is always false because **`NaN != NaN`** by IEEE 754 rules.
  - `=>` has two unrelated meanings depending on position: in a **value position** (`Func<int,int> f = x => x*2`) it's a lambda; in a **member declaration position** (`public int X => 42`) it's an expression-bodied member, equivalent to `{ return 42; }`. C# 6 introduced expression-bodied members.
  - `[MemberData]` accepts three member shapes — all must be `public static`:
    1. Property + expression body: `public static IEnumerable<object[]> Data => new[] { ... };` (runs every access)
    2. Field + initializer: `public static IEnumerable<object[]> Data = new[] { ... };` (runs once at class load)
    3. Method: `public static IEnumerable<object[]> Data() => new[] { ... };` (runs every call)
    Property form is the default choice.
  - Modern alternative to `IEnumerable<object[]>`: `TheoryData<T1, T2, ...>` — strongly typed, compile-time checked, no boxing.
  - Real-world solution layout: `src/` and `tests/` are sibling folders, each contains 5-15 projects, with **1:1 mirror mapping** between a production project and its test project. Clean Architecture splits production into Domain → Application → Infrastructure → Presentation, with dependency arrows always pointing inward.

## Future Topics (Priority Order)

1. **DLL / API / SDK concepts** — terminology + ecosystem clarity (requested by learner today).
2. **Library vs host + observability basics** — `ILogger`, structured logging, Application Insights.
3. ~~Boundary value testing / test coverage~~ ✅ **DONE** (Part 2 — `[Theory]` + `[InlineData]`, 3 equivalence classes).
4. **Dependency injection + service lifetimes** — `AddSingleton/Scoped/Transient`, correct `SemaphoreSlim` lifetime.
5. **Test doubles**: mock / stub / fake / spy distinction; Moq / NSubstitute basics.
6. **TDD (test-driven development)** — red / green / refactor cycle.
7. **Integration tests vs unit tests**.
8. **CI/CD basics** — GitHub Actions or Azure DevOps running `dotnet test`.
9. ~~`Assert.Throws<T>`~~ ✅ **DONE** (Part 3 — B2 path: NaN / +∞ / -∞ on TemperatureSensor).
10. ~~`[MemberData]`~~ ✅ **DONE** (Part 3 — B3 path: 8-row static property data source). `[ClassData]` still queued for future deep dive.
11. ~~`[Theory]` upgrade to also assert payload~~ ✅ **DONE** (Part 3 — B1 path: `RecordReading_FullVerification` with `double?` + branch).
12. **`[ClassData]` deep dive** — cross-test-class reusable data sources (only previewed today).
13. **`TheoryData<T1, T2, ...>` strongly-typed data sources** — modern replacement for `IEnumerable<object[]>` (only previewed today).
14. **Expression-bodied members systematically** — on properties, methods, indexers, constructors, finalizers (today only saw the property + method forms).

## Next Session Recommendation

The learner finished a **massive** day: 16 passing tests, 5 test patterns (`[Fact]` / `[Theory]+[InlineData]` / `[Theory]+[MemberData]` / `Assert.Throws<T>` / object-spy with payload capture), exception hierarchy, defensive programming, Roslyn analyzer concept, and Clean Architecture overview.

Three natural next steps, learner can pick:

- **Option 1 — Consolidate via quiz**: "quiz me" session against the **83 review questions** in `Unit-Testing-Fundamentals-Summary.md` (Parts 1 + 2 + 3). Categories A–M.
- **Option 2 — Clarify ecosystem**: take Future Topic #1 (DLL / API / SDK) off the queue. Explain how `.nupkg`, `.dll`, public APIs, and SDKs relate. Touch on NuGet versioning and SemVer.
- **Option 3 — Modernize the test data source**: convert today's `IEnumerable<object[]>` to `TheoryData<double, bool>` and observe the compile-time type-check benefit. Then explore `[ClassData]` for cross-test-class reuse.

## Today's Final State

```text
TemperatureSensor.slnx
├── TemperatureSensor.Core/        ← production: NaN/Infinity defensive + threshold detection
│   └── TemperatureSensor.cs
└── TemperatureSensor.Tests/       ← 16 tests, all green
    └── TemperatureSensorTests.cs
        ├── [Fact] AboveThreshold_RaisesEvent              (payload-asserting)
        ├── [Fact] AtOrBelowThreshold_DoesNotRaiseEvent
        ├── [Theory] RecordReading_RaisesEventOnlyWhenAboveThreshold  (8 [MemberData] rows)
        ├── [Theory] RecordReading_FullVerification        (3 [InlineData] rows)
        ├── [Fact] RecordReading_NaN_ThrowsArgumentException
        ├── [Fact] RecordReading_PositiveInfinity_ThrowsArgumentException
        └── [Fact] RecordReading_NegativeInfinity_ThrowsArgumentException
```

Final `dotnet test` result: **total: 16, failed: 0, succeeded: 16, skipped: 0**.

## Evening Session: DLL / API / SDK Ecosystem + NuGet Packaging

After the testing marathon, picked Future Topic #1 ("DLL / API / SDK clarity") and turned
it into a hands-on packaging exercise.

### Concepts covered

- **DLL = compiled IL + metadata + manifest.** Single artifact, loaded at runtime, can't
  run on its own — needs a host.
- **API = the public surface** of a DLL. Defined by accessibility modifiers (`public` /
  `protected` are API; `private` / `internal` are implementation). `_threshold` correctly
  identified as not-API by the learner.
- **Library vs Framework vs SDK**: control-flow rule — library = *you call it*; framework =
  *it calls you* (inversion of control); SDK = curated toolkit of libraries + tools for
  a specific platform/service.
- **`.nupkg` is just a ZIP** containing OPC plumbing + `.nuspec` manifest + `lib/<TFM>/`
  payload DLLs.
- **The 4 required `.nuspec` fields**: `id`, `version`, `authors`, `description`. Others
  optional. (Learner got 2/4 — missed `authors` and `description`, included `dependencies`
  which is actually optional.)
- **SemVer**: PATCH = bug fix only, MINOR = additive backward-compatible features,
  MAJOR = breaking change. Learner correctly identified an overload addition as MINOR.
- **Multi-targeting**: a single `.nupkg` can ship multiple DLLs (`lib/net8.0/...dll`,
  `lib/net10.0/...dll`). NuGet picks the closest backward-compatible match for the
  consuming project's TFM.

### Hands-on done

- **Pass 1**: `dotnet pack -c Release` on bare `.csproj` → produced
  `TemperatureSensor.Core.1.0.0.nupkg` (4,303 bytes) with garbage defaults
  (`<authors>` = project name, `<description>` = "Package Description").
- **Pass 2**: Enriched `.csproj` with `PackageId`, `Version`, `Authors`, `Description`,
  `PackageTags`, `PackageLicenseExpression`, `PackageReleaseNotes`, `RepositoryUrl`.
  Produced `Learning.TemperatureSensor.Core.1.1.0.nupkg` (5,073 bytes) with a fully
  populated `.nuspec`.
- **Pass 3 (multi-targeting)**: Switched to `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>`
  → produced `Learning.TemperatureSensor.Core.1.1.1.nupkg` containing both
  `lib/net8.0/...dll` AND `lib/net10.0/...dll`.

### Multi-targeting syntax — 3 things to get right

1. Tag name is **plural**: `<TargetFrameworks>` not `<TargetFramework>`.
2. Separator is **semicolon** `;`, not comma.
3. DLLs land in `lib/<TFM>/` subfolders inside the `.nupkg`.

Hit error `NETSDK1046: To multi-target, use the 'TargetFrameworks' property instead`
when only the separator was changed but tag stayed singular — fixed by pluralizing the
tag.

## Evening Session Part 2: IL Inspection Preview (deferred)

Installed `ilspycmd` global tool. Decompiled and viewed raw IL for the
`TemperatureSensor` class. **Learner found IL too dense and chose to defer deeper
mastery to a future session — this was a smart trade-off.**

### What was previewed (high-level understanding only)

- **Auto-property `Reading { get; }`** compiles to:
  - A private backing field `<Reading>k__BackingField` (angle brackets are illegal in C#
    source — compiler reserves the `<X>` prefix for its own names).
  - A `[CompilerGenerated]` `get_Reading()` method with 3 IL instructions
    (`ldarg.0; ldfld; ret`).
  - A `.property` metadata block linking the method to the property name.
- **`public event EventHandler<T> Foo;`** compiles to:
  - A private delegate field named `Foo` (same name as the event — distinguished by kind).
  - An `add_Foo` method that is a **thread-safe CAS loop** using
    `Interlocked.CompareExchange` + `Delegate.Combine`. (~30 lines of IL.)
  - A symmetric `remove_Foo` method using `Delegate.Remove`.
  - A `.event` metadata block pairing `add_/remove_` as a named event.
- **`+=` on an event** compiles to a call to `add_X(...)`. The CAS loop is what makes
  the field-like event syntax thread-safe under contention.

### Big-picture takeaway captured

C# is high-level syntactic sugar over IL primitives. Examples logged:

| C# feature | IL reality |
|---|---|
| `public T X { get; }` | Field + `get_X()` method + `.property` metadata |
| `public event T Foo;` | Field + `add_Foo` (CAS) + `remove_Foo` (CAS) + `.event` metadata |
| `async/await` | State-machine class with `MoveNext()` (not yet covered) |
| `lambda` | Anonymous compiler-generated class with `Invoke` + closure fields |
| `using x = ...;` | `try { ... } finally { x.Dispose(); }` |
| `foreach` | `GetEnumerator()` + `MoveNext()` + `Current` in try/finally |
| `?.` | Null check + branch |
| `nameof(x)` | A compile-time string literal — zero runtime cost |

## Important Corrections Logged Evening

- **Multi-targeting tag**: `<TargetFramework>` (singular) takes ONE TFM. To use multiple,
  switch to `<TargetFrameworks>` (plural) with semicolon separators. Error `NETSDK1046`
  explicitly tells you this — read the SDK error messages, they're often verbatim
  instructions.
- **`net6.0` reached end-of-support November 2024** — by 2026 it's unsupported. For
  modern multi-targeting demos, use `net8.0` (LTS) + `net10.0` (current).
- **`<Version>`, `<PackageReleaseNotes>`, and the comment explaining the bump** are a
  three-way contract — keep them in sync, or future-you will be confused which version
  shipped which feature.
- **`dotnet pack` produces garbage defaults if metadata is missing**: `<authors>`
  defaults to the project name, `<description>` defaults to the literal string
  "Package Description". This is a feature, not a bug — it forces you to notice that
  you forgot to set them.
- **The `<X>k__BackingField` naming convention** uses angle brackets that are illegal in
  C# source. This is intentional: the compiler reserves the `<>` prefix so user code
  can never collide with compiler-generated names.

## Files added/modified this evening

- `2026-05-20/TemperatureSensor.Core/TemperatureSensor.Core.csproj` — added full NuGet
  metadata block, switched to `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>`.
- `2026-05-20/TemperatureSensor.Core/bin/Release/`:
  - `net8.0/TemperatureSensor.Core.dll`
  - `net10.0/TemperatureSensor.Core.dll`
  - `TemperatureSensor.Core.1.0.0.nupkg` (Pass 1)
  - `Learning.TemperatureSensor.Core.1.1.0.nupkg` (Pass 2 — single TFM with metadata)
  - `Learning.TemperatureSensor.Core.1.1.1.nupkg` (Pass 3 — multi-TFM)

## Future Topics (Priority Order) — Updated

1. ~~DLL / API / SDK concepts~~ ✅ **DONE** (evening session).
2. **Library vs host + observability basics** — `ILogger`, structured logging, Application
   Insights.
3. **Dependency injection + service lifetimes** — `AddSingleton/Scoped/Transient`,
   correct `SemaphoreSlim` lifetime.
4. **Test doubles**: mock / stub / fake / spy distinction; Moq / NSubstitute basics.
5. **TDD (test-driven development)** — red / green / refactor cycle.
6. **Integration tests vs unit tests**.
7. **CI/CD basics** — GitHub Actions or Azure DevOps running `dotnet test`.
8. **`[ClassData]` deep dive** — cross-test-class reusable data sources.
9. **`TheoryData<T1, T2, ...>` strongly-typed data sources**.
10. **Expression-bodied members systematically** — on properties, methods, indexers,
    constructors, finalizers.
11. **IL deeper coverage (deferred today)** — full IL walk of `add_X` CAS loop,
    `async/await` state machine, lambda closures. Revisit after more C# foundations land.
12. **Local NuGet feed round-trip (deferred today)** — publish to a folder feed, install
    back into Tests project to prove the consume loop.

