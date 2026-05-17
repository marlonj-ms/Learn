using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================
// 2026-05-14 学习内容：扩展方法 + Lambda 深度理解
// ============================================================
// 核心收获：
//   1. 扩展方法三铁律：static class + static method + this 第一参数
//   2. this 是语法糖：n.IsEven() ≡ MyExtensions.IsEven(n)
//   3. 编译器靠 using + this 参数类型查找，跟 class 名字无关
//   4. 任何类型都能扩展：基础类型、引用类型、接口、泛型、委托
//   5. lambda 是"没名字的方法"，是一个值，可以被赋值/传递/return
//   6. 方法可以 return lambda（函数工厂思想）
//   7. lambda 参数 = 形参，调用时才拿到具体值
//   8. lambda 可以捕获外层变量（闭包 closure）
//   9. 多个 lambda 可以串成 pipeline（高阶函数组合）
// ============================================================

namespace ExtensionMethodsDemo;

// -----------------------------------------------------------
// 第 1 部分：扩展方法 — 三条铁律
// -----------------------------------------------------------
public static class MyExtensions
{
    // ✅ 规则：① 类必须 static  ② 方法必须 static  ③ 第一参数加 this

    // (1) 扩展 int
    public static bool IsEven(this int n) => n % 2 == 0;

    // (2) 扩展 string —— 数单词数
    public static int WordCount(this string sentence) =>
        sentence.Split(' ').Length;

    // (3) 扩展 IEnumerable<int> —— 自己实现 Average
    public static int Average2(this IEnumerable<int> nums)
    {
        int sum = 0;
        int count = 0;
        foreach (int x in nums)
        {
            sum += x;
            count++;
        }
        return sum / count;
    }

    // (4) 扩展 int —— 带额外参数
    //     调用：m.AddBy(3)  →  AddBy(m, 3)
    //     this 那个参数由"点前面的对象"自动传，后面参数显式传
    public static int AddBy(this int age, int y) => age + y;

    // (5) 扩展委托 Func<int, bool> —— 描述判断结果
    //     证明：扩展方法不仅能扩展 int/string，也能扩展委托类型
    public static string Describe(this Func<int, bool> predicate)
    {
        bool resultOn5 = predicate(5);
        return $"对 5 的判断结果是：{resultOn5}";
    }

    // (6) 扩展委托 Func<int, int> —— 函数组合 AndThen
    //     a.AndThen(b) 表示"先做 a，然后做 b"
    //
    //     ⚠️ 重要：返回的 lambda 写成 second(first(n))，不是 first(second(n))
    //         理由：C# 求值从内层括号往外算
    //               所以"内层 = 先执行"
    //               名字叫 first 的必须放在内层 → 才符合"first 先执行"的语义
    public static Func<int, int> AndThen(
        this Func<int, int> first,
        Func<int, int> second)
    {
        // 返回一个新的 lambda
        // n 是这个新 lambda 自己的形参，调用时才拿到值
        // first 和 second 被"闭包捕获"
        return n => second(first(n));
        //          ──────  ──────
        //          外层    内层（最内层 = 最先执行）
    }
}

// -----------------------------------------------------------
// 补充：重名扩展方法 —— 编译器不会猜，会报错
// -----------------------------------------------------------
// 假设另一个 static class 也定义了 IsEven，但语义不同：
//
//   public static class MyExtensions1
//   {
//       public static bool IsEven(this int n) => n % 3 == 0;
//   }
//
// 同时 using 两个命名空间：
//   x.IsEven();   // ❌ 编译错误 ambiguous call
//   MyExtensions.IsEven(x);    // ✅ 必须显式指定
//   MyExtensions1.IsEven(x);   // ✅ 必须显式指定
//
// 编译器规则：从不猜测。遇到不明确就报错。

// -----------------------------------------------------------
// 第 2 部分：函数工厂 — 返回 lambda 的方法
// -----------------------------------------------------------
public static class Factories
{
    // 输入：你想加多少
    // 输出：一个"加这么多"的函数
    public static Func<int, int> AddBy(int amount)
    {
        // 等价拆解写法：
        //   Func<int, int> x = n => n + amount;
        //   return x;
        return n => n + amount;
        //     ↑       ↑
        //  形参 n  捕获了外层的 amount（闭包）
    }
}

