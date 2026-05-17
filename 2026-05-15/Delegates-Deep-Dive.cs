using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================
// 2026-05-15 学习内容：委托深度理解（Delegates Deep Dive）
// ============================================================
// 你已经会用 Func<> / Action<> / Predicate<>
// 今天回答的核心问题：
//   1. Func<> 到底是什么？ → 它就是一个委托（delegate）
//   2. delegate 关键字做了什么？ → 声明了一种"方法签名的类型"
//   3. 为什么需要自定义委托？ → 当 Func/Action 语义不够清晰时
//   4. 多播委托（multicast delegate）是什么？ → 一个变量调用多个方法
//   5. 委托怎么连接到事件（event）？ → 下一课的桥梁
// ============================================================

namespace DelegatesDeepDive;

// ============================================================
// 第 1 部分：delegate 关键字 — 声明"方法签名的类型"
// ============================================================

// 这一行声明了一个新的类型，名字叫 MathOperation
// 意思：任何 "接收两个 int，返回一个 int" 的方法，都能赋值给它
public delegate int MathOperation(int a, int b);

// 对比 Func<int, int, int>：
//   delegate int MathOperation(int a, int b);   ← 自定义委托
//   Func<int, int, int>                          ← .NET 内置委托
// 
// 它们的签名完全相同：(int, int) → int
// 但 MathOperation 有名字，代码可读性更好
// 在生产代码中，当委托表达领域概念时，自定义委托更清晰

// ============================================================
// 第 2 部分：Func<>/Action<> 的真面目
// ============================================================
// 打开 .NET 源码，Func<T, TResult> 就是这样定义的：
//
//   public delegate TResult Func<in T, out TResult>(T arg);
//
// 所以 Func<int, int> 就是：
//   public delegate int Func(int arg);
//
// 结论：Func<> 本身就是用 delegate 关键字定义的
//       你之前一直在用委托，只是不知道它叫委托

// ============================================================
// 第 3 部分：自定义委托 vs Func<> — 什么时候用哪个？
// ============================================================

// 场景 1：简单数据处理 → 用 Func<> 就够了
//   Func<int, int> transform = n => n * 2;

// 场景 2：领域概念 → 自定义委托更清晰
//   delegate decimal TaxCalculator(decimal subtotal, string region);
//   
//   比 Func<decimal, string, decimal> 清晰得多
//   因为看到 TaxCalculator 就知道这是算税的

public delegate decimal TaxCalculator(decimal subtotal, string region);
public delegate bool    ValidationRule(string input);
public delegate void    Logger(string message);

// ============================================================
// 第 4 部分：多播委托（Multicast Delegate）
// ============================================================
// 核心概念：
//   一个委托变量可以用 += 挂多个方法
//   调用一次，所有挂上去的方法都会按顺序执行
//
// 规则：
//   += 添加方法到调用链
//   -= 从调用链移除方法
//   如果委托有返回值，只能拿到最后一个方法的返回值（前面的丢失）
//   所以多播委托最适合 void 返回值（Action / 自定义 void delegate）

public delegate void Notifier(string message);

// ============================================================
// 第 5 部分：委托作为回调（Callback Pattern）
// ============================================================
// 生产中常见：方法完成某件事后，调用你传进去的委托
// 这就是回调（callback）

public class OrderProcessor
{
    // onSuccess 和 onFailure 都是回调
    public static void ProcessOrder(
        int orderId,
        Action<int> onSuccess,
        Action<int, string> onFailure)
    {
        if (orderId > 0)
        {
            Console.WriteLine($"  [处理] 订单 {orderId} 处理成功");
            onSuccess(orderId);
        }
        else
        {
            Console.WriteLine($"  [处理] 订单 {orderId} 处理失败");
            onFailure(orderId, "无效的订单号");
        }
    }
}

