# Tests Refresher Summary — 2026-05-26

> **Day 47**｜C# Learning Journey ｜Mode：C# Mentor walk-through (mode A — mentor reviews, learner writes)  
> **Focus**：xUnit fundamentals refresher before moving to integration tests + Docker (Session N+3)  
> **Folder**：`d:\AITriage\2026-05-26\`  
> **Final state**：13 test cases all green ✅ in `TemperatureSensor.Core.Tests`

---

## 🎯 Mission Today

刷新 xUnit 测试基本功，把上次没讲透的几个概念**真正锁住**——  
test ordering 哲学、static state trap、`Assert.Throws<T>` 内部机制、try/catch 反模式。

---

## 📚 今日重点概念（按学习顺序）

### 1️⃣ `dotnet test` 默认 verbosity 太低 — 看不到每行 Theory case

**问题**：bare `dotnet test` 只显示统计摘要，看不到每个 `[Theory]` row 名字。

**解决（三种等价写法）**：
```powershell
dotnet test --logger "console;verbosity=detailed"
dotnet test -v normal
dotnet test -v detailed
```

**输出差别**：
- Default: `Passed! - Failed: 0, Passed: 13, Skipped: 0, Total: 13`
- Detailed: `RecordReading_InvalidValues_ThrowsArgumentException(invalidValue: NaN) [PASS]`

> 🔒 **Lock-in**：调试测试时，永远开 detailed verbosity——能立刻定位哪个 [Theory] row 挂了。

---

### 2️⃣ Test : Production 代码比例 — 工业现实

| 代码类型 | 测试代码:生产代码 比例 |
|---|---|
| 工具函数（utilities） | 1:1 |
| 业务逻辑（business logic） | 2:1 |
| 边界 / 安全关键（critical） | 3:1 ~ 5:1 |

> 🔒 **Lock-in 比喻**："测试不是减速带，是安全带（test is not a speed bump, it's a seat belt）。"

**测试代码比生产代码多很正常**——一个方法里编码了 N 个决策（boundary、null、异常、并发），每个决策都需要单独验证。

**例外**：throw-away 脚本、原型、PoC 代码——不是所有代码都需要测试。

---

### 3️⃣ `[Theory] + [InlineData]` vs `[Theory] + [MemberData] + TheoryData<T>`

#### `[InlineData]`
```csharp
[Theory]
[InlineData(double.NaN)]
[InlineData(double.PositiveInfinity)]
[InlineData(double.NegativeInfinity)]
public void Test(double invalidValue) { ... }
```
- ✅ 简单直接
- ❌ 只接受 **compile-time constants**（数字、字符串、bool、null、typeof）
- ❌ 不能放 `new MyObject()`、`DateTime.Now` 等运行时值

#### `[MemberData] + TheoryData<T>`
```csharp
public static TheoryData<double> InvalidReadings => new()
{
    double.NaN,
    double.PositiveInfinity,
    double.NegativeInfinity
};

[Theory]
[MemberData(nameof(InvalidReadings))]
public void Test(double invalidValue) { ... }
```
- ✅ 可以放任意运行时构造的数据
- ✅ `TheoryData<T>` 强类型（compile-time check）
- ✅ `TheoryData<T1,T2,T3>` 支持多参数
- ✅ `nameof()` refactor-safe（重命名属性时编译器会跟着改）

#### 关键 C# 语法点
| 语法 | 中文术语（English term） | 含义 |
|---|---|---|
| `new()` | 目标类型 new 表达式（target-typed new expression）, C# 9+ | 编译器从左边的类型推断 |
| `nameof()` | nameof 表达式（nameof expression） | 编译时字符串字面量，重构安全 |
| `static` | 静态成员（static member） | 类级别，所有实例共享 |
| `=>` | 表达式主体成员（expression-bodied member） | `get`-only 属性的简写 |

---

### 4️⃣ xUnit 的测试执行哲学（颠覆直觉但有道理）

#### 4.1 **不保证顺序（no order guarantee）**
- xUnit **不**按源码顺序、不按字母顺序、不按定义顺序运行测试
- 同一个测试每次跑的顺序可能不同
- **为什么这么设计**：故意暴露隐藏的 order dependency bug

#### 4.2 **每个测试方法 = 新的类实例**（关键差异！）
```csharp
public class TemperatureSensorTests
{
    public TemperatureSensorTests() { /* ctor runs PER TEST */ }
    
