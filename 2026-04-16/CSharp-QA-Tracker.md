# C# Learning Q&A Tracker — April 16, 2026

---

## Summary Stats
- **Total Questions Asked**: 10
- **Topics Covered**: C# Syntax (Array Initialization), LINQ (Where, Select, OrderBy, FirstOrDefault, GroupBy, Lazy Evaluation, Memory Behavior, IGrouping vs Dictionary, Dictionary Hash Table Order)

---

## Questions Log

### Q1 — April 16, 2026
- **Topic**: C# Syntax — Array Initialization
- **Difficulty**: Beginner
- **Question**: What's the difference between `string[] t1 = ["as","de","ttt"]`, `string[] t2 = new string[] { "as","de","ttt" }`, and `string[] t3 = { "as","de","ttt" }`?
- **Key Concepts Covered**:
  - Single quotes (`'a'`) are for `char`, double quotes (`"a"`) are for `string`
  - `new string[] { ... }` — explicit initializer, works in all C# versions
  - `{ ... }` — implicit shorthand, works only in variable declarations (not method arguments)
  - `[ ... ]` — collection expression, C# 12+ only, works almost everywhere
  - All three produce the same `string[]` result
  - `{ ... }` shorthand fails when passing to a method; `[ ... ]` and `new string[] { ... }` work

### Q2 — April 16, 2026
- **Topic**: LINQ — `.Where()` introduction
- **Difficulty**: Beginner
- **Question**: Baseline test — filter even numbers without LINQ, then introduction to `.Where()` with lambda
- **Key Concepts Covered**: `foreach`+`if` vs `.Where()`, lambda syntax `n => condition`, PascalCase for LINQ methods

### Q3 — April 16, 2026
- **Topic**: LINQ — `.Where()` practice
- **Difficulty**: Beginner
- **Question**: Get numbers greater than 5 using `.Where()`
- **Result**: ✅ Correct logic, minor case error (`.where` → `.Where`)

### Q4 — April 16, 2026
- **Topic**: LINQ — `.Select()` introduction
- **Difficulty**: Beginner
- **Question**: Convert list of names to uppercase using `.Select()`
- **Result**: ✅ Perfect — `names.Select(n => n.ToUpper())`

### Q5 — April 16, 2026
- **Topic**: LINQ — Memory behavior & lazy evaluation
- **Difficulty**: Intermediate
- **Question**: Does `.Select()` modify strings in-place on the heap?
- **Key Concepts Covered**:
  - Strings are immutable — `.ToUpper()` creates a NEW string, never modifies original
  - LINQ never mutates the source collection
  - `.Select()` without storing the result = nothing happens (lazy evaluation)
  - Lazy = recipe, not cooked food. Must iterate to trigger work

### Q6 — April 16, 2026
- **Topic**: LINQ — Lazy evaluation deep dive
- **Difficulty**: Intermediate
- **Question**: How does lazy evaluation work with file I/O? Doesn't one-line-at-a-time cause high I/O cost?
- **Key Concepts Covered**:
  - `StreamReader` uses a 4-8KB internal buffer — disk reads are batched
  - "One at a time" refers to memory management, not disk I/O
  - Re-iterating a lazy sequence runs all lambdas AGAIN (use `.ToList()` to materialize)
  - Lazy vs eager method cheat sheet

### Q7 — April 16, 2026
- **Topic**: LINQ — Source mutation with lazy evaluation
- **Difficulty**: Intermediate
- **Question**: What does `nums.Select(n => n * 2)` return after adding an item to `nums`?
- **Result**: ✅ Correct — Answer B: `{ 2, 4, 6, 8 }` because lazy reads source at iteration time

### Q8 — April 16, 2026
- **Topic**: LINQ — `.OrderBy()` / `.OrderByDescending()`
- **Difficulty**: Beginner
- **Question**: Filter scores > 50 and sort highest to lowest
- **Result**: ✅ Perfect — `scores.Where(n => n > 50).OrderByDescending(y => y)`

### Q9 — April 16, 2026
- **Topic**: LINQ — `.FirstOrDefault()`
- **Difficulty**: Beginner
- **Question**: Get first city longer than 5 characters safely
- **Result**: ✅ Correct logic, minor case error (`.length` → `.Length`). Learned shorthand `.FirstOrDefault(predicate)`

### Q10 — April 16, 2026
- **Topic**: LINQ — `.GroupBy()`, `IGrouping`, chain order, Dictionary internals
- **Difficulty**: Intermediate
- **Question**: Group employees by first letter with filtering and sorting; what type does GroupBy return; Dictionary order guarantee
- **Key Concepts Covered**:
  - `.GroupBy()` returns `IEnumerable<IGrouping<TKey, TElement>>`, not a Dictionary
  - `.OrderBy()` should come BEFORE `.GroupBy()` to sort items within groups
  - `.GroupBy()` and `.OrderBy()` are deferred but buffer everything when iterated
  - GroupBy preserves first-appearance order of keys
  - Dictionary has no guaranteed iteration order due to hash table internals
  - `SortedDictionary` for ordered key iteration

<!-- New questions will be appended here -->
