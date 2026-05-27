# C# Learning Journey — Current Status

> **Last updated**: 2026-05-27 (Day 49 — Generics Session 1 close-out)
> **Mode**: C# Mentor walk-through (基础重建 detour + 表层泛型 + 显式 park variance)

---

## 🎯 Today's Actual Topic (Day 49)

Day 49 的真实主题**不是泛型本身**——而是借着读 `Repository<T> where T : class, IAuditable` 这一行，把 C# 类定义基础 (constructor / property / `init` / `required` / `=>` / 4 种 `new` 语法) 彻底重建一遍。

最终状态：`Generics-Constraints-Variance.cs` **GREEN ✅**（`dotnet run` exit code = 0）。

> Day 48 的 4 段冲刺（CI/CD + Kestrel + ACA + OIDC）所有 artifact 已落盘：
> `CI-CD-Closeout-Summary.md` / `Kestrel-Docker-DeepDive-Summary.md` /
> `ACA-Deployment-Milestone-Summary.md` / `Automation-OIDC-Theory-Summary.md`。
> 该日终结的 21 个口诀 + 坑 30–43 保留在各自 summary + cheatsheet 里。

---

## ✅ Day 49 Done

**基础重建（核心 ROI 在这里）：**
- [x] 接口 ≠ DI ——锚定区分（interface 是语言契约，DI 是模式 + 框架）
- [x] 类的 5 个基础部位：ctor / field / property / `init` / `required`
- [x] `=>` 三身份（lambda / 表达式属性体 / 表达式方法体）—— 位置决定身份
- [x] `new` 的 4 种语法 + 何时可省 `()`
- [x] `required` × 默认值 = 默认值永远不赢（编译期门卫 vs 运行期值）
- [x] `new()` 约束的真正用途：让方法体内可写 `new T()`，**仅此而已**
- [x] CS9040 编译错：`required` 和 `new()` 互斥（两个承诺打架）
- [x] 工厂方法 (factory method) 概念：`CreateDefault()` / `Dictionary.GetOrAdd` / EF Core

**泛型表层：**
- [x] 泛型动机：避免 `object` cast + 避免每个类型复制一份代码
- [x] 泛型约束矩阵：`class` / `struct` / 基类 / 接口 / `new()` / `notnull` / `unmanaged`
- [x] **类型实参替换发生在编译期**——不是运行期 cast

**变性 (Variance)：**
- [x] `out T` = 协变 covariance（T 只能作返回值）—— 表层
- [x] `in T` = 逆变 contravariance（T 只能作参数）—— 表层
- [x] 默认 invariance：`string : object` ≠ `IBox<string> : IBox<object>`
- [x] 实验验证过：删 `out` → exit 1；恢复 `out` → exit 0
- [🅿️] **变性"为什么安全"的深层 reasoning ——PARKED**（下次回头路已经写好）

---

## 🧠 Day 49 Locked 的 10 个口诀（编号续 Day 48 的 21 个）

22. **接口 ≠ DI** — 看见 `interface` 别立刻喊 DI；DI 必用接口，但反之不成立
23. **`=>` 三身份** — 位置决定身份：参数列表 / 属性签名 / 方法签名
24. **`new` 4 语法** — `()` 在 `{ }` 跟随时可省，仅限无参构造
25. **`required` 是编译期门卫** — 默认值在它面前永远落败
26. **`new()` 约束只为方法体** — 让 `new T()` 能写出来，仅此而已
27. **CS9040 — `required` × `new()` 互斥** — 两个承诺直接打架
28. **工厂方法回答"谁来 new、何时 new"** — generic + DI 场景里频繁出现
29. **类型实参替换发生在编译期** — `Repository<T>` → `Repository<Order>` 不是运行期 cast
30. **默认 invariance** — 继承关系不会自动延伸到类型实参关系，要 opt-in
31. **知道何时该 park，比硬怼一个不明白的概念更重要** ← 今天最大的元收获

---

## 📂 Files touched today (Day 49)

```
2026-05-27/
  Generics-Constraints-Variance.cs     ← GREEN ✅ (new()/CreateDefault 注释掉，out/in 在)
  Generics-Constraints-Variance.csproj
  Generics-Session-1-Summary.md        ← REWRITTEN (基础重建 + variance park)
  Current-Learning-Status.md           ← this file (Day 49 update)

dailyread/
  dailyread-2026-05-27.html            ← UPDATED (追加 Day 49 段落，含变性 park 回头路)

dailyread.html                          ← UPDATED (05-27 卡片 title/desc 追加 Day 49 关键词)
csharp-syntax-cheatsheet.html           ← UPDATED (追加坑 44–53，共 10 个新坑)
```

