# LINQ Fundamentals — Session Summary
**Date**: April 16, 2026  
**Duration**: Full session  
**Topics**: Array initialization syntax, LINQ Big 5, Lazy evaluation, Memory behavior, IGrouping, Dictionary ordering

---

## 1. Array Initialization Syntax (Warm-up)

```csharp
// C# 12+ collection expression
string[] t1 = ["as", "de", "ttt"];

// Explicit new (all versions)
string[] t2 = new string[] { "as", "de", "ttt" };

// Implicit new — only works in variable declarations
string[] t3 = { "as", "de", "ttt" };
```

- Single quotes `'a'` = `char`, double quotes `"a"` = `string`
- `{ }` shorthand fails when passing to a method

---

## 2. LINQ Big 5 Methods

### `.Where()` — Filter
```csharp
var evens = numbers.Where(n => n % 2 == 0);
```

### `.Select()` — Transform
```csharp
var upper = names.Select(n => n.ToUpper());
```

### `.OrderBy()` / `.OrderByDescending()` — Sort
```csharp
var sorted = numbers.OrderBy(n => n);
var desc = numbers.OrderByDescending(n => n);
var byLength = names.OrderBy(n => n.Length);  // sort by a property
```

### `.First()` / `.FirstOrDefault()` — Get one item
```csharp
int first = numbers.First();                          // crashes if empty
int safe = numbers.FirstOrDefault();                  // returns 0 if empty
string city = cities.FirstOrDefault(n => n.Length > 5); // shorthand with predicate
```

### `.GroupBy()` — Group items
```csharp
var grouped = names.GroupBy(n => n[0]);  // group by first letter
foreach (var group in grouped)
{
    Console.WriteLine($"Key: {group.Key}");
    foreach (var item in group) { Console.WriteLine($"  {item}"); }
}
```

---

## 3. Chaining

LINQ methods chain with dots, read like English:
```csharp
var result = employees
    .Where(n => n.Length > 3)       // filter
    .OrderBy(n => n)                // sort
    .GroupBy(m => m[0]);            // group
```

**Order matters**: `.OrderBy()` BEFORE `.GroupBy()` sorts items within groups.

---

## 4. Lazy Evaluation (Deferred Execution)

### Key rules:
- `.Where()`, `.Select()`, `.OrderBy()`, `.GroupBy()` are all **deferred** — no work happens until you iterate
- Without iterating (foreach, .ToList(), etc.), literally **nothing happens**
- The lazy result is a **recipe**, not stored data

### Streaming vs Buffering:
| Method | Streams one at a time? |
|---|---|
| `.Where()` / `.Select()` | Yes — true streaming |
| `.OrderBy()` / `.GroupBy()` | No — must load all items into memory |

### Gotchas:
1. **Source can change** between definition and iteration (lazy reads source at iteration time)
2. **Re-iterating** runs all lambdas again (use `.ToList()` to materialize and reuse)
3. **Discarding the result** means zero work was done

### I/O buffering:
- `File.ReadLines()` uses `StreamReader` with a 4-8KB buffer
- "One at a time" = memory management, NOT unbuffered disk I/O
- Disk reads are batched internally; your code just sees one item at a time

---

## 5. Memory Behavior

- **Strings are immutable** — `.ToUpper()` creates a NEW string, never modifies the original
- **LINQ never mutates** the source collection
- `.Select()` produces new objects at new heap locations; original list is untouched

---

## 6. IGrouping vs Dictionary

| Feature | `.GroupBy()` (`IGrouping`) | `Dictionary` |
|---|---|---|
| Access by key | Must iterate | Direct lookup `dict['A']` |
| Order | Preserves first-appearance order | No guaranteed order |
| Random access | No | Yes |

- Dictionary uses a hash table → iteration order depends on hash slots, not insertion order
- Use `SortedDictionary<K,V>` if you need ordered key iteration
- Convert with `.ToDictionary(g => g.Key, g => g.ToList())`

---

## 7. Common Mistakes Made Today

| Mistake | Fix |
|---|---|
| `.where()` lowercase | `.Where()` — PascalCase for all methods |
| `.length` lowercase | `.Length` — PascalCase for all properties |
| `Lenght` typo | `Length` |
| `name` instead of `names` | Check variable names |
| `.GroupBy().OrderBy()` | `.OrderBy().GroupBy()` — sort before grouping |
| Single quotes for strings `'as'` | Double quotes `"as"` |

---

## Review Questions for Next Day

### Basics
1. What's the difference between `'a'` and `"a"` in C#?
2. What are three ways to initialize a `string[]`? Which one only works in variable declarations?

### LINQ Where & Select
3. Write a LINQ statement to filter a `List<int>` for numbers divisible by 3.
4. Write a LINQ statement to transform a `List<string>` of names into their lengths (e.g., `"alice"` → `5`).
5. What happens if you write `names.Select(n => n.ToUpper());` without assigning it to a variable or iterating?

### Lazy Evaluation
6. Is `.Where()` lazy or eager? What about `.ToList()`?
7. You create `var result = nums.Where(n => n > 5);` then add a number `10` to `nums`. Does `result` include `10` when you iterate? Why?
8. What happens if you iterate a lazy LINQ result twice? Why is this a problem with database queries?
9. Does `File.ReadLines()` actually read one line per disk I/O call? Explain.

### OrderBy & GroupBy
10. You want names sorted alphabetically within each group. Should `.OrderBy()` come before or after `.GroupBy()`?
11. What type does `.GroupBy()` return? Is it a Dictionary?
12. What does "preserves order of first appearance" mean for `.GroupBy()`?

### Dictionary
13. Why does Dictionary have no guaranteed iteration order?
14. When should you use `Dictionary` vs `SortedDictionary` vs `List`?

### Combo Challenge
15. Given `List<int> { 10, 25, 3, 47, 8, 31, 15 }`, write one LINQ chain that: filters for numbers > 10, sorts descending, and takes the first 3 results as a `List<int>`.
