<!-- markdownlint-disable MD029 -->

# LINQ, Lambda, and Delegate Review Summary — May 13, 2026

## Session Focus

Today we paused the delegate lesson and reviewed the foundation needed for it:

- Lambda expressions
- LINQ core methods
- Deferred execution
- `Func`, `Action`, and `Predicate`
- How LINQ connects to delegates
- The beginning of delegate fundamentals

Tomorrow's recommended starting point: continue with **delegates (delegate)** from passing behavior into methods.

---

## Teaching Preference Added

A local teaching preference file was created:

- `Agent-Teaching-Preferences.md`

Important technical nouns should be explained using Chinese plus English:

```text
中文术语（English term）
```

Examples:

- 泛型委托类型（generic delegate type）
- 委托变量（delegate variable）
- 参数类型（parameter type）
- 返回值类型（return type）
- 方法签名（method signature）

---

## 1. Lambda Expressions

A lambda 表达式（lambda expression） is a short way to write an unnamed function.

Example:

```csharp
number => number * 3
```

Meaning:

```text
输入 number -> 返回 number * 3
```

Equivalent regular method:

```csharp
int Triple(int number)
{
    return number * 3;
}
```

Another example:

```csharp
text => text.Length
```

Equivalent regular method:

```csharp
int GetLength(string text)
{
    return text.Length;
}
```

Key idea:

```text
lambda 表达式（lambda expression）本质上是一段短方法逻辑。
```

---

## 2. Lambda vs Expression-Bodied Method vs LINQ

These are different concepts even though they can all use `=>`.

### Lambda Expression

```csharp
number => number * 10
```

This is a lambda 表达式（lambda expression）.
It has no method name.

### Expression-Bodied Method

```csharp
int TimesTen(int number) => number * 10;
```

This is an 表达式主体方法（expression-bodied method）.
It is still a regular method because it has a method name: `TimesTen`.

Equivalent form:

```csharp
int TimesTen(int number)
{
    return number * 10;
}
```

### LINQ Method

```csharp
numbers.Select(number => number * 10)
```

Here:

```text
Select = LINQ 方法（LINQ method）
number => number * 10 = lambda 表达式（lambda expression）
```

### LINQ With Method Group

```csharp
numbers.Select(TimesTen)
```

Here:

```text
Select = LINQ 方法（LINQ method）
TimesTen = 方法组（method group）
```

Important distinction:

```csharp
numbers.Select(TimesTen);    // correct: passes the method itself
numbers.Select(TimesTen(5)); // wrong: calls the method first and passes an int result
```

---

## 3. LINQ Mental Model

LINQ can be understood as:

```text
集合.操作方法(规则)
```

Examples:

```csharp
numbers.Where(number => number > 3)
```

Meaning:

```text
集合 numbers
操作方法 Where：过滤
规则 number => number > 3：保留大于 3 的元素
```

```csharp
numbers.Select(number => number * 10)
```

Meaning:

```text
集合 numbers
操作方法 Select：转换
规则 number => number * 10：每个数乘 10
```

---

## 4. Where vs Select

### Where

`Where` is filtering（filter）.

It needs a lambda with this shape:

```text
输入 T -> 返回 bool
```

Example:

```csharp
List<int> numbers = new() { 1, 2, 3, 4 };

var result = numbers.Where(number => number > 2);

Console.WriteLine(string.Join(", ", result));
```

Output:

```text
3, 4
```

`Where` uses the returned bool to decide:

```text
true = keep original element
false = remove original element
```

### Select

`Select` is transformation（transform）.

It needs a lambda with this shape:

```text
输入 T -> 返回 any new type
```

Example:

```csharp
List<int> numbers = new() { 1, 2, 3, 4 };

var result = numbers.Select(number => number * 10);

Console.WriteLine(string.Join(", ", result));
```

Output:

```text
10, 20, 30, 40
```

Key contrast:

```csharp
var a = numbers.Where(number => number > 2);
var b = numbers.Select(number => number > 2);
```

If `numbers` is `{ 1, 2, 3, 4 }`:

```text
a = 3, 4
b = False, False, True, True
```

Reason:

```text
Where uses bool to decide whether to keep original elements.
Select converts every element into the returned value.
```

---

## 5. Common Where Mistake

This fails:

```csharp
var a = names.Where(name => name.Length);
```

