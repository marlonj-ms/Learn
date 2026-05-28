# Day 50 — 嵌套泛型阅读流利度 (Nested Generics Reading Fluency)

> **Date**: 2026-05-28
> **Topic**: 把 `List<List<int>>` / `Dictionary<string, List<Order>>` / `Func<Dictionary<string, List<int>>, List<string>>` 这种"长签名"训练到一眼就懂、一手就写
> **Result**: 6 级阶梯全打通 ✅ + 12 个新口诀 + 9 个新坑

---

## 🎯 一句话总结

> **"嵌套泛型不是更难，只是更深。心法只有一个：剥洋葱 —— 从外往内、给每层一个业务名字。"**

---

## 📐 6 级阶梯地图

| 级别 | 签名 | 心法关键词 |
|---|---|---|
| **L1** | `List<int>` | 单层封闭；3 种初始化语法等价 |
| **L2** | `List<T>` | 开放泛型类型 ≠ 封闭泛型类型；T 还没替换前不能 new |
| **L3** | `List<List<int>>` / `Dictionary<string,int>` | 双层嵌套 + 两种"中括号"语义（index vs key） |
| **L4** | `List<List<List<int>>>` | 同模式多一层，剥洋葱套路不变 |
| **L5** | `Dictionary<string, List<Order>>` | 混合容器 + 业务签名；链式取值类型追踪 |
| **L6** | `Func<Dictionary<string,List<int>>, List<string>>` | Delegate 包裹嵌套泛型；**目光弹到最后一个泛型参数 = 返回类型** |

---

## 🧠 12 个新锁定口诀（编号续 Day 49 的 #31）

| # | 口诀 | 适用场景 |
|---|---|---|
| **32** | **空 List ≠ null** —— 空 List 是有对象（约 40 字节），只是 `Count == 0` | 永远别 `if (list == null)`，用 `if (list.Count == 0)` |
| **33** | **"传 null 安全，对 null 调方法才炸"** | 一句话解释整个 NRE 家族 |
| **34** | **`?.` = 空条件运算符 (null-conditional)** —— 短路；任一段 null 整条返回 null | 链式访问可能为 null 的字段 |
| **35** | **`int?` 是类型修饰；`?.` 是操作修饰** | 同一个 `?` 在两种位置含义不同，别串号 |
| **36** | **目标类型 `new()`** —— 跟着左侧"期望类型"走 | C# 9+；`var x = new()` 永远非法 |
| **37** | **`List<List<T>>` ≠ `T[,]`** —— 锯齿 (jagged) vs 矩形 (rectangular) | 内层长度能不能不一致 |
| **38** | **0-indexed：长度 N 集合，合法下标 0 到 N-1**，最后一个是 `Count - 1`，不是 `Count` | 防 off-by-one |
| **39** | **`list[^N]` ≡ `list[Count - N]`** —— 帽子运算符等价公式；`^1` = 最后一个 | C# 8+ index-from-end |
| **40** | **Dictionary 取值用 `TryGetValue`**，别用 `[]` 暴力取 | `[]` 缺 key 抛 `KeyNotFoundException` |
| **41** | **`Dictionary<string, ...>` 默认大小写敏感** | 想忽略：`new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` |
| **42** | **decimal 字面量必须带 `m` 后缀**：`1000m` / `1000.0m` | `m=money` / `f=float` / `d=double` / `L=long`；不带后缀的小数默认是 `double` |
| **43** | **读 `Func<...>`：目光直接弹到最后一个泛型参数 = 返回类型**，前面全是输入 | 解嵌套 + delegate 复合签名 |

---

## 🪤 9 个新坑（cheatsheet 继续编号 54+）

| # | 坑 | 一句话修法 |
|---|---|---|
| 54 | 把空 List 当成 null —— 跑 `list == null` 永远 False | 用 `list.Count == 0` 才是判空 |
| 55 | `?.` 短路 —— 链式中任一段 null，后面全跳过返回 null | 跨段调用前要么确认非 null，要么继续用 `?.` |
| 56 | `List<List<T>>` 和 `T[,]` 混用 | 不规则数据走锯齿；固定矩形（图像、棋盘）走 `T[,]` |
| 57 | 嵌套下标越界：写成 `m[Count][Count]` | 改 `m[^1][^1]` 或 `m[Count-1][Count-1]` |
| 58 | `dict["missingKey"]` → `KeyNotFoundException` | `dict.TryGetValue(key, out var v)` |
| 59 | `dict["Alice"]` vs `dict["alice"]` —— case-sensitive 默认 | 初始化传 `StringComparer.OrdinalIgnoreCase` |
| 60 | `decimal d = 1000.0;` —— 编译错（double 字面量）| 加后缀 `m`：`decimal d = 1000.0m;` |
| 61 | 读 `Func<...>` 时眼睛跑去看"中间最显眼的东西"忘了找返回类型 | 第一眼直接定位最后一个 `,` 后面那块 |
| 62 | 嵌套泛型里的逗号有两种身份：嵌套类型自己的 vs 外层 Func 的 | 数 `<` `>` 配对深度 = 0 时遇到的逗号才属于外层 |