// ============================================================
// 第 6 部分：委托与泛型结合 — 通用过滤器
// ============================================================
//
// ─── 泛型最小理解（Generics Minimum Mental Model） ───
//
// 问题：你想写一个"过滤"功能，能过滤 int，也能过滤 string。
//       不用泛型的话，你要写两份几乎一模一样的代码：
//
//         bool FilterInt(int item)    → 过滤 int
//         bool FilterString(string item) → 过滤 string
//
// 泛型的意思：用一个占位符 T 代替具体类型，等调用时再确定 T 是什么。
//
//         bool Filter<T>(T item)     → T 是占位符
//
// 当你写 Filter<int> 时：
//   编译器把 T 替换成 int → 变成 bool Filter(int item)
//
// 当你写 Filter<string> 时：
//   编译器把 T 替换成 string → 变成 bool Filter(string item)
//
// 所以泛型 = "类型也是参数"
//   普通参数：传值     int n = 5
//   泛型参数：传类型   T = int
//
// ─── 泛型委托拆解 ───
//
// 下面这行：
//   public delegate bool Filter<T>(T item);
//
// 意思拆成中文：
//   定义一个委托类型，名字叫 Filter
//   它有一个类型参数 T（占位符）
//   它接收一个 T 类型的参数 item
//   它返回 bool
//
// 具体使用时：
//   Filter<int>    → delegate bool Filter(int item)    → 判断一个 int
//   Filter<string> → delegate bool Filter(string item) → 判断一个 string
//   Filter<Order>  → delegate bool Filter(Order item)  → 判断一个 Order
//
// ─────────────────────────────────────────────────────────────

// 声明泛型委托
public delegate bool Filter<T>(T item);
//                         ↑    ↑
//                     类型参数  方法参数的类型也是 T

public static class CollectionUtils
{
    // FilterBy 方法本身也有泛型参数 <T>
    // 意思：这个方法能处理任何类型的集合，只要你给我一个对应的过滤规则
    //
    // 调用示例：
    //   FilterBy<int>(numbers, n => n > 5)
    //     → T = int, source = List<int>, rule = Filter<int>
    //
    //   FilterBy<string>(words, w => w.Length > 3)
    //     → T = string, source = List<string>, rule = Filter<string>
    //
    // C# 编译器通常能自动推断 T，所以你可以省略 <int>：
    //   FilterBy(numbers, n => n > 5)   ← 编译器看 numbers 是 List<int>，推断 T = int
    //
    public static List<T> FilterBy<T>(IEnumerable<T> source, Filter<T> rule)
    {
        var result = new List<T>();
        foreach (T item in source)     // item 的类型就是 T
        {
            if (rule(item))            // rule 接收 T，返回 bool
                result.Add(item);
        }
        return result;
    }
}

// ============================================================
// 第 7 部分：委托的本质 — 编译器做了什么
// ============================================================
// 当你写：
//   public delegate int MathOperation(int a, int b);
//
// 编译器自动生成一个类（大致等价于）：
//
//   public sealed class MathOperation : System.MulticastDelegate
//   {
//       public MathOperation(object target, IntPtr method);
//       public int Invoke(int a, int b);
//       public IAsyncResult BeginInvoke(int a, int b, AsyncCallback cb, object state);
//       public int EndInvoke(IAsyncResult result);
//   }
//
// 关键点：
//   1. delegate 关键字 = 编译器帮你生成一个类
//   2. 这个类继承 MulticastDelegate → 所以支持 +=
//   3. 调用 myDelegate(a, b) 实际调用的是 Invoke(a, b)
//   4. 这就是为什么委托是引用类型（reference type），可以为 null

// ============================================================
// 演示主程序
// ============================================================
public class Program
{
    // --- 普通方法，用来赋值给委托 ---
    static int Add(int a, int b) => a + b;
    static int Multiply(int a, int b) => a * b;

    // --- 多播演示用的方法 ---
    static void SendEmail(string msg)   => Console.WriteLine($"  📧 Email:   {msg}");
    static void SendSMS(string msg)     => Console.WriteLine($"  📱 SMS:     {msg}");
    static void WriteLog(string msg)    => Console.WriteLine($"  📝 Log:     {msg}");

