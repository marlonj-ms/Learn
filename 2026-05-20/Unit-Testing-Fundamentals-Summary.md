# 2026-05-20 — Unit Testing Fundamentals + xUnit + .NET Build Internals 总结

今天主线：从"什么是单元测试"零基础起步，写手动 PASS/FAIL → 抽方法 → 建 solution + 项目 → 转 xUnit → 制造 bug → 回归被抓住。中途还顺带把"编译 vs 运行"、"反射 vs 引用"、"NuGet vs dll"三个 .NET 关键底座搞清楚了。

## 一句话主线

> 手写 PASS/FAIL → AAA 抽方法 → 手写 runner 暴露 4 大痛点 → 建 .slnx / classlib / xunit 项目 → 加 ProjectReference → 转 `[Fact]` + `Assert` → 制造回归 → 看 FAIL stack trace → 修回绿 → 解构 dotnet test 的 5 阶段 → 反射 + metadata + NuGet → dll 流转

## 完成的代码

- `2026-05-18/Practice-Exercise/Program.cs`
  - `Test_AboveThreshold_RaisesEvent()` 手写 PASS/FAIL 测试
  - `Test_AtOrBelowThreshold_DoesNotRaiseEvent()` 手写负向测试
  - `Main` 当 mini test runner
- `2026-05-20/TemperatureSensor.slnx` —— 解决方案文件
- `2026-05-20/TemperatureSensor.Core/`
  - `TemperatureSensor.Core.csproj` —— classlib，无 Main
  - `TemperatureSensor.cs` —— `ThresholdExceededEventArgs` + `TemperatureSensor`，**零 Console.WriteLine**
- `2026-05-20/TemperatureSensor.Tests/`
  - `TemperatureSensor.Tests.csproj` —— xunit 模板 + ProjectReference 到 Core
  - `TemperatureSensorTests.cs` —— 两个 `[Fact]` 测试

## 核心概念清单（按主题分类）

### A. 单元测试核心概念

| 概念 | 中文 / English | 一句话理解 |
|---|---|---|
| 单元测试 | unit test | 一段自动判断 PASS/FAIL 的代码，不需要人眼 |
| 被测单元 / 被测系统 | unit under test / system under test (SUT) | 通常是一个方法或一个类 |
| 期望行为 | expected behavior | 测试声明的"代码应该这样工作" |
| 自动判断 | automated verdict | 由代码自己决定 PASS/FAIL，不靠人眼 |
| 手动验证 | manual verification | 人眼看终端输出 —— 不算测试 |
| AAA 模式 | Arrange / Act / Assert | 所有单元测试的三段式骨架 |
| 测试间谍 / 标志变量 | test spy / flag variable | `bool fired = false;` 用来观察 void 副作用 |
| 闭包 | closure | lambda 捕获并修改外层局部变量 |
| 红→绿循环 | red-green cycle | 先让测试 FAIL 一次，证明断言真的有用 |
| 正向 / 负向测试 | positive / negative test | "应该发生" + "不应该发生"配对 |
| 失败消息 | failure message | `Assert.True(x, "...")` 第二个字符串参数，FAIL 时打印 |
| 回归 | regression | 原本能工作的功能被后续修改弄坏 |
| 回归测试 | regression test | 抓住回归的测试 |

### B. xUnit 框架

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 特性 | attribute | 嵌在代码上的标签，如 `[Fact]` |
| `[Fact]` | fact | 标记"这是一个 xUnit 测试方法" |
| `[Theory]` + `[InlineData]` | theory | 参数化测试（明天可学） |
| `public void` | — | xUnit 要求测试方法是 public void（异步是 async Task） |
| 实例隔离 | per-test instance | xUnit 给每个测试方法新建测试类实例 |
| 命名约定 | test naming | `<Scenario>_<ExpectedBehavior>`，不加 `Test_` 前缀 |
| `Assert.True(x, msg)` | — | 期望 `x` 为 true |
| `Assert.False(x, msg)` | — | 期望 `x` 为 false（优于 `Assert.True(!x)`） |
| `Assert.Equal(expected, actual)` | — | 期望两值相等 |
| `Assert.Throws<T>(() => ...)` | — | 期望 lambda 抛出异常 T |
| `Assert.Contains(item, collection)` | — | 期望集合包含 item |
| `Assert.Null` / `Assert.NotNull` | — | 引用断言 |
| 全局 using | global using | csproj 里 `<Using Include="Xunit" />` 自动给每个 .cs 文件加 `using Xunit;` |

### C. 项目 / 解决方案结构

| 概念 | 中文 / English | 是什么 |
|---|---|---|
| 解决方案 | solution (`.slnx`) | 多个相关项目的组织容器，不参与运行时 |
| 项目 | project (`.csproj`) | 一个可编译单元，产出一个 `.dll` 或 `.exe` |
| 项目引用 | project reference | 一个项目声明依赖另一个项目，编译器自动链接 |
| 类库项目 | class library project | 无 Main，产出 `.dll`，供别的项目消费 |
| 测试项目 | test project | 也是 dll，由 xUnit runner 当 host 运行 |
| 入口项目 / 宿主 | host project | 有 Main 或运行时框架（ASP.NET / Functions）的项目 |
| 关注点分离 | separation of concerns | 库只管核心逻辑，host 管 UI / 日志 / IO |
| 镜像目录结构 | mirror structure | `src/Foo/Bar.cs` ↔ `tests/Foo/BarTests.cs` |

### D. 编译时 vs 运行时（.NET 底座）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 编译时 | compile-time | 源码 → IL 这一刻；编译器要解析所有符号 |
| 运行时 | runtime | 程序在跑；CLR 在执行 IL |
| 源代码 | source code | `.cs` 文件 |
| 中间语言 | IL (Intermediate Language) | C# 编译产物，独立于 CPU 的字节码 |
| 即时编译 | JIT (Just-In-Time) | 运行时把 IL 翻成 CPU 指令 |
| 程序集 | assembly | `.dll` 或 `.exe`，里面装着 IL + metadata |
| 元数据 | metadata | dll 自带的"我有什么类 / 方法 / 特性"的索引 |
| 反射 | reflection | 程序在运行时读 metadata 并动态调用方法 |
| 调试符号 | debug symbols (`.pdb`) | 让 stack trace 显示文件 + 行号 |