---

## 🔑 解码长签名的"剥洋葱法" (Onion Peel)

> 把任何长签名按这 3 步拆，几秒钟就能"说人话"：

### Step 1 — 找最外层
看最左的类型名 + 它紧跟的 `<...>`。这就是"最外面的盒子"。

### Step 2 — 数 `<` `>` 配对深度，切顶层逗号
**只有深度为 0 时遇到的逗号才属于最外层。** 嵌套类型自己的逗号要忽略。

```
Func<Dictionary<string, List<int>>, List<string>>
     ^                            ^               ^
     深度 1                       深度 0          深度 0 时遇到的逗号
                                  ↑ 切这里         才是 Func 的分界
```

### Step 3 — 每层翻译成业务白话
- `Dictionary<string, List<int>>` → "按 string（如学生名）查 List<int>（这个学生的分数列表）"
- `List<string>` → "字符串列表（如学霸名单）"
- 串起来：**"喂进一张分数总表，吐出一张学霸名单"**。

---

## 📂 Files touched today

```
2026-05-28/
  Nested-Generics-Fluency.cs       ← 6 级阶梯可运行 demo
  Nested-Generics-Summary.md       ← this file
  Current-Learning-Status.md       ← Day 50 close-out + Day 51 候选

dailyread/
  dailyread-2026-05-28.html        ← 5 分钟版子页

dailyread.html                      ← 首页 05-28 卡片插入 + 05-27 降级
csharp-syntax-cheatsheet.html       ← 追加坑 54–62
```

---

## 🛟 关键代码片段速查

### A. 3 种初始化 List 语法（等价）

```csharp
var a = new List<int>() { 10, 20, 30 };   // 完整
var b = new List<int>  { 10, 20, 30 };    // 省略空括号（仅无参 ctor）
List<int> c = new()    { 10, 20, 30 };    // C# 9+ 目标类型 new()
```

### B. Dictionary 安全取值

```csharp
// ❌ 缺 key 会抛 KeyNotFoundException
var x = dict["maybe-missing"];

// ✅ 推荐
if (dict.TryGetValue("maybe-missing", out var v))
{
    Console.WriteLine(v);
}

// ✅ 想忽略大小写
var safe = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
```

### C. 帽子运算符 `^`（C# 8+）

```csharp
var list = new List<int> { 10, 20, 30, 40 };
list[^1]   // = 40 = list[3]
list[^2]   // = 30 = list[2]
list[^0]   // ❌ OutOfRange（= list[Count]）
```

### D. 链式取值 + 类型追踪

```csharp
Dictionary<string, List<Order>> ordersByCustomer = ...;

// 一步一步追类型：
ordersByCustomer            // Dictionary<string, List<Order>>
ordersByCustomer["alice"]   // List<Order>
ordersByCustomer["alice"][0] // Order
ordersByCustomer["alice"][0].Total // decimal

// 左边变量类型 = 链式最后一步的类型
decimal firstTotal = ordersByCustomer["alice"][0].Total;   // ✅
// Order firstTotal = ordersByCustomer["alice"][0].Total;  // ❌ 类型不符
```

### E. Func<...> 解码法

```csharp
Func<A, B, C> f;   // 吃 A、B，吐 C
//   ^^ ^   ^
//   输入   返回（最后一个永远是返回）

// 应用：
Func<List<Order>, Dictionary<string, decimal>> summarize;
//   ^─── 输入 ─^  ^──────── 返回 ──────────^
```

---

## 🏁 Day 50 一句话收官

> **"嵌套泛型 6 级阶梯全通；`Func<...>` 最后一个参数永远是返回类型 —— 这一条口诀（#43）单独就抵 LINQ 半本书。"**

---

## 🅿️ Variance 仍 PARK

延续 Day 49 决定：variance 继续放着，等真正进 LINQ 链/`IEnumerable<T>` 实战时从场景反推。
今天打通嵌套泛型已经是大跨步，把变性也塞进来会冲淡 ROI。
