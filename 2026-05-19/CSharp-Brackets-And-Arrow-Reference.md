# C# 符号速查：`<>`、`()`、`{}`、`=>`

这是 C# Core Syntax Foundations 的补充章节。

目标：看到一个符号能立刻知道它在哪种语境下用。

---

## 1. 尖括号 `<>` — 泛型（generics）

`<>` 几乎只用于一件事：**泛型类型参数（type parameter）**。

| 用法 | 例子 | 说明 |
|------|------|------|
| 声明泛型类型变量 | `List<int>` | 一个装 `int` 的列表 |
| 创建泛型对象 | `new Dictionary<string, int>()` | 字符串 -> 整数的字典 |
| 泛型方法调用 | `JsonSerializer.Deserialize<User>(json)` | 调用时指定返回类型 |
| 标准事件 | `EventHandler<OrderCompletedEventArgs>` | 事件参数类型 |
| 异步返回值 | `Task<string>` | 返回 `string` 的异步任务 |

记忆模型：

```text
<>  =  类型尖括号
里面写的是“类型”，不是“值”。
```

陷阱：

```csharp
List<int> a;             // OK
List a;                  // 错，没指明 T 类型（除非用非泛型 List，不推荐）
List<int> b = new List;  // 错，缺 <int>()
```

---

## 2. 圆括号 `()` — 调用、签名、表达式分组

`()` 是 C# 最多义的符号，但语境清晰：

### 2a. 方法调用（method call）

```csharp
Console.WriteLine("hi");
calculator.Add(1, 2);
NumberUtils.Filter(list, 10);
```

### 2b. 方法声明的参数列表（parameter list）

```csharp
public int Add(int a, int b)
```

### 2c. `new` 调用构造函数

```csharp
new List<int>();
new Book("C#", "Skeet", 900);
```

注意：即使没有参数，括号也必须写。

```csharp
new List<int>     // 错
new List<int>()   // 对
```

### 2d. 控制结构条件（control flow conditions）

```csharp
if (x > 0)
while (running)
for (int i = 0; i < n; i++)
foreach (var item in list)
```

### 2e. 表达式分组（expression grouping）

```csharp
int x = (1 + 2) * 3;
double avg = (double)sum / count;     // 类型转换 (cast)
```

### 2f. 元组（tuple）

```csharp
(int min, int max) range = (1, 10);
```

### 2g. Lambda 参数

```csharp
Func<int, int> square = (x) => x * x;
list.Where((value, index) => index > 0);
```

记忆模型：

```text
() = 执行、传值、分组、签名。
看到 () 想：里面是“值或参数列表”。
```

---

## 3. 花括号 `{}` — 代码块与初始化器

### 3a. 代码块（block）

包含一组语句：

```csharp
public void Greet(string name)
{
    Console.WriteLine($"Hello, {name}");
}

if (x > 0)
{
    Console.WriteLine("positive");
}
```

### 3b. 类型定义体（type body）

```csharp
public class Book
{
    private string _title;
}
```

### 3c. 集合初始化器（collection initializer）

```csharp
List<int> nums = new List<int> { 1, 2, 3 };
Dictionary<string, int> d = new() { ["a"] = 1, ["b"] = 2 };
```

### 3d. 对象初始化器（object initializer）

```csharp
var p = new Person { Name = "Alice", Age = 30 };
```

### 3e. 数组初始化（array initializer）

```csharp
int[] arr = { 1, 2, 3 };
```

### 3f. 字符串插值占位符（interpolation hole）

```csharp
Console.WriteLine($"Hello, {name}, age = {age}");
```

注意 `{}` 在插值字符串里只是占位符，不是代码块。

### 3g. switch 表达式

```csharp
string label = score switch
{
    >= 90 => "A",
    >= 80 => "B",
    _     => "C"
};
```

记忆模型：

```text
{} = 一个“容器”：装语句、装成员、装初值、装占位符。
看到 {} 想：里面是“成组的东西”，不是“传参”。
```

陷阱（容易写错的）：

```csharp
Console.WriteLine{"hi"};   // 错：方法调用要用 ()
Console.WriteLine("hi");   // 对
```

---

## 4. 箭头 `=>` — Lambda 和表达式体

`=>` 有两个常见语境，意思完全不同，但形式像。

### 4a. Lambda 表达式（lambda expression）

读法：“给定输入，返回这个值”。

```csharp
Func<int, int> square = x => x * x;
list.Where(v => v >= 10);
list.Select((v, i) => $"{i}: {v}");
```

更完整的形态：

```csharp
(int a, int b) => a + b
(x, y) => { Console.WriteLine(x); return y; }   // 语句体 lambda
```

### 4b. 表达式体成员（expression-bodied member）

把一个简单方法写成一行：

```csharp
public int Add(int a, int b) => a + b;

public string FullName => $"{First} {Last}";   // 表达式体属性
```

等价于：

```csharp
public int Add(int a, int b)
{
    return a + b;
}
```

记忆模型：

```text
=> 左边 = 输入（或方法签名）
=> 右边 = 输出表达式或方法体
```

陷阱：

- `=>` 不是赋值，不是 `>=`，不是 `<=`。
- 不要把 `=` 和 `=>` 混用。

---

## 5. 一张对照表

| 符号 | 语境 | 一句话记忆 |
|------|------|-----------|
| `<>` | 泛型 | 里面写“类型” |
| `()` | 调用、签名、分组、cast、tuple | 里面写“值或参数列表” |
| `{}` | 代码块、初始化器、占位符 | 里面写“成组的东西” |
| `=>` | lambda、表达式体 | 左输入，右输出 |

---

## 6. 复合例子拆解

```csharp
List<int> nums = new List<int> { 1, 2, 3 };
List<int> big  = nums.Where(v => v >= 2).ToList();
```

逐符号拆：

```text
List<int>        -> <> 泛型，T = int
new List<int>()  -> () 构造函数调用（这里被省略，因为 {} 紧跟）
{ 1, 2, 3 }      -> {} 集合初始化器
.Where(v => v >= 2)
   .Where(...)   -> () 方法调用
   v => v >= 2   -> => lambda：输入 v，返回 v >= 2 的布尔值
.ToList()        -> () 方法调用，无参
```

如果能在脑子里这样一段段拆，就不容易写错。
