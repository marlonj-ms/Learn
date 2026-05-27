# Generics Session 1 — Constraints + Variance（基础重建 + 入门）

> **Day 49 · 2026-05-27**（接续 Day 48 多段冲刺之后）
> **State**: `Generics-Constraints-Variance.cs` 已 GREEN（`dotnet run` exit code = 0）
> **核心收获**: 这一天的真正主题不是泛型本身，而是**重建 C# 类定义基础**——并在结尾把变性 (variance) 显式 park 掉。

---

## 🧭 Mental Path — 今天实际走过的路

```
Generics Session 1 开场
   ↓
"稍微等，我注意到这个文件涉及 DI"          ← 用户的关键提问
   ↓
澄清：接口 ≠ DI                            ← 第一个大锚点
   ↓
要求逐行讲解 → 暴露了"类怎么定义"基础松动
   ↓
🛠️  基础重建 detour（核心 ROI 在这里）
       - 构造函数 vs 字段 vs 属性
       - init / required 的真正语义
       - => 的三种身份
       - new 的 4 种语法
       - required + new() 互斥 (CS9040)
       - 工厂方法 (CreateDefault) 概念
   ↓
回到泛型主题：T、约束、Repository<T>
   ↓
变性 (out / in) 攻关
   ↓
"完全没懂" → 重新讲 default invariance
   ↓
"这里暂时放弃" → 🅿️ park variance，标好回头路
```

---

## 🔑 今天真正锁定的 9 个概念

### 1. 接口 ≠ DI（Dependency Injection）

| 角度 | 接口 (interface) | DI 容器 (IServiceCollection) |
|---|---|---|
| 是什么 | C# 语言特性，**契约** | 设计模式 + 框架 |
| 用途 | "我承诺有这些成员" | "请你帮我组装我的依赖" |
| 看到 `IAuditable` 出现 | **不一定**用 DI | DI 必用接口，但反之不成立 |

> **锚句**: "看见 `interface` 不代表在用 DI。`IAuditable` 在这个文件里只是泛型约束 (`where T : IAuditable`)，**没有任何 `IServiceCollection`、`AddSingleton`、构造函数注入**——所以这里和 DI 无关。"

---

### 2. 类的 5 个基础部位

```csharp
public class Order : IAuditable                       // 4. 继承/接口实现
{
    public required string Id { get; init; }          // 3. 属性 (property) + 访问器
    public DateTime CreatedAt { get; init; }          //    + init 修饰符
        = DateTime.UtcNow;                            // 5. 默认值
    public decimal Amount { get; init; }

    public override string ToString()                 // 2. 方法
        => $"Order(Id={Id}, Amount={Amount})";        //    用 => 代替方法体
}
```

- **字段（field）vs 属性（property）**: 字段是纯存储；属性是 `get`/`set`/`init` 访问器包装的"看似变量"。
- **`init` 访问器**: 只允许在**对象初始化阶段**赋值（构造函数 + `new C { ... }` 内），之后只读。
- **`required` 修饰符**: C# 11+ 关键字，**强制调用方必须在 `new` 时给它赋值**，否则编译错。

---

### 3. `=>` 的三种身份（关键混淆点）

| 位置 | 身份 | 含义 |
|---|---|---|
| `(x) => x * 2` | Lambda | 输入 → 输出 |
| `public int X => _x;` | 表达式属性体 (expression-bodied member) | 简写一个返回式 getter |
| `public int Add(int a, int b) => a + b;` | 表达式方法体 | 简写一个单返回式方法 |

> **锚句**: "`=>` 是个长得一样的符号，但**位置**决定身份。看到它别立刻当 lambda——可能只是个方法的简写。"

---

### 4. `new` 的 4 种语法

```csharp
// (1) 完整形式：构造函数 + 对象初始化器
new Order() { Id = "A-100", Amount = 88.8m }

// (2) 省略构造函数空括号（只有无参构造时才行）
new Order { Id = "A-100", Amount = 88.8m }

// (3) target-typed new (C# 9+)
Order o = new() { Id = "A-100", Amount = 88.8m };

// (4) 集合初始化
var list = new List<int> { 1, 2, 3 };
```

> **要点**: `()` 不是"被吞掉了"，而是 C# 允许在**有 `{ }` 跟随时**省略**无参括号**。背后仍然调用了无参构造函数，再做属性赋值。

---

### 5. `required` × 默认值初始化器 — 默认值永远不会赢

```csharp
public required string Id { get; init; } = "DEFAULT";
//          ↑ 强制要求传                   ↑ 默认值
// 即使写了 = "DEFAULT"，调用方依然必须显式赋值
// 因为 required 是编译期检查，不看运行期默认值
new Order { /* Id 没传 */ }  // ❌ CS9035 编译错
new Order { Id = "DEFAULT" } // ✅ 显式传了才行
```

> **锚句**: "`required` 是编译期门卫；默认值是运行期值。编译期失败的代码永远不会走到运行期，所以**默认值在 `required` 面前永远不会被采纳**。"

---

### 6. `new()` 约束（constraint）是关于"方法体"的，不是关于 T 的

