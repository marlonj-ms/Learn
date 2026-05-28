# C# Learning Journey — Current Status

> **Last updated**: 2026-05-28 (Day 50 — Nested Generics Reading Fluency close-out)
> **Mode**: C# Mentor walk-through (6 级阶梯 + 业务签名解码)

---

## 🎯 Today's Topic (Day 50)

主题：**嵌套泛型阅读流利度 (Nested Generics Reading Fluency)**
出发点：Day 49 的"我看得懂但读起来吃力" → 训练成"一眼说人话 + 一手写访问"。

最终状态：6 级阶梯全部打通 ✅。`Nested-Generics-Fluency.cs` 6 个 demo 完整可跑。

---

## ✅ Day 50 Done

**嵌套泛型 6 级阶梯：**
- [x] L1 — `List<int>` 单层封闭；3 种初始化语法（完整 / 省空括号 / 目标类型 `new()`）等价
- [x] L2 — `List<T>` 开放泛型类型 vs `List<int>` 封闭泛型类型；开放型不能直接 `new`
- [x] L3 — `List<List<int>>` 锯齿数组 + `Dictionary<string,int>` 字典；中括号两种语义（index vs key）
- [x] L4 — `List<List<List<int>>>` 三层嵌套；剥洋葱模式不变
- [x] L5 — `Dictionary<string, List<Order>>` 混合容器；链式取值类型追踪
- [x] L6 — `Func<Dictionary<string,List<int>>, List<string>>` delegate 包裹嵌套泛型

**配套深挖（今天高 ROI 的 detour）：**
- [x] **空 List ≠ null** —— 空列表是有对象的（~40 字节），只是 `Count == 0`
- [x] **NRE 哲学**：传 null 安全，对 null 调方法才炸
- [x] `?.` 空条件运算符 vs `int?` 可空类型修饰 —— 同一个 `?` 两种身份
- [x] C# 8+ 帽子运算符 `[^N]` ≡ `[Count - N]`，`^1` = 最后一个
- [x] Dictionary 取值的 4 个真坑：`[]` 抛异常 / case-sensitive / TryGetValue 是正解 / `OrdinalIgnoreCase`
- [x] `decimal` 字面量必须带 `m` 后缀（钱用 decimal，never double）
- [x] 解码 Func<...> 心法：**目光直接弹到最后一个泛型参数 = 返回类型**

---

## 🧠 Day 50 Locked 的 12 个口诀（编号续 Day 49 的 #31）

| # | 口诀 |
|---|---|
| 32 | 空 List ≠ null —— 用 `Count == 0` 判空，别用 `== null` |
| 33 | "传 null 安全，对 null 调方法才炸" |
| 34 | `?.` 空条件运算符短路 —— 链中任一段 null 整条 null |
| 35 | `int?` 是类型修饰；`?.` 是操作修饰 |
| 36 | 目标类型 `new()` 跟着左侧期望类型走（C# 9+） |
| 37 | `List<List<T>>` ≠ `T[,]` —— 锯齿 vs 矩形 |
| 38 | 0-indexed：最后一个是 `Count - 1`，不是 `Count` |
| 39 | `list[^N]` ≡ `list[Count - N]` |
| 40 | Dictionary 取值用 `TryGetValue`，别用 `[]` |
| 41 | `Dictionary<string, ...>` 默认大小写敏感 |
| 42 | decimal 字面量必须带 `m` 后缀 |
| 43 | 读 `Func<...>`：目光直接弹到最后一个泛型参数 = 返回类型 |

至此口诀累计 **43** 条。

---

## 📂 Files touched today (Day 50)

```
2026-05-28/
  Nested-Generics-Fluency.cs       ← 6 级阶梯可跑 demo（main 一次跑完 6 关）
  Nested-Generics-Summary.md       ← 心法 + 12 口诀 + 9 坑 + 关键代码速查
  Current-Learning-Status.md       ← this file

dailyread/
  dailyread-2026-05-28.html        ← 5 分钟版子页（新页）

dailyread.html                      ← 首页 05-28 卡片插入；05-27 降级
csharp-syntax-cheatsheet.html       ← 追加坑 54–62（9 个新坑）
```

---

## 🪜 Next Session — Day 51 候选（用户选）

Day 50 把嵌套泛型读流利了，**下一步有两条强候选路**：

### 候选 A — LINQ 链式流利度 (推荐 🌟)

理由：LINQ 链返回的就是 `IEnumerable<IGrouping<TKey, TElement>>` 这种**重度嵌套泛型**签名。Day 50 的新技能立刻能复用 —— 不练 LINQ 就浪费今天积累的"读签名"肌肉记忆。

预期产出：
- `Where` / `Select` / `GroupBy` / `Join` / `OrderBy` / `Aggregate` 全家桶
- 链式返回类型追踪（`IEnumerable<T>` → `IGrouping<K,V>` → `List<T>`）
- 延迟执行 (deferred execution) vs 立即执行 (`ToList()` / `ToArray()`)
- LINQ + Func<>/Predicate<> 的天然结合

### 候选 B — Variance 回头路 (Day 49 显式 park 的回收)

理由：现在有了嵌套泛型流利度，可以从 `IEnumerable<T>` / `Action<T>` / `Func<TIn,TOut>` 三个真实接口反推 `out` / `in` 的"为什么安全"。Day 49 已经写好 breadcrumb。

预期产出：
- 协变 / 逆变 / 不变性的"读 vs 写"直觉
- `string : object` ≠ `IBox<string> : IBox<object>` 为什么不自动延伸
- 编译期 reasoning 实验（删 `out` → exit 1；加回 → exit 0）

### 候选 C — Records + Pattern Matching

理由：现代 C# 必备 2 大语法糖。带去做 LINQ / Clean Arch 更顺手。

预期产出：
- `record class` vs `record struct`
- 值相等 (value equality) 自动生成
- `with` 表达式
- Pattern matching 全家桶（type / property / tuple / list pattern）

> **建议默认：A（LINQ）**。直接吃今天积攒的红利。
> 让用户选 / 否决 / 给完全不同的方向。

---

## 🏁 Today's One-Line Win (Day 50)

> **"`List<int>` → `Func<Dictionary<string,List<int>>, List<string>>` 6 级阶梯一气通关；Func 最后一个泛型参数 = 返回类型 (#43) 这条口诀单独就抵 LINQ 半本书。"**

---

## 🅿️ Still Parked

- **Variance 深层 reasoning** —— Day 49 显式 park；下次可作为 Day 51 候选 B 回收
- **IL 检视 (`ilspycmd`)** —— Day 47 (05-20) 已装好工具，留待泛型 / records 时回看 codegen
- **Local NuGet feed 回环** —— Day 47 多目标 pack 出过 3 个 `.nupkg`，下次需要时再试

---

## 📚 Backlog (after Day 50)

- LINQ 链式流利度（强候选 A）
- Records + pattern matching（候选 C）
- Variance 回头路（候选 B）
- Test doubles (mock/stub/fake/spy) + Moq / NSubstitute
- TDD 节奏（red-green-refactor 在新 feature 上）
- EF Core 基础
- Clean Architecture 实战 refactor
- IL revisit（park）
- E2E 测试（Playwright）