    [Fact] public void Test1() { }   // ← new TemperatureSensorTests() #1
    [Fact] public void Test2() { }   // ← new TemperatureSensorTests() #2 (fresh!)
}
```
- xUnit ≠ NUnit / MSTest（后两者整个类共享一个实例）
- 每个 `[Fact]` / `[Theory]` row 都拿到**全新的 fresh instance**

#### 4.3 **并行执行（parallel execution）默认行为**
- **同一个类内**：sequential
- **不同类之间**：parallel（默认开启）

#### 4.4 **三条测试独立性铁律（test independence rules）**
1. **No order dependency**：不能依赖某个测试先跑
2. **No shared state mutation**：不能改外部共享状态
3. **No external resource dependency**：不能依赖文件 / 数据库 / 网络 / 时钟

> 🔒 **Lock-in**："测试隔离能挡 instance state，挡不住 static state、文件、数据库、网络、时钟。"

---

### 5️⃣ ⚠️ Static field trap（quiz 答错的地方 — 必须锁住！）

**场景**：
```csharp
public class TestClass
{
    private static int _counter = 0;   // ← static！

    [Fact] public void Test1() { _counter++; Assert.Equal(1, _counter); }
    [Fact] public void Test2() { _counter++; Assert.Equal(1, _counter); }
    [Fact] public void Test3() { _counter++; Assert.Equal(1, _counter); }
}
```

**实际发生**：
| 第 N 个跑 | `_counter` 累加后 | 断言结果 |
|---|---|---|
| 1 | 1 | ✅ |
| 2 | 2 | ❌ Expected 1, Actual 2 |
| 3 | 3 | ❌ |

**为什么**：
- xUnit **新建实例（new instance per test）** 只能重置**实例字段（instance field）**
- **`static` 是类级别（class-level）**——所有实例共享同一份
- `static int _counter = 0;` 的初始化只在**类型加载（class load）**时跑一次

**对比表**：
| 字段类型 | 中文 | 归属 | xUnit 每个 test 是否重置 |
|---|---|---|---|
| `private int _counter` | 实例字段（instance field） | 每个对象一份 | ✅ 重置 |
| `private static int _counter` | 静态字段（static field） | 整个类共享一份 | ❌ 不重置（累加！） |

**生产代码里常见的 static 陷阱**：
- `DateTime.Now` / `DateTime.UtcNow`（隐式 static 时钟）
- `Random.Shared`
- `static Dictionary<...>` 做缓存
- `static Logger`、`static HttpClient`

**解决方案**：把这些注入进来（DI），让代码可测——这就是 Session N+1 学的 DI 真正价值之一。

> 🔒 **Lock-in**：**`static` 是测试隔离机制的盲点（blind spot）。修饰符（modifier）和泛型类型参数（generic type parameter）是 code review 时最容易被眼睛跳过的关键字。**

---

### 6️⃣ ⚠️ try/catch 反模式（false positive 测试）

#### 反模式（坑！）
```csharp
[Fact]
public void Calculate_NegativePrice_Throws()
{
    var calc = new PriceCalculator();
    try
    {
        calc.Calculate(-10);
    }
    catch (ArgumentException) { /* expected */ }
}
```

**为什么是 bug**：
- 测试里**没有任何 `Assert.*` 调用**
- xUnit 判定 pass/fail 的规则极简：测试方法**有没有抛未捕获异常**
  - 干净返回 → ✅ pass
  - 抛 `XunitException` → ❌ fail
  - 抛其他异常 → ❌ fail
- 如果 SUT 被改坏不再抛异常 → try 块顺利返回 → catch 不被触发 → 测试方法干净返回 → **✅ 假阳性 pass**

> 🔒 **Lock-in**："**没有 `Assert.*` 调用的测试 = 没有断言的测试 = 不是测试，是'运行一遍代码看会不会爆'。**" 这种东西叫 **assertion-free test（无断言测试）**，给你**假的信心（false confidence）**。

#### 正确写法
```csharp
[Fact]
public void Calculate_NegativePrice_ThrowsArgumentException()
{
    var calc = new PriceCalculator();
    Assert.Throws<ArgumentException>(() => calc.Calculate(-10));
}
```

#### 行为对比
| 场景 | try/catch 版 | `Assert.Throws<T>` 版 |
|---|---|---|
| SUT 抛对的异常 | ✅ pass | ✅ pass |
| **SUT 不抛异常（被改坏）** | ✅ pass（**bug！**） | ❌ fail（safety net 起作用） |
| SUT 抛错的异常类型 | ❌ fail（异常逃出 catch） | ❌ fail（带清晰错误信息） |

---

### 7️⃣ `Assert.Throws<T>` 内部机制（揭秘"魔法"）

#### 伪代码实现
```csharp
public static T Throws<T>(Action testCode) where T : Exception
{
    try
    {
        testCode();                          // 跑你传入的 lambda
    }
    catch (T expected)
    {
        return expected;                     // ✅ 类型对 → 返回异常对象
    }
    catch (Exception wrongType)
    {
        throw new ThrowsException(typeof(T), wrongType);  // ❌ 类型不对
    }
    
    throw new ThrowsException(typeof(T));    // ❌ 根本没抛
}
```

#### 三种结局
| 实际发生 | 走哪条 path | 结局 |
|---|---|---|
| 抛了 `T` | `catch (T expected) → return` | ✅ pass，返回异常给你做后续断言 |
| 抛了其他类型 | `catch (Exception wrongType) → throw ThrowsException` | ❌ fail |
| 啥都没抛 | 走到最后一行 → `throw ThrowsException` | ❌ fail |

#### 关键术语区分（user 容易混的地方）
| 名字 | 是什么（type） | 角色 |
|---|---|---|
| `Assert.Throws<T>()` | **方法（method）** | 执行检查的"警察" |
| `ThrowsException` | **类（class，继承自 `XunitException`）** | 检查失败时抛出的"罚单" |

> 🔒 **Lock-in**：**`Assert.Throws<T>` 不是被动等异常——它是个"主动验证器（active validator）"。如果你的委托没抛 `T`，它就替你抛一个 fail 信号。这就是它能解决 try/catch 反模式的原因。**

---

### 8️⃣ `Assert.Throws<T>` 的"精确类型陷阱"（exact-type match）

```csharp
// SUT 抛 ArgumentNullException（继承自 ArgumentException）