// -----------------------------------------------------------
// 第 3 部分：演示主程序
// -----------------------------------------------------------
public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== 1) 扩展方法基础：n.IsEven() ===");
        int n = 6;
        Console.WriteLine($"{n}.IsEven() = {n.IsEven()}");
        // 等价写法：MyExtensions.IsEven(n)
        Console.WriteLine($"MyExtensions.IsEven({n}) = {MyExtensions.IsEven(n)}");

        Console.WriteLine();
        Console.WriteLine("=== 2) 扩展 string：WordCount ===");
        string sentence = "hello world from C#";
        Console.WriteLine($"\"{sentence}\".WordCount() = {sentence.WordCount()}");

        Console.WriteLine();
        Console.WriteLine("=== 3) 扩展 IEnumerable<int>：Average2 ===");
        var nums = new List<int> { 10, 20, 30, 40 };
        Console.WriteLine($"Average2 = {nums.Average2()}");

        Console.WriteLine();
        Console.WriteLine("=== 4) 多参数扩展：m.AddBy(3) ===");
        int m = 10;
        Console.WriteLine($"{m}.AddBy(3) = {m.AddBy(3)}");

        Console.WriteLine();
        Console.WriteLine("=== 5) 扩展委托 Func<int, bool>：Describe ===");
        Func<int, bool> isPositive = n => n > 0;
        Func<int, bool> isNegative = n => n < 0;
        Console.WriteLine($"isPositive.Describe() = {isPositive.Describe()}");
        Console.WriteLine($"isNegative.Describe() = {isNegative.Describe()}");

        Console.WriteLine();
        Console.WriteLine("=== 6) 函数工厂：AddBy(amount) 返回 lambda ===");
        Func<int, int> add5 = Factories.AddBy(5);
        Func<int, int> add7 = Factories.AddBy(7);
        Console.WriteLine($"add5(100) = {add5(100)}");   // 105
        Console.WriteLine($"add7(100) = {add7(100)}");   // 107

        // 等价的"笨"拆解写法（说明 lambda 只是个值）：
        //   public static Func<int, int> AddBy(int amount)
        //   {
        //       Func<int, int> x = n => n + amount;
        //       return x;
        //   }

        Console.WriteLine();
        Console.WriteLine("=== 7) 函数组合：AndThen ===");
        Func<int, int> addOne = x => x + 1;
        Func<int, int> times10 = x => x * 10;

        // 顺序 A：先 addOne 再 times10
        Func<int, int> combinedA = addOne.AndThen(times10);
        Console.WriteLine($"addOne.AndThen(times10) 应用到 5 = {combinedA(5)}");
        //  → addOne(5)=6 → times10(6)=60

        // 顺序 B：先 times10 再 addOne（顺序不同结果不同）
        Func<int, int> combinedB = times10.AndThen(addOne);
        Console.WriteLine($"times10.AndThen(addOne) 应用到 5 = {combinedB(5)}");
        //  → times10(5)=50 → addOne(50)=51

        Console.WriteLine();
        Console.WriteLine("=== 8) 链式组合 pipeline ===");
        Func<int, int> pipeline = addOne
            .AndThen(times10)
            .AndThen(x => x - 5);
        Console.WriteLine($"((5+1)*10)-5 = {pipeline(5)}");   // 55

        Console.WriteLine();
        Console.WriteLine("Done.");
    }
}

// ============================================================
// 复习题（明天自测）
// ============================================================
// 扩展方法
// Q1. 写出扩展方法的三条铁律。
// Q2. n.IsEven() 编译后等价于哪句静态调用？
// Q3. 编译器靠什么定位扩展方法（不是 class 名字）？
// Q4. 两个 class 都定义了 IsEven(this int n)，同时 using 它们会怎样？
// Q5. 能给委托类型（如 Func<int, bool>）写扩展方法吗？
//
// => 语法
// Q6. 下面有几个 => ？分别是 lambda 箭头还是表达式体？
//       public Func<int,int> MakeAdder(int n) => x => x + n;
// Q7. 表达式体 => 后面能写代码块 { ... } 吗？
// Q8. public string Name => "Tom"; 这里的 => 是什么？
//
// Lambda
// Q9. Lambda n => n + 5 和方法 int Add5(int n) { return n + 5; } 有什么关系？
// Q10. AddBy(7) 调用结束、还没用它的时候，lambda 里的 n 是什么？
// Q11. AddBy(7) 返回的 lambda 怎么"记住"了 7？这种现象叫什么？
// Q12. 把 return n => n + amount; 拆成两行等价代码。
//
// 函数组合
// Q13. AndThen 方法体里 second(first(n))，谁先执行？为什么不能改成 first(second(n))？
// Q14. times10.AndThen(addOne) 应用到 5 等于多少？
// Q15. 用 addOne、times10、x => x - 5 组合一个 pipeline，作用到 5 等于 55，怎么写？
//
// 语法盲点
// Q16. List<T>、int[]、IEnumerable<T> 三种类型分别怎么取长度？
// Q17. 为什么 LINQ 的 list.Where(...) 看起来像 List 自带方法？