### E. NuGet / 构建 / 部署

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| NuGet 包 | NuGet package (`.nupkg`) | 分发格式（zip） |
| 包还原 | package restore | 把 `.nupkg` 下载并解压到 `~/.nuget/packages/` |
| 传递依赖 | transitive dependency | 依赖的依赖（A→B→C） |
| 输出目录 | output directory | `bin/{Config}/{TFM}/` 自包含的运行单位 |
| 探测路径 | probing path | 运行时找 dll 的搜索路径，最常见是"同一文件夹" |
| 拓扑序构建 | topological build | 按依赖顺序编译（先 Core 后 Tests） |
| 部署单元 | deployment unit | 整个 `bin/Release/net10.0/publish/` 文件夹 |

### F. 生产架构

| 概念 | 中文 / English | 一句话 |
|---|---|---|
| 类库 = 积木 | library = building block | 类库不能独立运行，必须被 host 消费 |
| 入口项目 | host project | 有 Main / 运行时框架的项目 |
| 生产可观测性 | observability | 生产环境用日志 / 指标 / 追踪代替断点调试 |
| 三大支柱 | logs / metrics / traces | 日志、指标、分布式追踪 |
| 横切关注点 | cross-cutting concerns | 日志、安全、监控等不属于业务逻辑的关注点 |

## 关键心智模型

### 1. 单元测试的本质

```text
人 ──写测试代码──> 测试代码 ──自己判断──> PASS / FAIL
                                    没有"看终端"这一步
```

### 2. AAA 三段式

```text
┌──────────┬──────────────────────────────────────┐
│ Arrange  │ 建对象、设状态、订阅事件、准备输入   │
├──────────┼──────────────────────────────────────┤
│ Act      │ 调用被测代码（通常只有一行）         │
├──────────┼──────────────────────────────────────┤
│ Assert   │ Assert.True / False / Equal ...      │
└──────────┴──────────────────────────────────────┘
```

### 3. 测试事件的套路（spy 模式）

```csharp
// Arrange
bool fired = false;                                  // 标志变量
target.SomeEvent += (s, e) => fired = true;          // lambda 闭包翻牌

// Act
target.DoSomething();                                // 触发事件路径

// Assert
Assert.True(fired);     // 或 Assert.False(fired); 看场景
```

### 4. 解决方案 / 项目 / 引用

```text
┌─── TemperatureSensor.slnx ─────────────────────────────────┐
│                                                            │
│  ┌──── Core.csproj ─────┐    ┌──── Tests.csproj ────────┐  │
│  │ 类库（class lib）    │ ◄──│ ProjectReference         │  │
│  │ 无 Main              │    │ + xunit + test SDK       │  │
│  │ 产出 Core.dll        │    │ 产出 Tests.dll           │  │
│  │ (部署给用户)         │    │ (不部署，只 CI 用)       │  │
│  └──────────────────────┘    └──────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

### 5. 编译时 vs 运行时

```text
编译时                          运行时
═══════                          ═══════
.cs ──编译──> IL (.dll)         IL ──JIT──> CPU 指令 ──执行

编译器看引用                    运行器看 metadata（反射）
                                  ↑
                            xUnit 的工作领域
```

### 6. NuGet → dll 的流转

```text
NuGet.org / 私有源
    │
    │ dotnet restore
    ▼
~/.nuget/packages/xunit/2.9.3/...
    │
    │ dotnet build
    ▼
bin/Debug/net10.0/*.dll      ← 这才是运行时真正加载的
    │
    │ dotnet test / dotnet run / 部署
    ▼
运行
```

### 7. xUnit 通过反射工作（伪代码）

```csharp
Assembly dll = Assembly.LoadFrom("TemperatureSensor.Tests.dll");
foreach (Type type in dll.GetTypes())
    foreach (MethodInfo method in type.GetMethods())
        if (method.GetCustomAttribute<FactAttribute>() != null)
        {
            object instance = Activator.CreateInstance(type);
            try { method.Invoke(instance, null); /* PASS */ }
            catch { /* FAIL */ }
        }
```

### 8. 手写 runner 的 4 大痛点 → xUnit 的 4 个解药

| 痛点 | xUnit 的解药 |
|---|---|
| 忘加 Main 调用 → 测试被遗忘 | 自动发现（auto-discovery via reflection） |
| 50 PASS 里混 1 FAIL 看不见 | 结构化汇总报告 `total/passed/failed/skipped` |
| CI 不知道有没有挂 | 失败 → 非零退出码（exit code）|
| 想跑单个测试做不到 | `dotnet test --filter "Name~Below"` |

## 关键代码模板

### 类库（Core）—— 产品代码示例

```csharp
namespace TemperatureSensor.Core;

public sealed class ThresholdExceededEventArgs : EventArgs
{
    public double Reading { get; }
    public double Threshold { get; }
    public DateTime Timestamp { get; }

    public ThresholdExceededEventArgs(double reading, double threshold, DateTime timestamp)
    {
        Reading = reading;
        Threshold = threshold;
        Timestamp = timestamp;
    }
}

public class TemperatureSensor
{
    private readonly double _threshold;
    public TemperatureSensor(double threshold) => _threshold = threshold;

    public event EventHandler<ThresholdExceededEventArgs>? ThresholdExceeded;

    public void RecordReading(double celsius)
    {
        if (celsius > _threshold)
            OnThresholdExceeded(new ThresholdExceededEventArgs(celsius, _threshold, DateTime.Now));
    }

    protected virtual void OnThresholdExceeded(ThresholdExceededEventArgs e)
        => ThresholdExceeded?.Invoke(this, e);
}
```

### 测试（Tests）—— xUnit 模板

```csharp
using TemperatureSensor.Core;
namespace TemperatureSensor.Tests;