Assert.Throws<ArgumentException>(...)       // ❌ FAIL！要求精确类型
Assert.Throws<ArgumentNullException>(...)   // ✅ pass

Assert.ThrowsAny<ArgumentException>(...)    // ✅ pass，接受所有子类
```

---

### 9️⃣ ⚠️ 对称失败（symmetric failure）— quiz 拓展锁住的新洞察

`Assert.Throws<T>` 的精确类型匹配在**两个方向都不放过**——和普通 C# `try/catch` 完全不同！

#### 普通 C# `try/catch` 是**不对称**的：
```csharp
try { throw new ArgumentNullException(); }
catch (ArgumentException) { /* ✅ catch 得到 — 父能接子 */ }

try { throw new ArgumentException(); }
catch (ArgumentNullException) { /* ❌ catch 不到 — 子接不了父 */ }
```

#### `Assert.Throws<T>` 是**对称失败**的（两个方向都 fail）：
| SUT 实际抛 | `Assert.Throws<T>` 写的 T | 结果 | 原因 |
|---|---|---|---|
| `ArgumentNullException`（子类） | `ArgumentException`（父类） | ❌ FAIL | 类型不完全相等 |
| `ArgumentException`（父类） | `ArgumentNullException`（子类） | ❌ FAIL | 类型不完全相等 |
| `ArgumentNullException` | `ArgumentNullException` | ✅ PASS | 完全相等 |

#### 底层判断逻辑（关键）
| 写法 | 判断逻辑 | 中文 |
|---|---|---|
| `catch (T expected)` | `expected is T`（多态匹配） | 父接子 OK |
| `Assert.Throws<T>` | `caught.GetType() == typeof(T)`（**精确相等**） | 父子兄弟一律不认 |

> 🔒 **Lock-in 比喻**：**"`Assert.Throws<T>` 看类型像看身份证号——完全相等才放行。父子兄弟堂表，一概不认。"**

---

### 🔟 选择正确的"类型检查工具"（decision tree）

xUnit 提供了 4 个 API，对应 2×2 矩阵：

|  | 严格精确（strict equality） | 多态匹配（polymorphic / `is`） |
|---|---|---|
| **检查异常** | `Assert.Throws<T>` | `Assert.ThrowsAny<T>` |
| **检查对象类型** | `Assert.IsType<T>` | `Assert.IsAssignableFrom<T>` |

**选择原则**：
- 你想锁死"必须是 `ArgumentNullException`，不能退化成 `ArgumentException`"——用 `Throws<T>`
- 你只关心"是 `ArgumentException` 家族里某种"——用 `ThrowsAny<T>`
- 同理：判断返回对象是不是某个**精确类型**用 `IsType<T>`，判断**是不是某接口/基类**用 `IsAssignableFrom<T>`

---

### 1️⃣1️⃣ `Assert.Throws<T>` 的返回值 — 三层断言深度（assertion depth）

`Assert.Throws<T>` 返回的不是 `void`，而是**被捕获的真实异常对象本尊**（same reference SUT threw）：

```csharp
public static T Throws<T>(Action testCode) where T : Exception
{
    catch (T expected) { return expected; }   // ← 原物返回
}
```

拿到这个对象后，可以继续**断言它的属性**——这就是 SUT 公共契约（public contract）的细节层。

#### 三层断言深度
```
┌──────────────────────────────────────────────────────────────┐
│ 浅： 类型正确          → Assert.Throws<T>()      （最低线）     │
│ 中： 类型 + 结构化字段 → var ex = ...; Assert.Equal(...,ex.X)   │
│ 深： 类型 + 字段 + 消息关键词 → Assert.Contains(关键词, ex.Message)│
└──────────────────────────────────────────────────────────────┘

