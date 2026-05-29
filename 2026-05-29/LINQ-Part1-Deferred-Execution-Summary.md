# LINQ 链式流利度 — Part 1: 延迟执行 (Deferred Execution)

> **Day 51 · Part 1** — 2026-05-29
> 主题：`Where` 的真实签名 + `IEnumerable<T>` 不是 `List<T>` + 延迟执行机制
> 状态：3/3 测验全对 ✅

---

## 🎯 Part 1 一句话总结

> **LINQ 链 = 配方书（recipe book），物化方法 = 下锅开火。在下锅之前，你只是在写菜谱，没动一根菜。**

---

## 🧠 3 条新口诀（续 Day 50 的 #43）

| # | 口诀 |
|---|---|
| **44** | LINQ 操作的返回类型是 `IEnumerable<T>`，不是你输入的具体类型。能 `foreach` ≠ 是 `List`。 |
| **45** | `IEnumerable<T>` 每被 `foreach` / `Count()` / `Any()` 一次，整本配方就重做一次。要复用结果，先 `.ToList()` 把它"煮熟"。 |
| **46** | 延迟执行 = 配方在 `ToList()` / `foreach` 这一刻才看数据源。期间数据源被改了，配方会看到新值。 |

至此累计口诀 **46** 条。

---

## 📐 `Where` 真实签名解码（用 #43 心法）

```csharp
public static IEnumerable<TSource> Where<TSource>(
    this IEnumerable<TSource> source,
    Func<TSource, bool> predicate)
```

| 位置 | 内容 | 角色 |
|---|---|---|
| 签名最前面 `IEnumerable<TSource>` | **返回值类型（return type）** | ← 目光直接弹到这里 |
| `this IEnumerable<TSource> source` | 第 1 个参数（被扩展的对象） | 输入数据源 |
| `Func<TSource, bool> predicate` | 第 2 个参数 | 谓词委托（过滤函数） |

**关键点**：输入和输出**都是** `IEnumerable<TSource>` —— 元素类型不变，只是少了一些。
（这跟 `Select` 不同 — `Select` 可以换类型，下个 Part 学。）

---

## 🪤 3 个易错直觉（自己刚才踩中并打掉的）

### 错位 1：「输入是什么类型，输出就是什么类型」

```csharp
List<int> nums = [1, 2, 3, 4, 5];
var result = nums.Where(n => n > 2);
// result 是 IEnumerable<int>，不是 List<int>
```

`List<int>` 能传进 `Where` 是因为 `List<int>` **实现了** `IEnumerable<int>` 接口，
不是因为它"就是" `IEnumerable<int>`。

### 错位 2：「能 foreach = 是 List」

`foreach` 只要求实现 `IEnumerable<T>`，它根本不在乎你是不是 `List`。

| 能被 foreach 的东西 | 都实现了 `IEnumerable<T>` |
|---|---|
| `List<T>` | ✅ |
| `int[]` | ✅ |
| `HashSet<T>` | ✅ |
| `Dictionary<K,V>` | ✅ |
| LINQ 链式结果（迭代器对象） | ✅ |

### 错位 3：「Where 一调用，过滤就执行了」

错。`Where(...)` 这一行只是**打包了一个迭代器对象**，
谓词委托 `n => n > 2` **一次都没被调用**。

---

## 🔬 延迟执行实战示例

```csharp
var nums = new List<int> { 1, 2, 3, 4, 5 };

var result = nums.Where(n =>
{
    Console.WriteLine($"  ☎️ 谓词被调用，检查 {n}");
    return n > 2;
});

Console.WriteLine("=== Where 已经写完，还没 foreach ===");
// ☎️ 出现次数：0

foreach (var x in result) { Console.WriteLine($"取到 {x}"); }
// ☎️ 出现次数：5（每个元素被检查一次）

foreach (var x in result) { Console.WriteLine($"再取到 {x}"); }
// ☎️ 又出现 5 次！总共 10 次
```

**为什么第二个 foreach 又跑了 5 次？**
因为 `result` 不是缓存好的结果，它是配方。每次 foreach，CLR 会：
```
foreach (var x in result)
   ↓
result.GetEnumerator()        ← 重新拿一个迭代器
   ↓
对每个原始元素调用谓词         ← 整本菜谱重做一遍
```

---

## 💣 生产事故场景：多次枚举 (Multiple Enumeration)

```csharp
// ❌ 反例：数据库被打 3N 次
var expensiveQuery = users
    .Where(u => SlowDatabaseLookup(u.Id))
    .Select(u => u.Name);

if (expensiveQuery.Any())                              // 打 N 次
{
    Console.WriteLine($"找到 {expensiveQuery.Count()} 个");  // 又打 N 次
    foreach (var name in expensiveQuery) ...           // 再打 N 次
}
```

```csharp
// ✅ 正例：加一行 ToList()，谓词只跑 N 次
var names = users
    .Where(u => SlowDatabaseLookup(u.Id))
    .Select(u => u.Name)
    .ToList();                       // ← 配方煮熟

if (names.Count > 0)                 // List.Count 是属性，不重算
{
    Console.WriteLine($"找到 {names.Count} 个");
    foreach (var name in names) ...  // 直接遍历内存
}
```

> JetBrains Rider / ReSharper 的对应警告：
> **"Possible multiple enumeration of IEnumerable"**

---

## 🍳 物化 (Materialization) 三件套

把延迟的"配方"变成实打实的容器：

| 方法 | 返回类型 | 何时用 |
|---|---|---|
| `.ToList()` | `List<T>` | 最常用，可索引、可改 |
| `.ToArray()` | `T[]` | 固定大小、稍微省内存 |
| `.ToDictionary(k => ...)` | `Dictionary<TKey,TValue>` | 按 key 查找 |

---

## ⚠️ 延迟执行的"动态绑定"特性

```csharp
var nums = new List<int> { 1, 2, 3, 4, 5 };

var step1 = nums.Where(n => n > 2);     // 配方 A
var step2 = step1.Select(n => n * 10);  // 配方 B（基于配方 A）

nums.Add(6);                            // ← 数据源被改

var output = step2.ToList();            // ← 此刻才执行
// output = [30, 40, 50, 60]   ← 看到了新添加的 6！
```

**心法**：配方在 `ToList()` 这一刻才"翻菜谱、看冰箱"。冰箱里多了什么菜，配方就用什么菜。

---

## 📂 Files

```
2026-05-29/
  LINQ-Part1-Deferred-Execution-Summary.md   ← this file
```

---

## ⏭️ Part 2 预告

主题：`Select` 投影 — LINQ 链能"变形"的核心
预告签名（即将能盲读）：

```csharp
public static IEnumerable<TResult> Select<TSource, TResult>(
    this IEnumerable<TSource> source,
    Func<TSource, TResult> selector)
```

注意：**两个**泛型参数 `TSource` 和 `TResult` —— 输入输出类型可以完全不同。
这正是 `Where` 没有的能力（`Where` 只有 `TSource` 一个）。
