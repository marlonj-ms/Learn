// Day 50 — 嵌套泛型阅读流利度 (Nested Generics Reading Fluency)
// =====================================================================
// 目标：把 List<List<int>> / Dictionary<string, List<Order>> / Func<...>
// 这种"长签名"训练到看一眼就能：
//   (1) 说出它在表达什么（业务白话）
//   (2) 写出正确的访问代码（链式 + ^ 帽子运算符 + TryGetValue）
//   (3) 不被嵌套深度吓退
//
// 运行方式（独立 console 程序）：
//   cd d:\AITriage\2026-05-28
//   dotnet new console -n NestedGenericsFluency -o .  # 仅首次需要
//   # 然后 Program.cs 替换为本文件内容
//   dotnet run
//
// 本文件按 6 级阶梯组织，配上口诀和常见坑（对照 Summary.md）。

using System;
using System.Collections.Generic;

namespace NestedGenericsFluency;

// ---------------------------------------------------------------------
// 业务模型 —— L5/L6 用
// ---------------------------------------------------------------------
public class Order
{
    public required int Id { get; init; }
    public required decimal Total { get; init; }    // 💰 金额永远用 decimal，别用 double
    public override string ToString() => $"Order(#{Id}, {Total:C})";
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Day 50 — Nested Generics Fluency ===\n");

        Level1_SingleClosed();
        Level2_OpenVsClosed();
        Level3_DoubleNested();
        Level4_TripleNested();
        Level5_MixedContainers();
        Level6_FuncWrappingNested();

