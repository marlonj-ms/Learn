# 2026-05-19 — LINQ Drills + Dictionary 进阶 总结

今天主线：补基础语法肌肉记忆 → 创建 Core Syntax 课程 → 进入 Drill 模式（自己写、我纠错）→ 完成 5 个连续 drill。

## 一句话主线
> Async 节流（SemaphoreSlim）→ Core Syntax 基础课程 → Brackets 参考 → Drill 5/6/7/8（Book/LINQ/Dictionary）

## 完成的文件
- `Async-Demo/Program.cs`（昨天的）加上 V5 节流版（SemaphoreSlim(3)、Interlocked 计数）
- `CSharp-Core-Syntax-Foundations.md`：7 模块基础课
- `CSharp-Brackets-And-Arrow-Reference.md`：`<> () {} =>` 四符号深度参考
- `Drill5-Book/`：构造函数 + List<Book> 装对象
- `Drill6-BookQueries/`：Where / Select / OrderBy / First / Distinct / string.Join / Sort + Comparison<T>
- `Drill8-WordCount/`：Dictionary 计数（ContainsKey 版 + TryGetValue 版）

## 学到的概念
| 概念 | 关键点 |
|------|--------|
| 集合初始化器 | `new List<T> { ... }` 需要 IEnumerable + Add + 无参构造 |
| 对象初始化器 | `new Foo { Prop = val }` 需要 public 属性 + 无参构造 |
| target-typed new | `List<T> x = new() { ... }` |
| `<>` 含义 | 泛型，里面写类型 |
| `()` 含义 | 调用 / 签名 / cast / tuple / 分组 |
| `{}` 含义 | 代码块 / 初始化器（看左边 token 判断） |
| `=>` 含义 | lambda 或表达式体 |
| LINQ 链 | Where 过滤 / Select 投影 / 不放副作用 |
| Sort | 接 `Comparison<T>` 委托，返回 int（不是 bool）|
| Aggregate | 通用聚合 = 累加器 + 折叠规则；Sum 是它的特例 |
| TryGetValue | 一次哈希查找，不在时 out 拿默认值 |
| 计数模式 | `dict.TryGetValue(k, out int c); dict[k] = c + 1;`（2 行无 if） |

## 反复踩的坑（重点！）
1. **`_field` 只能在类内部访问** — 在 Main 里写 `_title` 一共错了 3 次。外部必须走 public 属性。
2. **插值字符串少 `$`** — `"{var}"` 打印的是字面文本。
3. **LINQ 不放副作用** — 不在 `Select` 里 `Console.WriteLine`，那是数据管道。
4. **`Sort` 比较器返回 int 不是 bool** — `(a, b) => a.X - b.X`。
5. **`dict[k] = v` vs `dict.Add(k, v)`** — 前者"在则改不在则插"，后者"在则抛"。
6. **lambda 参数名遮蔽外层变量** — `g.Select(g => g.Title)` 能跑但容易看错，用 `b` 区分。

## 明天/后续可选方向
- Drill 9：嵌套 `Dictionary<string, List<T>>` 或 `Dictionary<K, V>` 与 GroupBy 对比
- TemperatureSensor 事件练习（`2026-05-18/Practice-Exercise/`）
- 第一个 xUnit 单元测试（给 Book 或 NumberBag 写测试）
- 依赖注入（DI）入门