Reason:

```text
Where requires string -> bool.
name.Length returns int, not bool.
```

Correct:

```csharp
var a = names.Where(name => name.Length > 3);
```

This works with `Select`:

```csharp
var b = names.Select(name => name.Length);
```

Because `Select` can transform:

```text
string -> int
```

---

## 6. Chaining

In a LINQ chain, the next method works on the output of the previous method.

Example:

```csharp
List<string> names = new() { "Amy", "Alice", "Bob", "Charlie" };

var result = names
    .Where(name => name.Length > 3)
    .Select(name => name.Length);

Console.WriteLine(string.Join(", ", result));
```

Output:

```text
5, 7
```

Steps:

```text
Where: Amy, Alice, Bob, Charlie -> Alice, Charlie
Select: Alice, Charlie -> 5, 7
```

Another example:

```csharp
List<int> numbers = new() { 1, 2, 3, 4, 5 };

var result = numbers
    .Select(number => number * 10)
    .Where(number => number > 25);
```

Output:

```text
30, 40, 50
```

Because `Where` sees the transformed values:

```text
10, 20, 30, 40, 50
```

---

## 7. Empty Sequence vs Null

If `Where` finds no matching items, it returns an 空序列（empty sequence）, not `null`.

Example:

```csharp
List<int> numbers = new() { 1, 2, 3 };

var result = numbers.Where(number => number > 10);

Console.WriteLine(result == null);
Console.WriteLine(result.Count());
```

Output:

```text
False
0
```

Key idea:

```text
Where 没有匹配项时，返回 empty IEnumerable<T>，不是 null。
```

---

## 8. Deferred Execution / Lazy Evaluation

Most LINQ methods like `Where`, `Select`, `OrderBy`, and `GroupBy` are delayed by default.

They create a 查询规则（query rule）, not finished data.

Example:

```csharp
List<int> numbers = new() { 1, 2, 3 };

var result = numbers.Where(number => number > 2);

numbers.Add(4);

Console.WriteLine(string.Join(", ", result));
```

Output:

```text
3, 4
```

Reason:

```text
Where did not run when result was created.
It ran when result was enumerated by string.Join.
```

---

## 9. ToList Materialization

`ToList()` causes immediate execution and stores the result in a real list.

Example:

```csharp
List<int> numbers = new() { 1, 2, 3 };

var result = numbers
    .Where(number => number > 2)
    .ToList();

numbers.Add(4);

Console.WriteLine(string.Join(", ", result));
```

Output:

```text
3
```

Reason:

```text
ToList fixed the result before 4 was added.
```

Memory rule:

```text
IEnumerable<T> = query rule, runs when enumerated
List<T> = materialized data, already calculated
```

---

## 10. Multiple Enumeration

Without `ToList()`, enumerating an LINQ query multiple times runs it multiple times.

Example:

```csharp
List<int> numbers = new() { 1, 2, 3 };

var result = numbers.Select(number =>
{
    Console.WriteLine($"Processing {number}");
    return number * 10;
});

foreach (int item in result)
{
    Console.WriteLine(item);
}

foreach (int item in result)
{
    Console.WriteLine(item);
}
```

`Processing 1` prints twice.

With `ToList()`:

```csharp
var result = numbers.Select(number =>
{
    Console.WriteLine($"Processing {number}");
    return number * 10;
}).ToList();
```

`Processing 1` prints once, but `10` can print twice if the list is looped twice.

---

## 11. LINQ Does Not Mutate Source

Example:

```csharp
List<int> numbers = new() { 1, 2, 3, 4 };

var result = numbers.Select(number => number * 10);

Console.WriteLine(string.Join(", ", result));
Console.WriteLine(string.Join(", ", result));
```

Output:

```text
10, 20, 30, 40
10, 20, 30, 40
```

It does not become:

```text
10, 20, 30, 40
100, 200, 300, 400
```

Reason:

```text
Select does not modify source.
Each enumeration recalculates from the original source.
```

If the source changes between enumerations, the query reflects the changed source.

---

## 12. OrderBy and ThenBy

### OrderBy

```csharp
List<string> names = new() { "Charlie", "Amy", "Bob", "Alice" };

var result = names.OrderBy(name => name.Length);
```

Output:

```text
Amy, Bob, Alice, Charlie
```