---

## 🅿️ Variance 显式 Park — 回头路 breadcrumb

下次重新进 variance，**不要从术语开始**，按这个顺序：

1. **先看 3 个真实接口**：`IEnumerable<T>` (out) / `Action<T>` (in) / `Func<TIn, TOut>` (in + out)
2. **建立"读 vs 写"直觉**：能读出 = out 安全；能写入 = in 安全；既读又写 = 必须 invariant
3. **画方向箭头**：`object ← string`（继承）vs `IBox<object> ← IBox<string>`（替换），看哪些方向 C# 允许
4. **再回来读 `out` / `in`** —— 这时它们应该已经"显然"

---

## 🪜 Next Session — Day 50 已锁定 🎯

> **明天主题**: **嵌套泛型类型阅读流利度 (Nested Generics Reading Fluency)**
>
> 出发点：用户自述"`List<int>` 和 `List<T>`、`List<List<List<T>>>` 我大概能看懂，但是读起来吃力。"
> → 这是把今天的泛型从"知道"推到"看一眼就懂"的最实用一步。
> → Variance 继续 🅿️ park（已确认）。

### 计划覆盖的 6 级阶梯

| 级别 | 例子 | 训练目标 |
|---|---|---|
| L1 — 单层闭合 | `List<int>` | "T 被换成 int 的 List" — 看一眼脱口而出 |
| L2 — 单层开放 | `List<T>` / `Repository<T>` | 区分开放 / 封闭泛型类型 |
| L3 — 双层嵌套 | `List<List<int>>` / `Dictionary<string, int>` | 2D 矩阵 + 字典；`m[0][0]` 索引练习 |
| L4 — 三层嵌套 | `List<List<List<T>>>` | 3D 立方体；`cube[i][j][k]` 索引；用真实场景理解（比如分组×订单×行项目） |
| L5 — 混合容器 | `Dictionary<string, List<Order>>` / `IDictionary<TKey, IEnumerable<TValue>>` | 接口嵌套 + 现实业务签名 |
| L6 — Delegate 包裹泛型 | `Func<Dictionary<string, List<int>>, List<string>>` | "丑陋签名" 解码 |

### 读法心法（明天会反复练）

- **从外往内**：先看最外层是什么容器 → 它装的是什么 → 再展开
- **从右往左给名字**：`List<Order>` 读 "Order 的列表"
- **加业务上下文**：`Dictionary<string, List<Order>>` 读 "按 string（customerId）分组的 Order 列表"

### 产出预期

- `2026-05-28/Nested-Generics-Fluency.cs` — 6 个 demo + 5 道"看签名说人话"小测
- `2026-05-28/Nested-Generics-Summary.md` — 心法 + 易错点
- Cheatsheet 追加 5–8 个新坑（嵌套泛型相关）

---

## 🏁 Today's One-Line Win (Day 49)

> **"接口 ≠ DI、`=>` 三身份、`new` 四语法、`required` × `new()` 互斥——今天该懂的小石头全部对位。变性那块大石头先放着，下次从 `IEnumerable<T>` 进来再撬。"**

---

## 🗂️ Day 48 Recap (earlier today — 4 sessions, all closed)

完整内容见各自的 summary 文件。本节仅做索引：

| 段落 | 关键产物 | 关键口诀号 |
|---|---|---|
| 早上 CI/CD close-out | Run #3 success · GHCR image live | 1–5 |
| 傍晚 Kestrel/SDK/反向代理 深挖 | `Kestrel-Docker-DeepDive-Summary.md` | 6–11 |
| 晚上 ACA 部署 bonus 🎯 | `temperature-sensor` live in `marlondb`/eastasia · 4 curl green | 12–16 |
| 夜里 Automation + OIDC 理论 | `Automation-OIDC-Theory-Summary.md` · 纵深防御 4 层 | 17–21 |

**Azure live**（持续运行直到 delete）：
- Container App `temperature-sensor` · rg `marlondb` · region `eastasia`
- Environment `cae-tempsensor-dev`
- FQDN: `https://temperature-sensor.thankfulplant-cb0fc5e9.eastasia.azurecontainerapps.io`
