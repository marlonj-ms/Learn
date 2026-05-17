# Delegates Deep Dive Summary — May 15, 2026

## Today’s Focus

今天主要学习了 委托（delegate），并把之前的 Lambda、LINQ、扩展方法、闭包串起来理解。

核心目标：从“会用 `Func<>` / `Action<>`”升级到理解它们背后的 delegate 机制。

---

## 1. 类型（Type）的理解

类型（type）可以理解为一套规则，告诉编译器：

1. 这个变量能存什么。
2. 这个变量能做什么操作。

例子：

```csharp
int x = 5;
string name = "Tom";
List<int> numbers = new();
```

- `int`：能存整数，能做加减乘除。
- `string`：能存文本，能调用 `.Length`, `.ToUpper()` 等。
- `List<int>`：能存一组 `int`，能 `.Add()`, `.Remove()`。
- `delegate`：能存方法，并且能调用这个方法。

重要理解：

> 委托（delegate）不是方法本身，而是一个能“指向方法并调用方法”的对象。

---

## 2. delegate 的基本定义

```csharp
public delegate int MathOperation(int a, int b);
```

这行声明了一个 委托类型（delegate type）。

意思是：

> `MathOperation` 这种类型可以存“接收两个 `int`，返回一个 `int`”的方法。

使用：

```csharp
int Add(int a, int b) => a + b;

MathOperation op = Add;
int result = op(5, 6); // 11
```

拆解：

- `MathOperation`：委托类型（delegate type）。
- `op`：委托变量（delegate variable）。
- `Add`：被委托对象记录的方法。
- `op(5, 6)`：调用委托，实际执行 `Add(5, 6)`。

---

## 3. delegate 对象内部：target + method

委托对象内部大致保存两样东西：

```text
target: 要在哪个对象上调用方法
method: 要调用哪个方法
```

### static 方法

```csharp
static int Add(int a, int b) => a + b;
Func<int, int, int> op = Add;
```

```text
target = null
method = Add
```

因为 static 方法不属于某个具体对象。

### 实例方法（instance method）

```csharp
var calc = new Calculator();
Func<int, int, int> op = calc.AddWithBias;
```

```text
target = calc
method = AddWithBias
```

因为实例方法需要知道 `this` 是谁。

### 扩展方法（extension method）

扩展方法必须是 static method，所以如果把扩展方法赋值给 delegate：

```csharp
Func<int, bool> check = MyExtensions.IsEven;
```

```text
target = null
```

---

## 4. 闭包对象（Closure Object）

例子：

```csharp
int bonus = 10;
Func<int, int> addBonus = n => n + bonus;
bonus = 20;

int result = addBonus(5); // 25
```

为什么不是 `15`？

因为 lambda 捕获的是变量本身，不是当时的值。

编译器大致会生成一个 闭包对象（closure object）：

```csharp
class Closure
{
    public int bonus;
}

var closure = new Closure();
closure.bonus = 10;

Func<int, int> addBonus = n => n + closure.bonus;

closure.bonus = 20;
```

所以：

```text
addBonus(5)
→ 5 + closure.bonus
→ 5 + 20
→ 25
```

关键句：

> lambda 捕获的是变量本身，不是变量当时的值。

---

## 5. Func<> / Action<> / Predicate<> 都是 delegate

`Func<>` 本身就是 .NET 内置的委托类型。

```csharp
Func<string, int> getLength = text => text.Length;
```

意思：

- 参数类型（parameter type）：`string`
- 返回值类型（return type）：`int`
- `getLength("hello")` 返回 `5`

常见内置委托：

| Delegate | Meaning |
|---|---|
| `Func<T, TResult>` | 接收 `T`，返回 `TResult` |
| `Action<T>` | 接收 `T`，返回 `void` |
| `Predicate<T>` | 接收 `T`，返回 `bool` |

---

## 6. 自定义委托 vs Func<>

```csharp
public delegate int MathOperation(int a, int b);

MathOperation customOp = (a, b) => a + b;
Func<int, int, int> funcOp = (a, b) => a + b;
```

虽然签名一样：

```text
(int, int) -> int
```

但它们是不同类型。

```csharp
customOp = funcOp; // 编译错误
funcOp = customOp; // 编译错误
```

原因：C# 使用 名义类型（nominal typing）。

> 类型名字不同，就是不同类型，即使方法签名一样。

但是可以用 lambda 包装：

```csharp
Combiner comb = (a, b) => calc(a, b);
```

这不是把 `calc` 转成 `Combiner`，而是创建一个新的 `Combiner`，内部再调用 `calc`。

调用顺序：

```text
comb(2, 3)
→ 进入新的 lambda
→ lambda 内部调用 calc(2, 3)
→ calc 返回结果
→ lambda 返回结果
```

---

## 7. 多播委托（Multicast Delegate）

一个委托变量可以挂多个方法。

```csharp
void A(string msg) => Console.WriteLine("A: " + msg);
void B(string msg) => Console.WriteLine("B: " + msg);
void C(string msg) => Console.WriteLine("C: " + msg);

Action<string> notify = A;
notify += B;
notify += C;
notify -= A;

notify("Done");
```

调用列表（invocation list）：

```text
B, C
```

输出：

```text
B: Done
C: Done
```

规则：