Reason:

```text
Sort by string length from shortest to longest.
```

`OrderBy` is stable. If two sort keys are equal, original relative order is preserved.

### ThenBy / ThenByDescending

```csharp
List<string> names = new() { "Ben", "Amy", "Bob", "Alice", "Anna" };

var result = names
    .OrderBy(name => name.Length)
    .ThenByDescending(name => name);
```

Output:

```text
Bob, Ben, Amy, Anna, Alice
```

Reason:

```text
First sort by length ascending.
For equal lengths, sort by name descending.
```

---

## 13. FirstOrDefault vs First

### FirstOrDefault

```csharp
List<int> numbers = new() { 2, 4, 6, 8 };

int result = numbers.FirstOrDefault(number => number > 10);
```

Output:

```text
0
```

Reason:

```text
No number is > 10.
default(int) = 0.
```

For string:

```csharp
List<string> names = new() { "Amy", "Bob" };

string? result = names.FirstOrDefault(name => name.Length > 5);

Console.WriteLine(result == null);
```

Output:

```text
True
```

Reason:

```text
default(string) = null.
```

### string vs string?

```csharp
string name;
```

Means:

```text
Expected to be non-null.
```

```csharp
string? name;
```

Means:

```text
May be string or null.
```

This is a 可空引用类型（nullable reference type） annotation for compiler safety.

### First

```csharp
List<int> numbers = new() { 2, 4, 6 };

int result = numbers.First(number => number > 10);
```

This throws an 异常（exception）, specifically `InvalidOperationException`.

Reason:

```text
First requires a match.
If no match exists, it throws instead of returning default.
```

---

## 14. GroupBy

`GroupBy` groups elements by a key.

Example:

```csharp
List<string> words = new() { "cat", "car", "dog", "door", "apple" };

var groups = words.GroupBy(word => word[0]);

foreach (var group in groups)
{
    Console.WriteLine($"{group.Key}: {string.Join(", ", group)}");
}
```

Output:

```text
c: cat, car
d: dog, door
a: apple
```

Important:

```text
GroupBy preserves first appearance order of keys.
It does not automatically sort keys.
```

To sort by key:

```csharp
var groups = words
    .GroupBy(word => word[0])
    .OrderBy(group => group.Key);
```

Each group is an `IGrouping<TKey, TElement>`.

For this example:

```csharp
IGrouping<char, string>
```

Key ideas:

```text
group.Key = the group key
group itself = the items in that group
```

---

## 15. GroupBy With Select and Anonymous Object

Example:

```csharp
List<string> names = new() { "Alice", "Anna", "Bob", "Ben", "Charlie" };

var result = names
    .GroupBy(name => name[0])
    .Select(group => new
    {
        FirstLetter = group.Key,
        Total = group.Count()
    });

foreach (var item in result)
{
    Console.WriteLine($"{item.FirstLetter}: {item.Total}");
}
```

Output:

```text
A: 2
B: 2
C: 1
```

`new { ... }` creates an 匿名对象（anonymous object）.

Example:

```csharp
new
{
    FirstLetter = group.Key,
    Total = group.Count()
}
```

Compiler creates a temporary type with read-only properties:

```text
FirstLetter
Total
```

Use anonymous objects for:

```text
temporary projections
group summaries
method-local data shapes
```

Use a class or record when data needs to cross method boundaries or API boundaries.

---

## 16. Func, Action, Predicate

### Func

`Func` is used when a method returns a value.

Rule:

```text
Func<input types..., return type>
```

Examples:

```csharp
Func<int, int> operation = number => number * 10;
```

Shape:

```text
int -> int
```

```csharp
Func<string, bool> rule = name => name.StartsWith("A");
```

Shape:

```text
string -> bool
```

Important rule:

```text
Func 最后一个类型是返回值类型（return type）。前面的类型都是输入参数类型（input parameter types）。
```

### Action

`Action` is used for methods that return `void`.

Example:

```csharp
void LogOrder(int orderId, string status)
{
    Console.WriteLine($"{orderId}: {status}");
}

Action<int, string> logger = LogOrder;
```

Shape:

```text
int, string -> void
```

Important:

```text
Func cannot use void as the final return type.
Use Action for void-returning methods.
```

### Predicate

`Predicate<T>` means:

```text
T -> bool
```

Example:

```csharp
bool IsAdult(int age)
{
    return age >= 18;
}

Predicate<int> rule1 = IsAdult;
Func<int, bool> rule2 = IsAdult;
```

Both work because the shape is:

```text
int -> bool
```

`Predicate<T>` emphasizes that the method is a 判断规则（predicate / rule）.

---

## 17. Delegate Basics Covered

A 委托（delegate） is a type-safe object that can hold a reference to a method.

Example:

```csharp
int AddOne(int number)
{
    return number + 1;
}

Func<int, int> operation = AddOne;

Console.WriteLine(operation(5));
```

Output:

```text
6
```

Meaning:

```text
operation is a delegate variable.
It can hold any method with shape int -> int.
AddOne matches that shape.
```

A method can be passed into another method:

```csharp
int ApplyOperation(int value, Func<int, int> transform)
{
    return transform(value);
}

int Double(int number)
{
    return number * 2;
}

Console.WriteLine(ApplyOperation(6, Double));
```

Output:

```text
12
```

Meaning:

```text
transform is a delegate formal parameter.
In this call, transform represents Double.
return transform(value) means return Double(6).
```

---

## 18. Custom Delegate Type

A custom delegate type can be defined like this:

```csharp
delegate bool ValidationRule(string input);
```

Meaning:

```text
ValidationRule can hold any method with shape string -> bool.
```

Example:

```csharp
bool IsNotEmpty(string text)
{
    return text.Length > 0;
}

ValidationRule rule = IsNotEmpty;
```

This works because parameter types and return type match.

Important:

```text
Delegate matching checks parameter types, parameter order, and return type.
It does not care about parameter names.
```

---

## 19. Delegate Reassignment

A delegate variable can be reassigned to another compatible method.

Example:

```csharp
delegate bool NumberRule(int number);

bool IsEven(int number)
{
    return number % 2 == 0;
}

bool IsPositive(int number)
{
    return number > 0;
}

NumberRule rule = IsEven;
Console.WriteLine(rule(6));

rule = IsPositive;
Console.WriteLine(rule(-3));
```

Output:

```text
True
False
```

Reason:

```text
First call: rule points to IsEven.
Second call: rule points to IsPositive.
```

---

## 20. Multicast Delegates

A 多播委托（multicast delegate） can hold multiple methods in its 调用列表（invocation list）.

Example:

```csharp
void A()
{
    Console.WriteLine("A");
}

void B()
{
    Console.WriteLine("B");
}

Action action = A;
action += B;
action += A;

action();
```

Output:

```text
A
B
A
```

`+=` adds to the end of the invocation list.

`-=` removes the last matching delegate entry by searching backward.

Example:

```csharp
Action action = A;
action += B;
action += A;
action -= A;
action += B;

action();
```

Output:

```text
A
B
B
```

Reason:

```text
A -> B -> A
-= A removes the last A
A -> B
+= B
A -> B -> B
```

### Multicast Delegate With Return Value

If a multicast delegate has a return value, all methods run, but only the last return value is kept.

Example:

```csharp
int First()
{
    Console.WriteLine("First");
    return 1;
}

int Second()
{
    Console.WriteLine("Second");
    return 2;
}

int Third()
{
    Console.WriteLine("Third");
    return 3;
}

Func<int> calculate = First;
calculate += Second;
calculate += Third;

int result = calculate();

Console.WriteLine($"Result: {result}");
```

Output:

```text
First
Second
Third
Result: 3
```

This is why multicast delegates are usually best with `Action` and event notification scenarios.

---

## 21. LINQ and Delegate Connection

LINQ methods receive delegates.

Example:

```csharp
var result = names.Where(name => name.StartsWith("A"));
```

Behind the scenes, `Where` needs something like:

```csharp
Func<string, bool>
```

Example:

```csharp
var result = names.Select(name => name.Length);
```

Behind the scenes, `Select` needs something like:

```csharp
Func<string, int>
```

Key idea:

```text
LINQ 方法接收委托（delegate）作为参数。
lambda 表达式（lambda expression）是创建委托的一种简洁写法。
```

---

## Review Questions For Tomorrow

### Lambda and LINQ Basics