```csharp
public class Repository<T> where T : class, IAuditable, new()
//                                              ─────  ─────
//                            T 必须是引用类型 + 实现接口 + 有无参构造函数
{
    public T CreateDefault() => new T();  // ← 这一行是 new() 存在的全部原因
}
```

- `new()` 约束的存在**只是为了让方法体里能写 `new T()`**。
- 不写这个约束，编译器不允许你 `new T()`，因为它不能保证 T 有无参构造函数。

---

### 7. CS9040 — `required` 和 `new()` 互斥

```csharp
public class Order : IAuditable
{
    public required string Id { get; init; }   // ← required 要求显式传值
}

new Order();   // ❌ CS9035 — required 成员没传
```

```csharp
public class Repository<T> where T : class, IAuditable, new()
{
    public T CreateDefault() => new T();   // ❌ CS9040 — T 有 required 成员，不能无参构造
}
```

> **锚句**: "`required` 把"必须传值"写进类型签名；`new()` 承诺"我能无参构造"。两个承诺直接打架——编译器替你提前发现。"

---

### 8. 工厂方法模式（factory method）— 在哪里见到

```csharp
// Dictionary.GetOrAdd 经典签名
public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

// EF Core
DbContextOptions.UseSqlServer(connectionString);  // 内部用 factory delegate 延迟构造

// 你今天看到的简化版
public class Repository<T> where T : class, IAuditable, new()
{
    public T CreateDefault() => new T();  // ← 这是"我自己也能造一个 T"的工厂能力
}
```

> **要点**: 工厂方法回答"**谁来 new 这个东西，什么时候 new**"。在 generic + DI 场景里非常常见。

---

### 9. 泛型动机 (motivation) + 约束 (constraints)

```csharp
// 不用泛型：要么 object（不安全 cast），要么每个类型复制一份代码
public class ObjectRepository { void Add(object item); }     // ← 退化为 object

// 用泛型 + 约束
public class Repository<T> where T : class, IAuditable
//                          ─────────  ─────────────────
//                          T 是引用类型 + T 实现 IAuditable
{
    public void Add(T item) { /* 编译期就知道 item 有 .CreatedAt */ }
}
```

| 约束 | 含义 |
|---|---|
| `where T : class` | T 必须是引用类型 |
| `where T : struct` | T 必须是值类型 |
| `where T : SomeBase` | T 必须继承自 SomeBase |
| `where T : ISomeInterface` | T 必须实现接口 |
| `where T : new()` | T 必须有公开无参构造 |
| `where T : unmanaged` | T 必须是非托管类型 |
| `where T : notnull` | T 不可为 null |

---

## 🅿️ Variance — 显式 PARK 标注

### 知道的部分

- `out T` 关键字标在泛型接口的类型参数上 = **协变 (covariance)**，允许 `IProducer<string>` 当 `IProducer<object>` 用。
- `in T` 关键字标在泛型接口的类型参数上 = **逆变 (contravariance)**，允许 `IConsumer<object>` 当 `IConsumer<string>` 用。
- 没标 `out` / `in` 时 = **不变 (invariance)**，C# 默认是不变。
- 经过实验验证：把 `out` 删掉 → 编译失败 (exit code 1)；恢复 `out` → exit code 0。

### Park 的部分

> **"为什么 `out` 就能让协变安全"——这个深层 reasoning 今天没拿下。**

- 直觉框架"声称 vs 实际 (claim vs reality)"已经讲过，但还没在脑子里真正落地。
- 用户决定**先 park**，不强推。

### 🔖 回头路（breadcrumb for future revisit）

下次重新进 variance 时，**不要从术语开始**，而是按这个顺序：

1. **先看 3 个真实接口**：`IEnumerable<T>`（out）、`Action<T>`（in）、`Func<TIn, TOut>`（in + out）。
2. **建立"读 vs 写"直觉**：能读出 = out 安全；能写入 = in 安全；既读又写 = 必须不变。
3. **画方向箭头**：`object ← string`（继承箭头）vs `IBox<object> ← IBox<string>`（泛型替换箭头），看哪些方向 C# 允许。
4. **再回来读 `out` / `in` 关键字**——这时它们应该已经"显然"。

---

## ✅ Done Today

- [x] 类的基础部位（ctor / field / property / init / required）线下拼写过
- [x] `=>` 三身份能立刻分辨
- [x] `new` 的 4 种语法 + `()` 何时可省
- [x] `required` × 默认值 = 默认永远不赢
- [x] `new()` 约束的真正用途（方法体可 `new T()`）
- [x] CS9040 = `required` + `new()` 互斥
- [x] 工厂方法模式概念（`CreateDefault()`、`GetOrAdd`、EF Core）
- [x] **接口 ≠ DI** ——重要锚点
- [x] 泛型动机 + 约束矩阵
- [x] `out` / `in` 关键字**表层**含义
- [🅿️] 变性"为什么安全"的深层 reasoning ——**PARKED**

---

## 🎯 Today's One-Line Win

> **"接口 ≠ DI、`=>` 三身份、`new` 四语法、`required` × `new()` 互斥——今天该懂的小石头全部对位。变性那块大石头先放着，下次从 `IEnumerable<T>` 进来再撬。"**