- `+=`：把方法添加到调用列表末尾。
- `-=`：从调用列表中移除一个匹配方法；如果同一个方法出现多次，通常移除最后一个匹配项。

---

## 8. 多播委托返回值陷阱

```csharp
delegate int NumberRule(int x);

int Double(int x) => x * 2;
int AddTen(int x) => x + 10;

NumberRule rule = Double;
rule += AddTen;

int result = rule(5);
```

两个方法都会执行：

```text
Double(5) -> 10
AddTen(5) -> 15
```

但最终：

```csharp
result == 15
```

因为多播委托如果有返回值，只保留最后一个方法的返回值。

重要规则：

> 多播委托最适合 `void` 返回值，比如 `Action<T>`。

---

## 9. 回调（Callback）

今天形成的关键学习模型：

> callback 不是结果自动返回外层，而是方法内部主动调用外层传进来的函数/动作。

普通参数：

```csharp
PrintName("Tom");
```

意思是：传一个值进去。

callback 参数：

```csharp
RunTask(() => Console.WriteLine("Callback"));
```

意思是：传一段将来要执行的动作进去。

例子：

```csharp
void RunTask(Action onFinished)
{
    Console.WriteLine("Start");
    onFinished();
    Console.WriteLine("End");
}

RunTask(() => Console.WriteLine("Callback"));
```

输出：

```text
Start
Callback
End
```

执行顺序：

```text
进入 RunTask
→ 打印 Start
→ 执行 onFinished()
→ onFinished 指向 lambda，打印 Callback
→ 回到 RunTask
→ 打印 End
```

---

## 10. null 安全调用：?.Invoke()

委托是引用类型（reference type），所以可以是 `null`。

```csharp
Action<int>? onNumber = null;

onNumber?.Invoke(10);

onNumber = n => Console.WriteLine(n * 2);

onNumber?.Invoke(10); // 20
```

输出：

```text
20
```

`?.Invoke()` 的意思：

> 如果委托不是 null，就调用；如果是 null，就什么也不做。

它避免了 `NullReferenceException`。

---

## 11. event 的预告

今天最后过渡到了 event。

```csharp
public Action<string>? OrderCompleted;
```

这是 public delegate 字段，外部权限太大，可以：

```csharp
service.OrderCompleted = null;
service.OrderCompleted("fake");
service.OrderCompleted = SomeHandler;
```

这些都危险。

如果改成 private：

```csharp
private Action<string>? OrderCompleted;
```

外部安全了，但外部也不能订阅。

所以需要：

```csharp
public event Action<string>? OrderCompleted;
```

核心理解：

> private delegate 太封闭，public delegate 太危险，event 正好提供“外部能订阅，但不能乱触发/清空”的权限边界。

---

## Memory Diagrams

### Delegate object

```text
委托变量 op
┌──────────┐
│ op ──────┼────► 委托对象
└──────────┘       ┌────────────────┐
                   │ target: null   │
                   │ method: Add    │
                   └────────────────┘
```

### Instance method delegate

```text
委托对象                          Calculator 对象
┌──────────────────────┐         ┌──────────────┐
│ target: ─────────────┼────►    │ Bias = 100   │
│ method: AddWithBias  │         └──────────────┘
└──────────────────────┘
```

### Closure object

```text
委托对象                           闭包对象
┌────────────────────┐            ┌──────────────┐
│ target: ───────────┼───────►    │ bonus = 20   │
│ method: lambda     │            └──────────────┘
└────────────────────┘
```

---

## Review Questions

### Delegate Basics

1. `public delegate int MathOperation(int a, int b);` 这行代码声明了什么？
2. `MathOperation op = Add;` 中，`op` 是什么？里面记录了什么？
3. `op(5, 6)` 实际执行了什么？
4. delegate 是方法本身吗？如果不是，它是什么？

### target / method

5. static 方法赋值给 delegate 时，`target` 是什么？为什么？
6. 实例方法赋值给 delegate 时，`target` 是什么？为什么？
7. 扩展方法赋值给 delegate 时，`target` 是什么？为什么？

### Closure

8. 下面代码结果是多少？为什么？

```csharp
int bonus = 10;
Func<int, int> addBonus = n => n + bonus;
bonus = 20;
int result = addBonus(5);
```

9. lambda 捕获的是变量的值，还是变量本身？
10. 闭包对象（closure object）是用来做什么的？

### Func / Action

11. `Func<string, int>` 表示什么？
12. `Action<int>` 表示什么？
13. `Predicate<string>` 表示什么？

### Delegate Type Identity

14. 两个自定义 delegate 签名相同，能直接互相赋值吗？为什么？
15. 为什么 `Combiner comb = (a, b) => calc(a, b);` 可以工作？

### Multicast Delegate

16. `+=` 对委托做了什么？
17. `-=` 对委托做了什么？
18. 多播委托有返回值时，最终返回哪个方法的返回值？

### Callback

19. callback 是什么？
20. 为什么说 callback 不是“结果自动流回外层”？
21. `onFinished()` 在方法内部执行时，实际会发生什么？

### Event Preview

22. `public Action<string>? OrderCompleted;` 为什么危险？
23. `private Action<string>? OrderCompleted;` 为什么又太封闭？
24. `event` 解决了什么权限问题？