选择原则：断言到能"抓住 regression、又不会被无关改动炸"的那一层。
```

#### 不同异常类型的"可断言结构化字段"
| 异常类型 | 关键属性 | 你会断言什么 |
|---|---|---|
| `ArgumentException` / `ArgumentNullException` | `ParamName` | 哪个参数错了 |
| `InvalidOperationException` | 一般只有 `Message` | 用 `Assert.Contains` 检查关键词 |
| `HttpRequestException` | `StatusCode` | 是 404 还是 500 |
| `SqlException` | `Number`, `LineNumber` | SQL 错误码 |
| 自定义异常（custom exception） | 自定义字段 | 如 `ex.ErrorCode == "INV001"` |

#### Production 智慧：哪些字段该断言、哪些不该
| 字段 | 谁读的 | 稳不稳定 | 能不能用 `Assert.Equal` |
|---|---|---|---|
| `ex.GetType()` | 程序读 | ✅ 合同的一部分 | ✅ 用 `Assert.Throws<T>` |
| `ex.ParamName` | 程序读 | ✅ 合同的一部分 | ✅ `Assert.Equal(...)` |
| `ex.ErrorCode`（自定义） | 程序读 | ✅ 合同的一部分 | ✅ `Assert.Equal(...)` |
| `ex.Message` | **人读** | ❌ 会改写 / 本地化 | ⚠️ 只用 `Assert.Contains(关键词)` |

> 🔒 **Lock-in**：**测试要断言"合同"，不要断言"措辞（wording）"。Contract is for machines, message is for humans.** 用 `Assert.Equal` 整段消息 = 脆测试（brittle test），改个错别字都会炸。

#### Regression 抓 bug 实例
| 测试写法 | SUT 把 `nameof(celsius)` 错改成 `nameof(_threshold)` |
|---|---|
| 只 `Assert.Throws<ArgumentException>(...)`（不接返回值） | ✅ pass — **漏掉了 bug（false positive）** |
| `var ex = Assert.Throws<...>(...); Assert.Equal("celsius", ex.ParamName);` | ❌ fail — **抓住了 bug** |

> 🔒 **Lock-in**：**"Assert 太浅 = 假阳性。"** 同样是 `Assert.Throws`，浅断言是"安慰剂（placebo）"，深断言才是真正的安全网。

---

## 📁 今日 Artifacts

### 文件
- [TemperatureSensor.Core.Tests/TemperatureSensor_UnitTest.cs](TemperatureSensor.Core.Tests/TemperatureSensor_UnitTest.cs) — 9 个测试方法，13 个测试用例
- 引用：`..\..\2026-05-20\TemperatureSensor.Core\TemperatureSensor.Core.csproj`

### 测试用例清单（全部 ✅）
| # | 测试方法 | 类型 | 验证什么 |
|---|---|---|---|
| 1 | `RecordReading_BelowThreshold_DoesNotFireEvent` | `[Fact]` | bool spy，25/30 |
| 2 | `RecordReading_AboveThreshold_FiresEvent` | `[Fact]` | bool spy，35/30 |
| 3 | `RecordReading_EqualThreshold_DoesNotFireEvent` | `[Fact]` | 严格 `>`，30/30 |
| 4 | `RecordReading_AboveThreshold_PassesCorrectPayload` | `[Fact]` | EventArgs capture |
| 5 | `RecordReading_AboveThreshold_NotifiesAllSubscribers` | `[Fact]` | 3 个计数器 |
| 6 | `RecordReading_SubscriberThrows_StopsLaterSubscribers` | `[Fact]` | multicast fail-fast |
| 7 | `RecordReading_NaN_ThrowsArgumentException` | `[Fact]` | `var ex = Assert.Throws<...>(...)` |
| 8 | `RecordReading_InvalidValues_ThrowsArgumentException` | `[Theory]` + `[InlineData]` × 3 | NaN / ±Infinity |
| 9 | `RecordReading_InvalidValues_ThrowsArgumentException1` | `[Theory]` + `[MemberData]` + `TheoryData<double>` × 3 | 同 #8，演示 MemberData |

### 🧹 遗留 cosmetic 项（不阻塞，下次随手改）
- `InvalidReading` → 应改为 `InvalidReadings`（复数）
- 该 `static` 属性顶格写了，应缩进 4 空格对齐类成员
- Test #9 的 `1` 后缀是临时对比用，决定是删 #8 还是删 #9
- Test #6 注释掉的 `sub2` 残留代码

---

## 🔒 今日九条 Locked Lessons

1. **`dotnet test` 默认 verbosity 隐藏每行 Theory 名字**——调试时永远用 `--logger "console;verbosity=detailed"` 或 `-v normal`。
2. **测试不是减速带，是安全带**——test:prod 比例 1:1 ~ 5:1 是工业常态。
3. **xUnit 三条独立性铁律**：no order dependency、no shared state mutation、no external resource dependency。
4. **`static` 是测试隔离机制的盲点**——xUnit 的"new instance per test"只保护实例字段，碰 `static` / 文件 / DB / 网络 / 时钟一律自找麻烦。
5. **没有 `Assert.*` 调用的测试 = 假阳性陷阱**——try/catch + 空 catch 是经典反模式，code review 应立即打回。
6. **`Assert.Throws<T>` 是主动验证器**——既检查"有没有抛"也检查"抛的类型对不对"，两个条件都满足才放行；它通过抛 `ThrowsException`（一种 `XunitException` 子类）来报告失败。
7. **`Assert.Throws<T>` 对称失败**——不像 C# `catch` 的"父接子"宽容多态，`Throws<T>` 看类型像看身份证号，父子双向都不认。想接受子类用 `ThrowsAny<T>`。
8. **四个类型检查 API 是 2×2 矩阵**：异常×对象 × 严格×多态 → `Throws<T>` / `ThrowsAny<T>` / `IsType<T>` / `IsAssignableFrom<T>`。选错了要么过度严格炸合理重构，要么过度宽松漏 regression。
9. **Assert 太浅 = 假阳性**——`Assert.Throws<T>` 不接返回值只验类型；接 `var ex = ...` 后断言 `ex.ParamName` / `ex.ErrorCode` 才能抓"类型对但细节错"的 regression。但**别用 `Assert.Equal` 比 `ex.Message` 整段**——message 是给人读的、不稳定，用 `Assert.Contains` 关键词即可。

---

## 📝 Review Questions（明天 quiz me 用）

### Easy
1. `dotnet test` 默认看不到 `[Theory]` 每行 case 名字，怎么打开详细输出？
2. 工业上 test 代码:production 代码典型比例是多少？
3. `[InlineData]` 不能放什么类型的值？为什么？

### Medium
4. xUnit 跑两个 `[Fact]` 时，会复用同一个测试类实例吗？这跟 NUnit / MSTest 有什么区别？
5. xUnit 三条测试独立性铁律是什么？
6. `TheoryData<T>` 比 `IEnumerable<object[]>` 好在哪里？

### Hard
7. 类里加了 `private static int _counter = 0;`，每个测试 `_counter++; Assert.Equal(1, _counter);`——会发生什么？为什么？
8. 下面这个测试为啥是 false positive？怎么改？
   ```csharp
   try { calc.Calculate(-10); } catch (ArgumentException) { }
   ```
9. `Assert.Throws<T>` 这个方法和 `ThrowsException` 这个类各自是什么角色？

### Expert（today's new layer）
10. SUT 抛 `ArgumentNullException`，`Assert.Throws<ArgumentException>(...)` 会 pass 吗？反过来呢（抛父接子）？为什么 `Assert.Throws<T>` 和普通 `catch` 行为不同？
11. xUnit 四个类型检查 API（`Throws<T>` / `ThrowsAny<T>` / `IsType<T>` / `IsAssignableFrom<T>`）的 2×2 矩阵怎么排？什么场景选哪个？
12. `Assert.Throws<T>` 的返回值类型是什么？拿到这个返回值后，你能做什么之前做不到的事？给一个真实的 regression 场景。
13. 为什么不该用 `Assert.Equal(整段文本, ex.Message)`？正确的 message 断言用什么 API？

---

## 🚀 Next Up

### 短期（明天可能）
- A) `Assert.Throws<T>` 精确类型陷阱 deep dive（`ArgumentException` vs `ArgumentNullException`，`ThrowsAny<T>`）
- B) 测试套件组织：`IClassFixture<T>` / `ICollectionFixture<T>` 共享 setup
- C) `IDisposable` / `IAsyncLifetime` 测试生命周期 hook

### 中期 — Session N+3（原计划）
- `WebApplicationFactory<T>` — 集成测试 API
- `Dockerfile` 多阶段构建 — 容器化 API
- 本地跑容器验证

### 远期 backlog
- Test doubles：mock / stub / fake / spy 区别 + Moq / NSubstitute
- TDD 红绿重构循环 discipline
- `TheoryData<T1,T2>` + `[ClassData]`
- E2E with Playwright

---

## 🧠 Style 反馈 / 教学笔记

- **User 答错 static quiz 反而是好事**——暴露了"修饰符跳过"的盲点，已 lock-in。
- **User 主动问"ThrowsException 这步不懂"**——非常好的"学习元能力（meta-learning skill）"，看到自己没懂的地方就停下来挖。
- **User 自己用话复述 `Assert.Throws` 机制**——主动 internalize，比单纯被讲懂效果好得多。下次继续这个模式：讲完 → user 复述 → 老师纠正用词。
- 今天信息密度很高（6 个大概念 + 1 个 bonus），但 user 都跟上了。建议下次别再叠了，给消化时间。
