# IList<IList<string>> Deep Dive — April 15, 2026

## Original Question
```csharp
public class Solution {
    public IList<IList<string>> GroupAnagrams(string[] strs) { }
}
```
What does this method signature mean?

---

## Concepts Covered (Bottom-Up)

### 1. String in Memory
- `string` is a **reference type** — lives on the **heap**
- Variable on the **stack** holds a **reference** (not a pointer) to the string object on the heap
- Reference = managed by garbage collector, safe. Pointer = manual, unsafe
- Reference size = **8 bytes on 64-bit CPU** (tied to CPU architecture)
- `int` size = **4 bytes always** (defined by C# language spec, NOT CPU)

### 2. Strings Are Immutable
- Once created, a string's characters **can NEVER be changed**
- `name[0] = 'X';` → **compiler error**
- `name = "hello";` does NOT modify the old string — it creates a **new** string object and reassigns the reference
- Old string becomes **orphaned** → garbage collector cleans it up
- Implication: string concatenation in loops creates many throwaway objects (use `StringBuilder` instead)

### 3. Reference Assignment
```csharp
string a = "cat";
string b = a;      // copies the REFERENCE, not the string
a = "dog";         // a points to new "dog", b still points to "cat"
```
- `b` is NOT affected when `a` is reassigned
- `"cat"` is NOT orphaned because `b` still holds a reference to it

### 4. Array (`string[]`) in Memory
- Array is a **reference type** — the array object lives on the **heap**
- For `string[]`: each cell stores a **reference** (8 bytes), NOT the string itself
- References are stored **contiguously** — easy offset math: `start + index × 8`
- The actual strings are **scattered** elsewhere on the heap
- Changing `arr[0] = "Tomato"` is safe — just swaps the 8-byte reference, doesn't overwrite characters
- **Fixed size** — cannot Add, Remove, or Insert

### 5. List<string> in Memory
- `List<T>` is a **class** (reference type) — wrapper object on the heap
- Internally contains: `_items` (reference to an internal array) + `_size` (count)
- **3 layers**: stack → List object → internal array → string objects
- vs Array's **2 layers**: stack → array → string objects
- Can **grow**: when full, creates a new array (double the size), copies references, orphans old array
- `Add()`, `Remove()`, `Insert()` all work

### 6. How List Grows
1. Internal array is full (e.g., 4/4 slots)
2. Create **new** array, double the size (4 → 8)
3. **Copy references** (not strings!) from old array to new array
4. Add the new item in the next slot
5. Update `_items` in the List object to point to the new array
6. Old array is orphaned → GC cleans up
- Strings themselves **never move** — only references are copied

### 7. Generics (`<T>`)
- `List<string>` = container labeled "STRINGS ONLY"
- `List<int>` = container labeled "INTEGERS ONLY"
- Provides **type safety** — compiler catches wrong types at compile time
- `<T>` is a placeholder — you fill in the actual type when using it

### 8. Interface (`IList<T>`)
- An interface is a **contract** — defines method names/signatures, no implementation
- A class **implements** an interface (not "inherits")
- `IList<T>` is an interface that says: "I have Add, Remove, [index], Count..."
- Both `List<T>` and `T[]` implement `IList<T>`
- Using `IList<T>` as a type gives **flexibility** — accepts any class that implements the contract

### 9. `IList<IList<string>>` — The Full Picture
- **Outer `IList`**: a collection of groups (can be `List` or array)
- **Inner `IList<string>`**: each group is a collection of strings (can be `List<string>` or `string[]`)
- Why `IList` not `List`? **Flexibility** — you can return either arrays or lists

### 10. Memory Layout: `string[]` vs `List<string>`
| Feature | `string[]` | `List<string>` |
|---|---|---|
| Heap objects (3 strings) | 4 (1 array + 3 strings) | 5 (1 List + 1 array + 3 strings) |
| Layers of indirection | 2 | 3 |
| Fixed size? | Yes | No — auto-grows |
| Add/Remove? | No | Yes |

### 11. Common Errors Found in Code

#### Error: `public` inside a method body
```csharp
void Main() {
    public string[] strs = ["eat"];  // ❌ public has no meaning for local variables
    string[] strs = ["eat"];         // ✅ just remove public
}
```
- `public`/`private` are **access modifiers** — only for class members, not local variables

#### Error: `new IList<...>()` — Cannot instantiate an interface
```csharp
new IList<string>();       // ❌ Interface has no constructor, no code — nothing to build
new List<string>();        // ✅ Class has real code — can create instance
new IList<string>[5];      // ✅ This is creating an ARRAY, not an IList instance
```
- `new` must be followed by a **class** (or struct), never an interface
- Left side of `=` CAN be an interface (it's just a label)
- Right side of `=` MUST be a real class (you're building something)

#### Error: Nested generic type mismatch
```csharp
IList<IList<string>> x = new List<List<string>>();    // ❌ inner types don't match
IList<IList<string>> x = new List<IList<string>>();   // ✅ inner types match exactly
```
- **Why it's dangerous**: If allowed, you could `Add(new string[])` through the interface, but the real `List<List<string>>` can't hold a `string[]`. This would cause a runtime crash.
- **Rule**: Inner type parameters must match exactly when nesting generics.

### 12. All Valid Ways to Create `IList<IList<string>>`
```csharp
IList<IList<string>> a = new List<IList<string>>();   // ✅ List (resizable)
IList<IList<string>> b = new IList<string>[5];         // ✅ Array (fixed size 5)

// ❌ INVALID:
IList<IList<string>> c = new List<List<string>>();     // inner type mismatch
IList<IList<string>> d = new List<string[]>();          // inner type mismatch
IList<IList<string>> e = new IList<IList<string>>();    // can't instantiate interface
```

---

## Method Signature Decoded
```csharp
public IList<IList<string>> GroupAnagrams(string[] strs)
```
| Part | Meaning |
|---|---|
| `public` | Anyone can call this method |
| `IList<IList<string>>` | Returns a list of groups, each group is a list of strings. Can use arrays or Lists. |
| `GroupAnagrams` | Method name |
| `string[] strs` | Takes in a fixed-size array of strings |

---

## Review Questions for April 16

### Memory & Types
1. Where does a `string` variable live? Where does the string data live?
2. What is the size of a reference on a 64-bit system? Why?
3. What is the size of an `int` in C#? Does it change on different CPUs?
4. What happens in memory when you write `string a = "cat"; a = "dog";`? How many string objects exist?

### Immutability
5. What does "immutable" mean?
6. Can you do `name[0] = 'X';` on a string? Why or why not?
7. What happens to the old string when you reassign a string variable?

### Arrays vs Lists
8. How many heap objects are created for `string[] arr = new string[] { "a", "b", "c" };`?
9. How many heap objects are created for `List<string> list = new List<string> { "a", "b", "c" };`?
10. What happens when a `List<T>` runs out of space and you call `Add()`?
11. When a List grows, do the string objects move in memory?

### Interfaces & Generics
12. What is the difference between "implements" and "inherits"?
13. Why would you use `IList<string>` instead of `List<string>` as a variable type?
14. Can you write `List<string> x = new string[] { "a" };`? Why or why not?
15. Can you write `IList<string> x = new string[] { "a" };`? Why or why not?

### The Big Picture
16. Explain `IList<IList<string>>` in your own words.
17. Draw the memory layout (stack/heap) for:
    ```csharp
    List<IList<string>> result = new List<IList<string>>();
    result.Add(new List<string> { "eat", "tea" });
    result.Add(new string[] { "bat" });
    ```

### Error Spotting
18. What's wrong with `new IList<string>()`? Why?
19. What's wrong with `IList<IList<string>> x = new List<List<string>>()`? Explain the danger scenario.
20. Is `new IList<string>[5]` valid? What does it create?
21. Which would you choose for GroupAnagrams — `new List<IList<string>>()` or `new IList<string>[5]`? Why?