1. What is a lambda expression（lambda expression）?
2. Translate `number => number * 5` into a regular method（regular method）.
3. What is the difference between `number => number * 10` and `int TimesTen(int number) => number * 10;`?
4. In `numbers.Select(number => number * 10)`, which part is LINQ and which part is lambda?
5. What is a method group（method group） in `numbers.Select(TimesTen)`?
6. Why does `numbers.Select(TimesTen(5))` fail?

### Where and Select

7. What does `Where` do?
8. What shape must the lambda passed to `Where` have?
9. What does `Select` do?
10. What shape can the lambda passed to `Select` have?
11. Given `{ 1, 2, 3, 4 }`, what does `.Where(n => n > 2)` return?
12. Given `{ 1, 2, 3, 4 }`, what does `.Select(n => n > 2)` return?
13. Why does `names.Where(name => name.Length)` fail?
14. Why does `names.Select(name => name.Length)` work?

### Chaining

15. Explain this chain step by step:

```csharp
names
    .Where(name => name.Length > 3)
    .Select(name => name.Length);
```

16. In a LINQ chain, does the next method process the original source or the previous method's output?
17. What is the output?

```csharp
List<int> numbers = new() { 1, 2, 3, 4, 5 };

var result = numbers
    .Select(n => n * 10)
    .Where(n => n > 25);
```

### Empty Sequences and Deferred Execution

18. If `Where` finds no elements, does it return `null` or an empty sequence（empty sequence）?
19. What does this print?

```csharp
List<int> numbers = new() { 1, 2, 3 };
var result = numbers.Where(n => n > 10);
Console.WriteLine(result == null);
Console.WriteLine(result.Count());
```

20. What is deferred execution（deferred execution）?
21. What does `ToList()` do in a LINQ query?
22. Why does this print `3, 4`?

```csharp
List<int> numbers = new() { 1, 2, 3 };
var result = numbers.Where(n => n > 2);
numbers.Add(4);
Console.WriteLine(string.Join(", ", result));
```

23. Why does this print only `3`?

```csharp
List<int> numbers = new() { 1, 2, 3 };
var result = numbers.Where(n => n > 2).ToList();
numbers.Add(4);
Console.WriteLine(string.Join(", ", result));
```

24. What is multiple enumeration（multiple enumeration）?
25. Why can repeated enumeration rerun a lambda?

### Sorting

26. What does `OrderBy(name => name.Length)` do?
27. What does `ThenByDescending(name => name)` do after `OrderBy(name => name.Length)`?
28. Is `OrderBy` stable? What does stable sort mean?

### First and FirstOrDefault

29. What does `FirstOrDefault` return when no `int` matches?
30. What does `FirstOrDefault` return when no `string` matches?
31. What is the difference between `string` and `string?`?
32. What happens when `First` finds no match?
33. When would you choose `First` instead of `FirstOrDefault`?

### GroupBy and Anonymous Objects

34. What does `GroupBy(word => word[0])` do?
35. Does `GroupBy` automatically sort keys?
36. What is `group.Key`?
37. What does `group.Count()` count?
38. What is an anonymous object（anonymous object）?
39. When is an anonymous object appropriate?
40. When should you use a real class or record instead?

### Delegates

41. What is a delegate（delegate）?
42. What does `Func<int, int>` mean?
43. In `Func<string, bool>`, which type is the input and which type is the return?
44. What does `Action<int, string>` mean?
45. Why is `Func<int, string, void>` wrong?
46. What does `Predicate<int>` mean?
47. What is a custom delegate type（custom delegate type）?
48. What does this mean?

```csharp
delegate bool ValidationRule(string input);
```

49. Does delegate matching care about parameter names?
50. What happens when a delegate variable is reassigned?
51. What is a multicast delegate（multicast delegate）?
52. What does `+=` do to a delegate invocation list?
53. What does `-=` remove from a delegate invocation list?
54. If a multicast delegate has return values, which return value is kept?
55. Why are multicast delegates commonly used with events（events）?

### Tomorrow's Starting Question

56. Explain this code:

```csharp
bool ValidateName(string name, Func<string, bool> rule)
{
    return rule(name);
}

bool IsLongName(string name)
{
    return name.Length > 3;
}

Console.WriteLine(ValidateName("Amy", IsLongName));
Console.WriteLine(ValidateName("Alice", IsLongName));
```

What are the outputs, and what does `rule` represent in each call?