public class TemperatureSensorTests
{
    [Fact]
    public void AboveThreshold_RaisesEvent()
    {
        // Arrange
        var sensor = new TemperatureSensor.Core.TemperatureSensor(threshold: 100.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (s, e) => eventFired = true;

        // Act
        sensor.RecordReading(101);

        // Assert
        Assert.True(eventFired, "ThresholdExceeded should fire when reading > threshold");
    }

    [Fact]
    public void AtOrBelowThreshold_DoesNotRaiseEvent()
    {
        var sensor = new TemperatureSensor.Core.TemperatureSensor(threshold: 100.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (s, e) => eventFired = true;

        sensor.RecordReading(99);

        Assert.False(eventFired, "ThresholdExceeded should not fire when reading <= threshold");
    }
}
```

### Tests.csproj —— 完整结构

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>   <!-- 不打成 NuGet 包 -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="..." />
    <PackageReference Include="xunit" Version="..." />
    <PackageReference Include="xunit.runner.visualstudio" Version="..." />
    <PackageReference Include="coverlet.collector" Version="..." />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />   <!-- global using Xunit; -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TemperatureSensor.Core\TemperatureSensor.Core.csproj" />
  </ItemGroup>
</Project>
```

## 命令行参考（从零建一个 xUnit 测试项目）

```powershell
# 1. 建工作目录
mkdir MyProject; cd MyProject

# 2. 建 solution
dotnet new sln -n MyProject

# 3. 建产品代码项目（类库）
dotnet new classlib -n MyProject.Core

# 4. 建测试项目（xUnit 模板）
dotnet new xunit -n MyProject.Tests

# 5. 把两个项目加进 solution
dotnet sln MyProject.slnx add MyProject.Core/MyProject.Core.csproj `
                              MyProject.Tests/MyProject.Tests.csproj

# 6. 让 Tests 引用 Core
dotnet add MyProject.Tests/MyProject.Tests.csproj `
       reference MyProject.Core/MyProject.Core.csproj

# 7. 跑测试
dotnet test

# 8. 只跑特定测试
dotnet test --filter "Name~Below"

# 9. 详细输出
dotnet test --logger "console;verbosity=detailed"
```

## 反复踩 / 反复修正的细节

1. **`Console.WriteLine` 不属于产品代码** —— 产品代码应该是纯逻辑（pure logic），不决定 IO。
2. **`Test_` 前缀是手写时代的产物**，xUnit 里 `[Fact]` 已经标明身份，不要再加 `Test_`。
3. **`Assert.True(!x)` 不 idiomatic**，应该用 `Assert.False(x)`。
4. **`static void` 测试方法不 idiomatic**，xUnit 强制每测试一实例就是为了隔离。
5. **方法名 "DoesNotRaiseEvent" 不是 "DoesNotRaisesEvent"** —— `does` 已经做了第三人称单数，主动词用原形。
6. **边界值要看清楚** —— 条件是 `>` 还是 `>=`，决定 100 是不是临界 PASS。
7. **类库不能独立运行** —— 必须被 host 项目消费。
8. **引用是给编译器的，反射才是 xUnit 用的**。
9. **NuGet 包是分发格式**，运行时只看 `bin/{Config}/{TFM}/` 里的 dll。
10. **每个 Assert 加 failure message**，未来的 oncall 会感谢你。

## 复习题（明天 quiz me 用，按主题分）

### A. 单元测试核心概念

1. 单元测试和手动验证（人眼看 Console）有什么本质区别？
2. AAA 模式三段分别是什么？每一段干嘛？
3. 测试事件（void 方法 + 副作用）时为什么需要"标志变量 / 测试间谍"？写出基本套路。
4. lambda 闭包（closure）在测试里起的关键作用是什么？
5. "红→绿循环"是什么？为什么写完一个 PASS 的测试还要故意让它 FAIL 一次？
6. 正向测试和负向测试分别用什么 Assert？举一个温度传感器的例子。
7. `Assert.True(x, "msg")` 第二个参数有什么用？什么时候看到它？

### B. xUnit 框架

8. `[Fact]` 是什么？xUnit 为什么用它而不是让你在 Main 里手动注册？
9. xUnit 怎么"知道"哪些方法是测试？（关键词：反射 / metadata）
10. xUnit 测试方法为什么是 `public void`？为什么不建议写 `static`？
11. 解释一下"每个测试一个新实例（per-test instance）"的设计目的。
12. `Assert.True(!x)` 和 `Assert.False(x)` 哪个更好？为什么？
13. 列出至少 5 个 `Assert` 方法和它们的用途。
14. 测试命名约定推荐什么？为什么不加 `Test_` 前缀？

### C. 项目 / 解决方案

15. solution（`.slnx`）/ project（`.csproj`）/ project reference 三者关系？
16. 类库项目和测试项目共同点是什么？不同点是什么？
17. 为什么测试项目要单独？说出至少两个理由。
18. csproj 里 `<ProjectReference Include="..." />` 是什么意思？
19. 在测试代码里 `new TemperatureSensor(...)` 是远程调用吗？为什么？
20. 镜像目录结构（mirror structure）是什么？为什么这样组织？

### D. 编译时 vs 运行时（.NET 底座）

21. C# 编译产物 IL 是什么？跟最终 CPU 指令是什么关系？
22. dotnet test 跑起来的 5 个阶段（restore / build Core / build Tests / discover / execute）分别在干什么？
23. 什么是"反射（reflection）"？xUnit 为什么必须用它？
24. 引用关系（references）是给谁用的？运行器为什么不能用引用？
25. 什么是元数据（metadata）？跟反射、attribute 是什么关系？
26. 什么是 attribute（特性）？举一个 `[Fact]` 之外的例子。

### E. NuGet 和部署

27. `.nupkg` 和 `.dll` 的区别？
28. restore / build / publish 各自做什么？
29. `bin/Debug/net10.0/` 里为什么有那么多 dll？它们是构建时复制还是运行时下载？
30. 如果只把 `Tests.dll` 单独放一个空文件夹，xUnit 能发现测试吗？能跑测试吗？为什么？
31. 部署到生产时，部署单元是什么？

### F. 回归 + 生产架构

32. 什么是"回归（regression）"？回归测试是什么？
33. 今天把 `>` 改成 `<` 时，两个测试都 FAIL —— 这说明测试的核心价值是什么？
34. 如果今天把 `>` 改成 `>=`，今天写的两个测试还能抓住这个 bug 吗？（边界覆盖缺口）
35. 类库（library）项目和入口（host）项目本质区别？
36. 列出 3 种可能的 host 项目类型来消费 `TemperatureSensor.Core`。
37. 为什么产品代码不应该有 `Console.WriteLine`？关注点分离怎么体现？
38. 生产环境怎么"调试"？跟开发期断点调试有什么不同？（关键词：可观测性、三大支柱）

---

# Part 2（下午延伸）— 参数化测试 + payload 验证 + 调试工作流

下午主线：发现"边界覆盖缺口"（昨天的 `>` vs `>=` 没人守门）→ 学 `[Theory]` + `[InlineData]` 把 3 个等价类用 1 个方法覆盖 → 做第一次回归（改 `>=`）→ 发现"测试盲点"（payload 没断言）→ 用可空引用 + lambda 闭包扣留 EventArgs → 做第二次回归（改 EventArgs 数据）→ 精读 stack trace，建立"测试只报症状不报病因"的调试心智模型。

## 下午完成的代码

- `TemperatureSensor.Tests/TemperatureSensorTests.cs`
  - 升级 `[Fact] AboveThreshold_RaisesEvent`：加 `captured` 对象 spy + 3 个 payload 断言
  - 新增 `[Theory] RecordReading_RaisesEventOnlyWhenAboveThreshold`：3 组 `[InlineData]` 覆盖 above/boundary/below
  - 总用例数：2 Fact + 3 Theory = **5 个测试用例**全过

## 核心概念清单（继续）

### G. 参数化测试（Parameterized Tests）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| Fact vs Theory | Fact vs Theory | Fact = "一个事实"；Theory = "对所有输入都成立的定理" |
| 内联数据 | `[InlineData(...)]` | 直接在源码里写测试数据 |
| 编译时常量约束 | compile-time constant constraint | `[InlineData]` 只接受 `int / double / string / bool / 枚举` 等基本类型常量 |
| 等价类 | equivalence class | 一组"行为相同"的输入，挑一个代表测就行 |
| 边界值 | boundary value | 条件改变的临界输入（如 `>` 100 时的 100） |
| 覆盖广度 | coverage breadth | 测了多少种**输入**等价类 |
| 覆盖深度 | coverage depth | 每次断言多少个**输出**维度 |
| 参数化失败标识 | per-row failure identification | xUnit 自动把失败那行的 `[InlineData]` 实参打出来 |

### H. 可空引用 + 对象间谍（Nullable Refs + Object Spy）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 可空引用类型 | nullable reference type (`T?`) | 类型后加 `?` 显式声明"可以为 null" |
| 空值流分析 | null-flow analysis | `Assert.NotNull(x)` 后，编译器认为 x 非空 |
| null 容忍运算符 | null-forgiving operator (`!`) | "我确信不为 null"，绕过流分析 |
| 启用可空 | `<Nullable>enable</Nullable>` | csproj 配置，强制所有引用类型声明可空性 |
| 对象间谍 | object spy | 用 lambda 闭包扣留事件参数对象本身 |
| 事件载荷 | event payload / args | 事件触发时携带的数据对象 |

### I. 失败诊断（Diagnostic Workflow）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 一失败就停 | fail-fast | 一个测试方法里任一 Assert 失败，剩余 Assert 不执行 |
| 测试盲点 | test blind spot | 没显式断言 = 没覆盖；改这个字段没人抓 |
| 伪绿 | false green | 测试 PASS 但产品仍有 bug |
| 症状 vs 病因 | symptom vs cause | 测试报告告诉你 WHAT 错（哪个值）；不告诉你 WHERE 错（哪行代码） |
| 反向追踪 | reverse trace | 从失败信息推到产品代码 bug 位置 |
| Stack trace 例外 | thrown-exception trace | 当产品代码抛异常时，stack trace **会**直接指向产品代码行 |
| 断言参数顺序铁律 | assert arg ordering | `Assert.Equal(expected, actual)` 顺序不可交换 |
| 测试 ↔ 生产对应 | testing/production parallel | 测试失败信息 ↔ 生产日志；stack trace ↔ exception traceback |

## 关键心智模型（继续）

### 9. 三个等价类（boundary value model）

```text
                  阈值（threshold = 100）
                        │
   ───────●─────────────●─────────●────────►  celsius
        50            100        150
      下方代表       临界值      上方代表
    （below）     （boundary）  （above）

    > 时：    不触发      不触发      触发
    >= 时：   不触发      触发        触发
                          ↑
              这里是唯一能区分 > 和 >= 的边界
              昨天的测试没测它 → 边界覆盖缺口
```

### 10. lambda 闭包扣留对象（object spy）

```text
[ Outer scope ]
    bool eventFired = false;
    ThresholdExceededEventArgs? captured = null;     ← 声明在 lambda 外面

    sensor.ThresholdExceeded += (sender, e) => {
       │   eventFired = true;        ← lambda 修改外部 bool
       │   captured = e;             ← lambda 把 e "扣留" 到外部对象
       └──────────────────────────   这就是闭包（closure）
    };

    // Assert
    Assert.NotNull(captured);                          ← 之后还能用！
    Assert.Equal(101, captured!.Reading);              ← 可以查任何字段
    Assert.Equal(100, captured.Threshold);
```

### 11. fail-fast 模型

```text
[Fact] Test_Method:
    Assert 1   ✅ PASS
    Assert 2   ✅ PASS
    Assert 3   ❌ FAIL  ←─── 整个方法在这里停（抛 AssertException）
    Assert 4   ⚫ 从未执行
    Assert 5   ⚫ 从未执行

⇒ 测试一次只能报告"它遇到的第一个不一致"
⇒ 修了第一个 FAIL，可能还有更多隐藏的 FAIL 在后面排队
```

### 12. 测试报告 → bug 反向追踪

```text
            产品代码（已退出）                Assert 在此处检测
                  │                                │
   sensor.RecordReading(101)                       │
        │                                          │
        └→ new EventArgs(_threshold, celsius, ...) ← 真正的 bug 行
                            │
                            └→ lambda 存 captured = e
                                              │
                                              ▼
                                      Assert.Equal(101, captured.Reading)
                                              │
                                              ▼
                                      Stack Trace 只能看到：
                                        testfile:line 35   ← 在这里
                                        Assert.Equal 内部
                                      ❌ 看不到产品代码 bug 行

⇒ 必须靠"失败信息（Expected 101 / Actual 100）"反向推理到产品代码
⇒ 测试只报"症状"，定位"病因"永远是开发者的工作
```

### 13. 测试 vs 生产环境的等价表

```text
测试环境                       生产环境
─────────────────────         ─────────────────────
Assert.Equal 失败信息      ←→  日志条目（log entry）
Stack Trace                ←→  exception traceback
dotnet test --filter       ←→  Kusto / AppInsights 查询
回看产品代码               ←→  回看产品代码 + git blame
```

## 关键代码模板（继续）

### `[Theory]` + `[InlineData]` —— 参数化测试

```csharp
[Theory]
[InlineData(150.0, true)]     // above   ─┐
[InlineData(100.0, false)]    // boundary─┼─ 3 个等价类各 1 个代表
[InlineData(50.0,  false)]    // below   ─┘
public void RecordReading_RaisesEventOnlyWhenAboveThreshold(
    double celsius, bool shouldRaise)
{
    // Arrange
    var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
    bool eventFired = false;
    sensor.ThresholdExceeded += (sender, e) => eventFired = true;

    // Act
    sensor.RecordReading(celsius);

    // Assert
    Assert.Equal(shouldRaise, eventFired);
}
```

### 对象间谍 + payload 断言

```csharp
[Fact]
public void AboveThreshold_RaisesEventWithCorrectPayload()
{
    // Arrange
    var sensor = new TemperatureSensor.Core.TemperatureSensor(threshold: 100.0);
    bool eventFired = false;
    ThresholdExceededEventArgs? captured = null;          // ← 可空声明
    sensor.ThresholdExceeded += (sender, e) =>
    {
        eventFired = true;
        captured = e;                                      // ← 扣留事件参数对象
    };

    // Act
    sensor.RecordReading(101);

    // Assert
    Assert.True(eventFired);
    Assert.NotNull(captured);
    Assert.Equal(101, captured!.Reading);                  // ! = null-forgiving
    Assert.Equal(100, captured.Threshold);
}
```

## 复习题（继续，按主题分）

### G. 参数化测试

39. `[Fact]` 和 `[Theory]` 的区别？什么时候用哪个？
40. `[InlineData]` 的参数能放 `new DateTime(...)` 吗？为什么？
41. 一个 `[Theory]` 方法配 3 个 `[InlineData]`，xUnit 跑出来报 Total: ? 个测试用例？
42. 等价类（equivalence class）和边界值（boundary value）的区别？
43. 用 `>` 作为条件时，至少要测哪 3 个等价类？各挑什么代表数据？
44. xUnit 的失败信息怎么告诉你"是哪一组 `[InlineData]` 失败"？
45. 把 3 个 `[Fact]` 合并成 1 个 `[Theory]` 后，代码节省了多少？带来什么好处？

### H. 可空引用 + 对象间谍

46. `ThresholdExceededEventArgs? captured = null;` 的 `?` 是什么意思？
47. 为什么 `var captured = null;` 不能编译过？
48. csproj 里 `<Nullable>enable</Nullable>` 开启了什么机制？
49. lambda 怎么把事件参数对象"扣留"到外部变量？写一个最小例子。
50. `Assert.NotNull(captured)` 后接 `captured.Reading` 为什么不需要 `!`？
51. `!` 这个 null-forgiving 运算符做了什么？滥用它会有什么风险？

### I. 失败诊断 / 调试工作流

52. xUnit 的 fail-fast 是什么？方法里 5 个 Assert，第 3 个 FAIL，后两个会跑吗？
53. 测试报告的 Stack Trace 为什么通常只指向测试代码，不指向产品代码？
54. 什么情况下 Stack Trace **会**直接指向产品代码？
55. "测试盲点（test blind spot）"是什么？给一个温度传感器的例子。
56. `Assert.Equal(expected, actual)` 参数为什么不能交换顺序？交换后会出什么问题？
57. 测试只告诉你 WHAT 错、不告诉你 WHERE 错 —— 这句话对你的调试工作流有什么含义？
58. 测试调试 vs 生产环境调试，调试工具上最大的差异是什么？
59. "覆盖广度（coverage breadth）"和"覆盖深度（coverage depth）"分别是什么？给一个例子说明今天的测试在哪个维度上是充分的、哪个不够。

---

# Part 3（下午延伸 II）— 异常测试 + 数据源进阶 + 项目结构 + C# 表达式语法

下午第二段主线：把"payload 全验证"做成 `[Theory]`（B1）→ 撞上 xUnit 不允许测试方法重载的规则 → 学到 Roslyn 分析器概念 → 给产品代码加 `NaN/Infinity` 防御（B2）→ 学 `Assert.Throws<T>` + `nameof` + 泛型语法 + `ArgumentException` 体系 → 把 `[InlineData]` 抽到 `[MemberData]`（B3）→ 顺带把 `=>` 的双重含义和"属性 vs 字段 vs 方法"三种成员形式彻底搞清。

## 下午第二段完成的代码

- `TemperatureSensor.Core/TemperatureSensor.cs`
  - `RecordReading` 加 `NaN` / `Infinity` 输入校验 → 抛 `ArgumentException(message, paramName)`
- `TemperatureSensor.Tests/TemperatureSensorTests.cs`
  - 新增 `[Theory] RecordReading_FullVerification` —— 3 行 `[InlineData]` 携带 `double?` 可选预期 + `if/else` 分支断言
  - 把 `RecordReading_RaisesEventOnlyWhenAboveThreshold` 从 `[InlineData]` 升级为 `[MemberData]`，挂到静态属性 `ThresholdTestData`（8 行数据，含 100.001 / 99.999 浮点边界微移）
  - 新增 3 个异常测试：`NaN_ThrowsArgumentException` / `PositiveInfinity_ThrowsArgumentException` / `NegativeInfinity_ThrowsArgumentException`
- **最终测试数：16 个全过**

## 核心概念清单（继续）

### J. 测试方法重载 + Roslyn 分析器

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 方法重载 | method overloading | 同一作用域允许同名方法只要参数列表不同（产品代码合法） |
| xUnit 不允许测试重载 | xUnit1024 rule | 测试方法名必须唯一，否则编译报错 |
| Roslyn 分析器 | Roslyn analyzer | 编译时插件，扫源码报告问题（warning / error） |
| 分析器规则 ID | analyzer rule ID | 如 `xUnit1024` `CA1822` `IDE0001`，规则唯一编号 |
| 抑制规则 | suppress rule | `#pragma warning disable xUnit1024` 局部关掉规则 |
| 严重程度 | severity | warning / error / info / hidden，可通过 csproj 调整 |
| 失败标识来源 | test ID source | CI 用方法的完全限定名（FQN）作唯一 ID，所以测试名必须唯一 |

### K. 异常测试（Exception Testing）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| 防御性编程 | defensive programming | 入口检查输入合法性，立刻抛异常，避免后续静默错误 |
| 快速失败 | fail-fast | 一发现问题就抛，让 bug 在源头被发现 |
| 静默失败 | silent failure | 不检查输入直接计算，bug 跑很远才暴露 |
| `Assert.Throws<T>` | — | 期望 lambda 抛出**精确类型 T**（不接受子类） |
| `Assert.ThrowsAny<T>` | — | 期望 lambda 抛出 T 或其**子类** |
| 异常类型层级 | exception hierarchy | `Exception → SystemException → ArgumentException → ArgumentNullException` 等 |
| `ArgumentException` | — | 参数不合法（最通用） |
| `ArgumentNullException` | — | 参数是 `null` |
| `ArgumentOutOfRangeException` | — | 参数超出允许范围 |
| `nameof` 操作符 | nameof operator | 编译时把符号名转成字符串，重构友好 |
| 异常构造签名 | `new ArgumentException(message, paramName)` | 第二个参数是出问题的参数名 |
| 异常捕获模式 | exception capture pattern | `var ex = Assert.Throws<...>(...); Assert.Contains("...", ex.Message);` |
| 泛型语法 | generic type parameter `<T>` | `Assert.Throws<T>` 的 `T` 是编译时已知的类型实参 |
| `double.IsNaN` | — | 判断是不是"非数字"（如 `0.0 / 0.0`） |
| `double.IsInfinity` | — | 判断是不是无穷大（`PositiveInfinity` / `NegativeInfinity`） |

### L. 数据源进阶（Advanced Data Sources）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| `[InlineData]` | inline data | 编译时常量在源码里直接写，1-5 行最适合 |
| `[MemberData]` | member data | 引用一个静态成员（属性/字段/方法），跨多行复用 |
| `[ClassData]` | class data | 引用一个实现 `IEnumerable<object[]>` 的独立类（跨测试类复用） |
| `IEnumerable<object[]>` | — | 数据源的"形状"：可枚举的对象数组序列，每行一个 `object[]` |
| `nameof(...)` 引用 | — | `[MemberData(nameof(MyData))]` 用 `nameof` 避免硬编码字符串 |
| 表达式体成员 | expression-bodied member | 用 `=>` 替代 `{ return ...; }` 的简写，C# 6 引入 |
| 属性 vs 字段 vs 方法 | property vs field vs method | 三种成员都能挂 `[MemberData]`，xUnit 自动识别 |
| `TheoryData<T1, T2, ...>` | — | 强类型替代 `IEnumerable<object[]>`，编译期就校验类型 |
| 静态成员 | static member | `[MemberData]` 要求引用的成员是 `public static` |
| 延迟执行 | deferred execution | 属性形式（`=>`）每次访问都重新执行右边的表达式 |

### M. 真实项目结构（Real-World Layout）

| 概念 | 中文 / English | 关键点 |
|---|---|---|
| src / tests 分离 | src/tests separation | 顶层两个文件夹：`src/` 放产品代码，`tests/` 放测试 |
| 1:1 镜像 | 1:1 mirror mapping | `src/Foo.Bar/Baz.cs` ↔ `tests/Foo.Bar.Tests/BazTests.cs` |
| 分层架构 | Clean Architecture | Domain → Application → Infrastructure → Presentation |
| Domain 层 | domain layer | 纯业务逻辑，零外部依赖 |
| Application 层 | application layer | 用例编排，调用 domain + 抽象出 infra 接口 |
| Infrastructure 层 | infrastructure layer | DB、HTTP client、消息队列、文件 IO 的实现 |
| Presentation 层 | presentation layer | Web API / CLI / UI 入口 |
| 5-15 个项目 | mid-size solution | 中型生产项目典型规模 |
| 测试金字塔 | testing pyramid | 单元测试多 / 集成测试中 / E2E 少 |

## 关键心智模型（继续）

### 14. xUnit1024 —— 为什么测试不能重载

```text
产品代码（合法）                  测试代码（被 Roslyn 分析器拦下）
═══════════════                    ═══════════════════════════════
class Calculator                   class CalculatorTests
{                                  {
  Add(int a, int b)                  [Theory]
  Add(double a, double b)            void Add_WorksCorrectly(int a, int b)   ❌
  Add(string a, string b)            [Theory]                                xUnit1024
}                                    void Add_WorksCorrectly(double a, ...) ❌

⇒ C# 编译器：OK，这就是方法重载，靠参数类型区分
⇒ xUnit 分析器：不行！CI 用"方法名"作测试唯一 ID
                  重名 → CI 报告里两条结果会撞车
```

⚠️ 关键认识：这**不是 C# 编译器报的错**，是 **xUnit 自己的 Roslyn 分析器**报的。`xUnit1024` 是它的规则号。

### 15. `=>` 的双重身份

```text
=> 出现在【值的位置】              ⇒  Lambda 表达式
   Func<int,int> f = x => x*2;            "输入 → 输出"的匿名方法

=> 出现在【成员声明的位置】        ⇒  表达式体成员
   public int Square(int x) => x*x;       替代 { return x*x; }
   public int Y => 42;                    属性的 get-only 简写
```

⚠️ 长得一样，意思完全不同。看出现在等号右边还是成员定义里来区分。

### 16. `[MemberData]` 三种成员形式

```text
形式 1：属性 + 表达式体（推荐 ⭐⭐⭐⭐⭐）
─────────────────────────────────────────
public static IEnumerable<object[]> Data => new[] { ... };
                                         ↑↑ 每次访问 Data 都重新执行右边

形式 2：字段 + 初始化（很少用）
─────────────────────────────────────────
public static IEnumerable<object[]> Data = new[] { ... };
                                         ↑ 类加载时执行一次，之后不变

形式 3：方法（需要参数或复杂生成时）
─────────────────────────────────────────
public static IEnumerable<object[]> Data() => new[] { ... };
                                          ↑↑↑ 多了一对括号 = 方法
                                              每次调用都重新执行
```

**判断口诀**：
1. 看名字后面有没有 `()` —— 有 → 方法；没有 → 看下一步
2. 看赋值符号 —— `=>` → 属性（表达式体）；`=` → 字段

### 17. 异常类型继承层级（最常见的一支）

```text
Exception                          所有异常的根
 └─ SystemException                CLR 抛的运行时异常
     └─ ArgumentException          参数不合法（通用）
         ├─ ArgumentNullException        参数是 null
         └─ ArgumentOutOfRangeException  参数超范围
```

- `Assert.Throws<ArgumentException>` —— **精确**类型，子类不算
- `Assert.ThrowsAny<ArgumentException>` —— 子类也算

如果产品代码抛的是 `ArgumentNullException`，那么：
- `Assert.Throws<ArgumentException>` ❌ FAIL（不是精确匹配）
- `Assert.Throws<ArgumentNullException>` ✅ PASS
- `Assert.ThrowsAny<ArgumentException>` ✅ PASS（接受子类）

### 18. 防御性编程的"快速失败"模型

```text
没有防御                         有防御（快速失败）
────────                         ────────────────
public void Foo(double x)        public void Foo(double x)
{                                {
   y = x / something;              if (double.IsNaN(x))
   // bug 跑 100 行后才暴露            throw new ArgumentException(
}                                       "Must be finite.",
                                        nameof(x));
                                   y = x / something;
                                 }

静默失败 —— bug 跑很远           快速失败 —— bug 在源头被发现
诊断困难                          诊断容易，调用方立刻知道错在哪
```

### 19. 真实项目结构（生产分层）

```text
MyProduct/
├── MyProduct.sln                      ← 整个解决方案
├── src/
│   ├── MyProduct.Domain/              ← 纯业务逻辑，零依赖
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Services/
│   ├── MyProduct.Application/         ← 用例编排，依赖 Domain
│   │   ├── UseCases/
│   │   └── Interfaces/
│   ├── MyProduct.Infrastructure/      ← DB / HTTP / 消息实现
│   │   ├── Persistence/
│   │   └── HttpClients/
│   └── MyProduct.WebApi/              ← 入口（Host）
│       └── Program.cs
└── tests/
    ├── MyProduct.Domain.Tests/         ← 1:1 镜像 src 结构
    ├── MyProduct.Application.Tests/
    ├── MyProduct.Infrastructure.Tests/
    └── MyProduct.WebApi.IntegrationTests/
```

**依赖方向铁律**：箭头永远朝内（Domain ← Application ← Infrastructure / WebApi）。Domain 不知道有 DB，DB 实现知道 Domain。

## 关键代码模板（继续）

### 异常测试 —— `Assert.Throws<T>`

```csharp
// 产品代码：防御性输入校验
public void RecordReading(double celsius)
{
    if (double.IsNaN(celsius) || double.IsInfinity(celsius))
    {
        throw new ArgumentException(
            "Reading must be a finite number.",
            nameof(celsius));              // ← 重构友好的参数名
    }
    if (celsius > _threshold)
        OnThresholdExceeded(new ThresholdExceededEventArgs(celsius, _threshold, DateTime.Now));
}

// 测试：3 个 [Fact] 覆盖 NaN / +∞ / -∞
[Fact]
public void RecordReading_NaN_ThrowsArgumentException()
{
    var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
    Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.NaN));
}

// 升级版：同时检查异常信息内容
[Fact]
public void RecordReading_NaN_ThrowsWithCorrectMessage()
{
    var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);

    var ex = Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.NaN));

    Assert.Contains("finite", ex.Message);
    Assert.Equal("celsius", ex.ParamName);
}
```

### `[Theory]` + 可选 payload 验证 —— B1 模式

```csharp
[Theory]
[InlineData(200.0, true, 200.0)]    // 触发 → 期望 Reading=200
[InlineData(100.0, false, null)]    // 不触发 → 没 payload
[InlineData( 50.0, false, null)]    // 不触发 → 没 payload
public void RecordReading_FullVerification(
    double celsius, bool shouldRaise, double? expectedReading)
{
    var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
    bool eventFired = false;
    ThresholdExceededEventArgs? captured = null;

    sensor.ThresholdExceeded += (s, e) => { eventFired = true; captured = e; };
    sensor.RecordReading(celsius);

    if (shouldRaise)
    {
        Assert.NotNull(captured);
        Assert.Equal(expectedReading, captured!.Reading);
        Assert.Equal(100.0, captured.Threshold);
    }
    else
    {
        Assert.False(eventFired);
        Assert.Null(captured);
    }
}
```

### `[MemberData]` —— B3 模式

```csharp
[Theory]
[MemberData(nameof(ThresholdTestData))]            // ← nameof 重构友好
public void RecordReading_RaisesEventOnlyWhenAboveThreshold(
    double celsius, bool shouldRaise)
{
    var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
    bool eventFired = false;
    sensor.ThresholdExceeded += (s, e) => eventFired = true;
    sensor.RecordReading(celsius);
    Assert.Equal(shouldRaise, eventFired);
}

// 静态属性（最常用形式）
public static IEnumerable<object[]> ThresholdTestData =>
    new List<object[]>
    {
        new object[] { 200.0,  true  },
        new object[] { 150.0,  true  },
        new object[] { 100.001, true },     // 浮点边界微移
        new object[] { 100.0,  false },     // 边界
        new object[] { 99.999, false },     // 浮点边界微移
        new object[] { 50.0,   false },
        new object[] { 0.0,    false },
        new object[] { -40.0,  false },     // 负温度
    };
```

### 现代化版本 —— `TheoryData<T1, T2>`（C# 9+，强类型）

```csharp
// 编译期就校验类型 —— 比 IEnumerable<object[]> 安全
public static TheoryData<double, bool> ThresholdTestData => new()
{
    { 200.0,  true  },
    { 100.0,  false },
    {  50.0,  false },
};

[Theory]
[MemberData(nameof(ThresholdTestData))]
public void RecordReading_...(double celsius, bool shouldRaise) { ... }
```

## 反复踩 / 反复修正的细节（继续）

1. **测试方法不能重载** —— xUnit 的 Roslyn 分析器规则 `xUnit1024` 报错，**不是 C# 编译器报的错**。同一个测试类里不允许同名方法。
2. **`Assert.Throws<T>` 是精确类型匹配** —— 子类异常不算。要接受子类用 `Assert.ThrowsAny<T>`。
3. **`Assert.Throws<T>` 的返回值是异常对象** —— 可以接住做进一步检查：`var ex = Assert.Throws<...>(...); Assert.Contains("...", ex.Message);`
4. **`nameof(...)` 不是字符串** —— 是编译时操作符，把符号名提取成字符串。重命名时编译器会一起更新。
5. **`[MemberData]` 引用的成员必须 `public static`** —— 私有不行，实例成员不行。
6. **`new object[] { ... }` 和 `new[] { ... }` 不一样** —— `new object[] { 1, "a" }` 强制成 `object[]`；`new[] { 1, 2 }` 推断成 `int[]`。`[MemberData]` 要 `object[]`。
7. **`=>` 在 Lambda 和表达式体里是两个不同的东西** —— 位置决定身份：值的位置是 Lambda；成员声明的位置是表达式体。
8. **属性形式（`=>`）每次访问都执行右边** —— 字段形式（`=`）只执行一次。`[MemberData]` 一般用属性形式即可。
9. **产品代码可以重载，测试代码不行** —— 因为 CI 报告靠方法名识别测试。
10. **生产项目结构是 src/tests 分离 + 1:1 镜像** —— 一个产品项目对应一个测试项目，不是所有测试堆一个工程里。

## 复习题（继续，按主题分）

### J. 测试方法重载 + Roslyn 分析器

60. 产品代码里 `Add(int, int)` 和 `Add(double, double)` 同名合法。同样的写法搬到 xUnit 测试类里编译失败 —— 谁报的错？规则号是什么？
61. 为什么 xUnit 要禁止测试方法重载？（提示：CI 报告的唯一 ID 来源）
62. 什么是 Roslyn 分析器？它在编译流程的哪一阶段工作？
63. 怎么临时关掉 xUnit1024？（提示：`#pragma`）什么时候应该这样做？什么时候**不应该**？
64. 产品代码可以重载、测试不能重载 —— 这个区别揭示了什么编程模式？（关键词：source of truth）

### K. 异常测试

65. `Assert.Throws<T>` 和 `Assert.ThrowsAny<T>` 区别？写一个 `ArgumentNullException` 例子说明。
66. `Assert.Throws<ArgumentException>(() => x.Foo(null))` 这行的 `<ArgumentException>` 是 C# 的什么语法？这个 `T` 编译时存在还是运行时存在？
67. `nameof(celsius)` 在编译后变成什么？跟硬编码字符串 `"celsius"` 比好处在哪？
68. `Exception → SystemException → ArgumentException → ArgumentNullException` 这条链上，应该抛**哪个**？标准是什么？
69. 防御性编程 vs 静默失败 —— 写一段 5 行的"参数检查 + 抛 ArgumentException"代码。
70. 为什么 `new ArgumentException(message, paramName)` 第二个参数要传 `nameof(...)` 而不是字符串字面量？
71. `Assert.Throws<T>` 返回什么？写出"先 Throws 再 Assert.Contains 消息"的两行模式。
72. 为什么 `double.IsNaN(x)` 比 `x == double.NaN` 安全？（提示：`NaN != NaN`）

### L. 数据源进阶

73. `[InlineData]` / `[MemberData]` / `[ClassData]` 三选一决策依据？各举一个使用场景。
74. `[MemberData(nameof(Data))]` 比 `[MemberData("Data")]` 好在哪？
75. `[MemberData]` 三种成员形式：属性 `=>`、字段 `=`、方法 `() =>`，执行时机分别是？
76. `=>` 出现在 `Func<int,int> f = x => x*2;` 和 `public int X => 42;` 里分别是什么？怎么区分？
77. `IEnumerable<object[]>` 这个"形状"为什么必须是 `object[]` 而不能是 `int[]` 或 `double[]`？
78. 用 `TheoryData<double, bool>` 替代 `IEnumerable<object[]>` 的好处是什么？
79. 写一个静态属性形式的 `[MemberData]` 数据源，3 行数据，参数是 `(int x, string label)`。

### M. 真实项目结构

80. 解释 Clean Architecture 的 4 层（Domain / Application / Infrastructure / Presentation）和依赖方向。
81. 为什么 Domain 层应该零外部依赖？
82. 一个中型 .NET 解决方案有 10 个项目，src 和 tests 一般是几比几？
83. 同一个产品项目下，单元测试 / 集成测试 / E2E 测试在数量上应该是什么关系？（关键词：测试金字塔）

## 下午所有完成路径回顾

| 路径 | 内容 | 测试增量 | 累计 |
|---|---|---|---|
| Part 2 | `[Theory]` + `[InlineData]` + 对象 spy + 回归 cycle 2/3 | +3 | 5 |
| **B1** | `[Theory]` + payload + `double?` + if/else 分支 | +3 | 8 |
| **B2** | `Assert.Throws<T>` × NaN / +∞ / -∞ | +3 | 11 |
| **B3** | `[InlineData]` → `[MemberData]` 升级 + 加 5 个浮点边界 | +5 | 16 |

**最终：`dotnet test` → total: 16, failed: 0, succeeded: 16, exit code 0**。

## 未来话题清单（按优先级，已更新）

1. **DLL / API / SDK 概念** —— 学完这三个能完整理解 .NET 包生态
2. **类库 vs 入口项目 + 可观测性入门** —— 日志（ILogger / Serilog）、Application Insights
3. ~~边界值测试 / 测试覆盖~~ ✅ 今天完成（上午 Part 2）
4. **依赖注入（DI）和服务生命周期** —— `AddSingleton` / `AddScoped` / `AddTransient`、共享 `SemaphoreSlim` 的正确做法
5. **测试 doubles**：mock / stub / fake / spy 的区别，Moq / NSubstitute 入门
6. **TDD（测试驱动开发）入门** —— 红绿重构循环、先写测试再写代码
7. **集成测试（integration test）vs 单元测试**
8. **CI/CD 入门** —— GitHub Actions / Azure DevOps 里跑 dotnet test
9. ~~`[MemberData]` / `[ClassData]`~~ ✅ 今天完成 B3 路径（`[ClassData]` 暂记 todo）
10. ~~`Assert.Throws<T>`~~ ✅ 今天完成 B2 路径
11. ~~`[Theory]` upgrade to also assert payload~~ ✅ 今天完成 B1 路径
12. **`[ClassData]` 深入** —— 跨测试类复用的数据源类（今天只浅讲，未实操）
13. **`TheoryData<T1, T2, ...>` 强类型数据源** —— 替代 `IEnumerable<object[]>` 的现代写法
14. **表达式体成员（expression-bodied member）系统化** —— 用在属性 / 方法 / 索引器 / 构造函数 / 析构函数的全场景