        Console.WriteLine("\n✅ All 6 levels GREEN.");
    }

    // -----------------------------------------------------------------
    // L1 — 单层封闭泛型 (Single-layer closed generic)
    // -----------------------------------------------------------------
    static void Level1_SingleClosed()
    {
        Console.WriteLine("--- L1: List<int> 单层封闭 ---");

        // 3 种等价初始化语法（不同人会写不同写法，读代码时都要认得）
        var a = new List<int>() { 10, 20, 30 };       // 完整 ctor + collection initializer
        var b = new List<int> { 10, 20, 30 };         // 省略空括号（仅无参 ctor 才行）
        List<int> c = new() { 10, 20, 30 };           // C# 9+ 目标类型 new()，跟着左侧期望类型走

        // 🪤 坑：空 list 不等于 null
        var empty = new List<int>();          // 这是一个"没有任何元素"的盒子，不是"没有盒子"
        Console.WriteLine($"  empty.Count = {empty.Count}");       // 0
        Console.WriteLine($"  empty == null ? {empty == null}");   // False
        Console.WriteLine($"  a[0]={a[0]}, b[^1]={b[^1]}, c.Count={c.Count}");
    }

    // -----------------------------------------------------------------
    // L2 — 开放 vs 封闭 (Open vs Closed Generic Type)
    // -----------------------------------------------------------------
    //  开放泛型类型 (open generic type)：List<T> —— T 还没被替换，不能直接 new
    //  封闭泛型类型 (closed generic type)：List<int> —— T 被替换成具体类型，可以 new
    static void Level2_OpenVsClosed()
    {
        Console.WriteLine("\n--- L2: List<T> 开放 vs 封闭 ---");

        // 方法本身是开放的（参数类型用 T）
        // List<int> / List<string> 都是它的具体实例化（closed）
        var ints = MakeBag<int>();      // 封闭：T → int
        var strs = MakeBag<string>();   // 封闭：T → string

        ints.Add(42);
        strs.Add("hello");
        Console.WriteLine($"  ints[0]={ints[0]}, strs[0]={strs[0]}");
    }

    // 开放泛型方法 (open generic method)
    static List<T> MakeBag<T>() => new();

    // -----------------------------------------------------------------
    // L3 — 双层嵌套 (Double Nesting)
    // -----------------------------------------------------------------
    static void Level3_DoubleNested()
    {
        Console.WriteLine("\n--- L3: List<List<int>> / Dictionary<string,int> 双层嵌套 ---");

        // (1) 锯齿数组 (jagged) —— 每行可以不一样长
        List<List<int>> matrix = new()
        {
            new() { 1, 2, 3 },         // row 0
            new() { 4, 5, 6 },         // row 1
            new() { 7, 8, 9 },         // row 2
        };

        // 🪤 0-indexed：3×3 矩阵的最后一格不是 [3][3]，是 [2][2]
        //    更稳的写法：用 ^1（"倒数第 1 个"）
        int lastCell = matrix[^1][^1];   // = matrix[2][2] = 9
        Console.WriteLine($"  matrix[^1][^1] = {lastCell}");
        Console.WriteLine($"  matrix[0][2]   = {matrix[0][2]}");

        // (2) Dictionary —— 同样是双层泛型，但 [Key] 不是 [Index]！
        Dictionary<string, int> scoreById = new()
        {
            ["alice"] = 92,
            ["bob"]   = 60,
        };
        Console.WriteLine($"  scoreById[\"alice\"] = {scoreById["alice"]}");

        // 🪤 用 [] 取不存在的 key → KeyNotFoundException
        // ✅ 安全姿势：TryGetValue
        if (scoreById.TryGetValue("carol", out var s))
            Console.WriteLine($"  carol = {s}");
        else
            Console.WriteLine("  carol not found (safely handled)");
    }

    // -----------------------------------------------------------------
    // L4 — 三层嵌套 (Triple Nesting) —— 同模式更深一层
    // -----------------------------------------------------------------
    static void Level4_TripleNested()
    {
        Console.WriteLine("\n--- L4: List<List<List<int>>> 三层嵌套 ---");

        // 业务场景：班级 → 学生 → 各科分数
        //   cube[c][s][k] = 第 c 班、第 s 学生、第 k 科的分数
        List<List<List<int>>> cube = new()
        {
            // 班 0
            new() { new() { 90, 85 }, new() { 70, 60 } },
            // 班 1
            new() { new() { 88, 92 } },
        };

        Console.WriteLine($"  cube[0][1][0]   = {cube[0][1][0]}");      // = 70
        Console.WriteLine($"  cube[^1][^1][^1] = {cube[^1][^1][^1]}");  // = 92
        // 心法：嵌套加深，只是同一个动作多做几次。"剥洋葱" 模式不变。
    }

    // -----------------------------------------------------------------
    // L5 — 混合容器 (Mixed Containers) —— 生产级真签名
    // -----------------------------------------------------------------
    static void Level5_MixedContainers()
    {
        Console.WriteLine("\n--- L5: Dictionary<string, List<Order>> 混合 ---");

        // 业务白话：按 customerId 查这个客户的所有订单列表
        Dictionary<string, List<Order>> ordersByCustomer = new()
        {
            ["alice"] = new()
            {
                new Order { Id = 1, Total = 100m },
                new Order { Id = 2, Total = 250m },
            },
            ["bob"] = new()
            {
                new Order { Id = 3, Total = 80m },
            },
        };

        // 🪤 case-sensitive：用 "Alice" 取会 KeyNotFound
        //    想忽略大小写，初始化时传 StringComparer.OrdinalIgnoreCase
        //    var dict = new Dictionary<string, List<Order>>(StringComparer.OrdinalIgnoreCase);
        Order firstAlice    = ordersByCustomer["alice"][0];          // Order 类型
        int   bobOrderCount = ordersByCustomer["bob"].Count;          // int
        decimal lastTotalA  = ordersByCustomer["alice"][^1].Total;    // decimal —— 链式取值，最后一步类型决定
        Console.WriteLine($"  alice 第一单 = {firstAlice}");
        Console.WriteLine($"  bob 订单数  = {bobOrderCount}");
        Console.WriteLine($"  alice 最后一单金额 = {lastTotalA:C}");

        // 安全查询用 TryGetValue
        if (ordersByCustomer.TryGetValue("carol", out var carolOrders))
            Console.WriteLine($"  carol = {carolOrders.Count}");
        else
            Console.WriteLine("  Customer 'carol' not found");
    }

    // -----------------------------------------------------------------
    // L6 — Delegate 包裹嵌套泛型 (Func wrapping nested generics)
    // -----------------------------------------------------------------
    //   读法心法：目光直接弹到最后一个泛型参数 = 返回类型；前面全是输入。
    //   Func<A, B>           —— 吃 A，吐 B
    //   Func<A, B, C>        —— 吃 A、B，吐 C
    //   Func<...,Last>       —— Last 永远是返回类型
    static void Level6_FuncWrappingNested()
    {
        Console.WriteLine("\n--- L6: Func<Dictionary<string, List<int>>, List<string>> ---");

        // 输入：每个学生 → 这个学生的所有分数
        // 返回：高分学生姓名列表（注意：返回的是 string 名字，不是 int 分数！）
        Func<Dictionary<string, List<int>>, List<string>> findHighScorers = scoresByStudent =>
        {
            List<string> winners = new();
            foreach (var (name, scores) in scoresByStudent)
            {
                int sum = 0;
                foreach (var s in scores) sum += s;
                double avg = sum / (double)scores.Count;
                if (avg >= 85) winners.Add(name);
            }
            return winners;
        };

        var scoresByStudent = new Dictionary<string, List<int>>
        {
            ["alice"] = new() { 92, 88, 95 },
            ["bob"]   = new() { 60, 55, 70 },
            ["carol"] = new() { 99, 91, 100 },
        };

        List<string> highScorers = findHighScorers(scoresByStudent);
        Console.WriteLine($"  high scorers = [{string.Join(", ", highScorers)}]"); // alice, carol

        // 另一个示例：Func<List<Order>, Dictionary<string, decimal>>
        // 业务：把一堆订单按 category 汇总成"类别→总金额"
        Func<List<Order>, Dictionary<string, decimal>> summarize = orders =>
        {
            // 简化：把所有订单都丢进一个 "All" 桶
            decimal sum = 0m;
            foreach (var o in orders) sum += o.Total;
            return new Dictionary<string, decimal> { ["All"] = sum };
        };

        var myOrders = new List<Order>
        {
            new() { Id = 100, Total = 1_000m },        // 数字下划线分隔符 _ 让人易读
            new() { Id = 101, Total = 250.5m },        // 💰 decimal 字面量必须带后缀 m！
        };
        Dictionary<string, decimal> totals = summarize(myOrders);
        Console.WriteLine($"  totals[\"All\"] = {totals["All"]:C}");
    }
}