    public static void Main()
    {
        // =============================================
        // 1) 自定义委托基本用法
        // =============================================
        Console.WriteLine("=== 1) 自定义委托（Custom Delegate） ===");

        MathOperation op = Add;                 // 赋值方法
        Console.WriteLine($"op(3, 4) = {op(3, 4)}");       // 7

        op = Multiply;                          // 重新赋值
        Console.WriteLine($"op(3, 4) = {op(3, 4)}");       // 12

        op = (a, b) => a - b;                   // 赋值 lambda
        Console.WriteLine($"op(3, 4) = {op(3, 4)}");       // -1

        // =============================================
        // 2) 自定义委托 vs Func<> — 完全等价
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 2) 自定义委托 vs Func<> ===");

        MathOperation     customOp = Add;
        Func<int, int, int> funcOp = Add;       // 同一个方法，两种委托都能接

        Console.WriteLine($"customOp(10, 20) = {customOp(10, 20)}");   // 30
        Console.WriteLine($"funcOp(10, 20)   = {funcOp(10, 20)}");     // 30

        // ⚠️ 但它们是不同类型！不能互相赋值：
        //   customOp = funcOp;   // ❌ 编译错误
        //   funcOp = customOp;   // ❌ 编译错误
        // 即使签名一样，C# 的委托是名义类型（nominal typing）
        // 名字不同 = 类型不同

        // =============================================
        // 3) 多播委托（Multicast）
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 3) 多播委托（Multicast Delegate） ===");

        Notifier notify = SendEmail;
        notify += SendSMS;          // += 添加第二个方法
        notify += WriteLog;         // += 添加第三个方法

        Console.WriteLine("调用 notify(\"订单已发货\")：");
        notify("订单已发货");
        // 输出三行：Email、SMS、Log 按添加顺序执行

        Console.WriteLine();
        Console.WriteLine("移除 SendSMS 后再调用：");
        notify -= SendSMS;          // -= 移除一个方法
        notify("库存不足");
        // 输出两行：只有 Email 和 Log

        // =============================================
        // 4) 多播委托 + 返回值的陷阱
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 4) 多播返回值陷阱 ===");

        MathOperation multi = Add;
        multi += Multiply;

        int result = multi(3, 4);
        Console.WriteLine($"multi(3, 4) = {result}");
        // result = 12（只拿到 Multiply 的返回值）
        // Add 的返回值 7 被丢弃了！
        // 教训：多播委托最好用 void 返回值

        // 如果确实需要每个方法的返回值：
        Console.WriteLine("逐个调用拿所有返回值：");
        foreach (Delegate d in multi.GetInvocationList())
        {
            int r = ((MathOperation)d)(3, 4);
            Console.WriteLine($"  {d.Method.Name}(3, 4) = {r}");
        }

        // =============================================
        // 5) 回调模式（Callback Pattern）
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 5) 回调模式（Callback Pattern） ===");

        OrderProcessor.ProcessOrder(
            42,
            id => Console.WriteLine($"  ✅ 成功回调：订单 {id} 已确认"),
            (id, err) => Console.WriteLine($"  ❌ 失败回调：订单 {id}，原因：{err}")
        );

        OrderProcessor.ProcessOrder(
            -1,
            id => Console.WriteLine($"  ✅ 成功回调：订单 {id} 已确认"),
            (id, err) => Console.WriteLine($"  ❌ 失败回调：订单 {id}，原因：{err}")
        );

        // =============================================
        // 6) 泛型委托 Filter<T>
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 6) 泛型委托 Filter<T> ===");

        var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Filter<int> 就是 delegate bool Filter(int item)
        // 意思：一个判断 int 的规则
        Filter<int> isEven = n => n % 2 == 0;       // n 是 int，返回 bool
        Filter<int> greaterThan5 = n => n > 5;      // n 是 int，返回 bool

        // FilterBy 的 T 被推断为 int（因为 numbers 是 List<int>）
        var evenNumbers = CollectionUtils.FilterBy(numbers, isEven);
        var bigNumbers = CollectionUtils.FilterBy(numbers, greaterThan5);

        Console.WriteLine($"偶数：{string.Join(", ", evenNumbers)}");
        Console.WriteLine($">5：  {string.Join(", ", bigNumbers)}");

        // 同一个 FilterBy 方法，换成 string 也能用
        // 这就是泛型的威力：一份代码，多种类型
        var words = new List<string> { "apple", "hi", "elephant", "go", "international" };

        // Filter<string> 就是 delegate bool Filter(string item)
        // 意思：一个判断 string 的规则
        Filter<string> longerThan3 = w => w.Length > 3;   // w 是 string，返回 bool

        // FilterBy 的 T 被推断为 string（因为 words 是 List<string>）
        var longWords = CollectionUtils.FilterBy(words, longerThan3);
        Console.WriteLine($"长单词：{string.Join(", ", longWords)}");

        // =============================================
        // 7) 委托 null 检查 — 防御性编程
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 7) 委托 null 检查 ===");

        Notifier? maybeNull = null;

        // 方式 1：?.Invoke() — 推荐
        maybeNull?.Invoke("测试");
        Console.WriteLine("maybeNull?.Invoke() — 安全，不会崩溃");

        // 方式 2：先检查 null
        if (maybeNull != null)
        {
            maybeNull("测试");
        }
        Console.WriteLine("if (maybeNull != null) — 也安全");

        // 方式 3：直接调用 — 会 NullReferenceException！
        // maybeNull("测试");   // ❌ 崩溃！

        // =============================================
        // 8) 总结对比表
        // =============================================
        Console.WriteLine();
        Console.WriteLine("=== 8) 总结对比表 ===");
        Console.WriteLine("┌──────────────────┬──────────────────────────┐");
        Console.WriteLine("│ 概念              │ 说明                      │");
        Console.WriteLine("├──────────────────┼──────────────────────────┤");
        Console.WriteLine("│ delegate 关键字   │ 声明一种方法签名的类型     │");
        Console.WriteLine("│ Func<>            │ .NET 内置的有返回值委托   │");
        Console.WriteLine("│ Action<>          │ .NET 内置的 void 委托     │");
        Console.WriteLine("│ Predicate<>       │ .NET 内置的 bool 委托     │");
        Console.WriteLine("│ 自定义委托        │ 领域语义更清晰             │");
        Console.WriteLine("│ 多播（+=/-=）      │ 一个变量挂多个方法         │");
        Console.WriteLine("│ 回调模式          │ 把"完成后做什么"传进去     │");
        Console.WriteLine("│ ?.Invoke()        │ null 安全调用              │");
        Console.WriteLine("└──────────────────┴──────────────────────────┘");

        Console.WriteLine();
        Console.WriteLine("Done.");
    }
}

// ============================================================
// 复习题（明天自测）
// ============================================================
//
// Q1. delegate int MathOperation(int a, int b); 这行做了什么？
//     提示：编译器生成了什么？
//
// Q2. Func<int, int, int> 和 MathOperation 签名一样，能互相赋值吗？为什么？
//     提示：名义类型（nominal typing）
//
// Q3. 多播委托调用有返回值的方法时，返回值是哪个？
//     提示：只有最后一个
//
// Q4. 怎样安全调用一个可能为 null 的委托？
//     提示：?.Invoke()
//
// Q5. 什么时候用自定义委托而不是 Func<>？
//     提示：领域概念需要名字表达意图
//
// Q6. notify += SendSMS; notify -= SendSMS; 各做了什么？
//     提示：添加/移除调用链中的方法
//
// Q7. 委托是值类型还是引用类型？为什么？
//     提示：继承自 MulticastDelegate，是 class
