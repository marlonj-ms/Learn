// Day 51 — LINQ 链式流利度 Demo (LINQ Chain Fluency)
// =====================================================================
// 目标：用一个可跑的 console 把 Day 51 的 8 条口诀 (#44–#51) 全部演示出来。
// 跑完一次你就亲眼看到：
//   ☎️ 谓词在不同时刻被调用几次（延迟执行）
//   📦 GroupBy 的双层结构（袋子 + 袋里的元素）
//   🥇 Bob 是 Top 消费用户（毕业题真实跑通）
//   🔬 OrderByDescending 返回的真实类型名 (IOrderedEnumerable)
//
// 运行方式（独立 console 程序）：
//   cd d:\AITriage\2026-05-29
//   dotnet new console -n LinqChainFluency -o .   # 仅首次需要
//   # 然后用本文件替换生成的 Program.cs
//   dotnet run
//
// 配套阅读：LINQ-Chain-Fluency-Summary.md (在同目录)

using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqChainFluency;

// ---------------------------------------------------------------------
// 业务模型 —— record 主构造器（一行 = 类 + 4 字段 + 构造函数 + 值相等）
// ---------------------------------------------------------------------
public record Order(int Id, string CustomerName, decimal Amount, string Status);

public record User(int Id, string Name, string Email, int Age);

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  Day 51 — LINQ Chain Fluency Demo");
        Console.WriteLine("=================================================\n");

        Part1_DeferredExecution();
        Part2_SelectProjection();
        Part3_GroupBy();
        Graduation_TopCustomer();
    }

    // =================================================================
    // Part 1 — 延迟执行 (Deferred Execution)
    // 口诀 #44 / #45 / #46
    // =================================================================
    static void Part1_DeferredExecution()
    {
        PrintHeader("Part 1 — 延迟执行 (#44 #45 #46)");

        var nums = new List<int> { 1, 2, 3, 4, 5 };

        // --- 口诀 #44 — Where 返回 IEnumerable<T>，不是 List<T> ---
        var result = nums.Where(n =>
        {
            Console.WriteLine($"  ☎️ 谓词被调用，正在检查 {n}");
            return n > 2;
        });

        // 类型探针：注意打印出来是 WhereListIterator/Enumerable+...，
        // 不是 List —— 这就是 #44 的实锤
        Console.WriteLine($"\n📌 result 的运行时类型: {result.GetType().Name}");
        Console.WriteLine("   (注意：不是 List，是某种 *Iterator) \n");

        Console.WriteLine("=== Where 已经写完，还没 foreach (☎️ 应该为 0 次) ===\n");

        // --- 口诀 #45 — 每次 foreach 配方就重做一次 ---
        Console.WriteLine("=== 第 1 次 foreach ===");
        foreach (var x in result) Console.WriteLine($"  取到 {x}");

        Console.WriteLine("\n=== 第 2 次 foreach (☎️ 会再被调用 5 次！) ===");
        foreach (var x in result) Console.WriteLine($"  再取到 {x}");

        // --- 口诀 #45 修复 — 物化一次，复用 N 次 ---
        Console.WriteLine("\n=== 修复：先 ToList() 煮熟 ===");
        var materialized = nums.Where(n =>
        {
            Console.WriteLine($"  🍳 [物化版] 谓词检查 {n}");
            return n > 2;
        }).ToList();   // ← 一次性下锅

        Console.WriteLine("\n--- 现在多次遍历，谓词不再被调用 ---");
        foreach (var x in materialized) Console.WriteLine($"  取到 {x}");
        foreach (var x in materialized) Console.WriteLine($"  再取到 {x}");

        // --- 口诀 #46 — 延迟执行 + 数据源变化 ---
        Console.WriteLine("\n=== 口诀 #46 — 配方动态绑定 ===");
        var step1 = nums.Where(n => n > 2);
        var step2 = step1.Select(n => n * 10);

        nums.Add(6);   // ← 在 ToList 之前改了源
        var output = step2.ToList();
        Console.WriteLine($"  nums.Add(6) 后 ToList() = [{string.Join(", ", output)}]");
        Console.WriteLine("  → 60 出现了！因为配方在 ToList() 那一刻才看冰箱");
    }

    // =================================================================
    // Part 2 — Select 投影 + 现代语法糖
    // 口诀 #47 / #48
    // =================================================================
    static void Part2_SelectProjection()
    {
        PrintHeader("Part 2 — Select 投影 (#47 #48)");

        // --- 口诀 #48 — collection expression [...] + target-typed new(...) ---
        List<User> users = [
            new(1, "Alice",   "a@x.com", 30),
            new(2, "Bob",     "b@x.com", 25),
            new(3, "Charlie", "c@x.com", 35),
        ];

        // --- 口诀 #47 — Select 换类型，保个数 ---

        // 用法 1：string → int（换类型）
        IEnumerable<int> lengths = users.Select(u => u.Name.Length);
        Console.WriteLine($"  Names 的长度: [{string.Join(", ", lengths)}]");
        Console.WriteLine($"  → 输入 {users.Count} 个 User，输出 {lengths.Count()} 个 int (#47 保个数)\n");

        // 用法 2：抽字段
        IEnumerable<string> names = users.Select(u => u.Name);
        Console.WriteLine($"  抽字段: [{string.Join(", ", names)}]\n");

        // 用法 3：匿名类型（生产代码 90% 用法）
        var lightweight = users.Select(u => new { u.Id, u.Name }).ToList();
        Console.WriteLine("  匿名类型投影:");
        foreach (var item in lightweight)
            Console.WriteLine($"    {{ Id={item.Id}, Name={item.Name} }}");
    }

    // =================================================================
    // Part 3 — GroupBy 双层嵌套
    // 口诀 #49 / #50 / #51
    // =================================================================
    static void Part3_GroupBy()
    {
        PrintHeader("Part 3 — GroupBy 双层嵌套 (#49 #50 #51)");

        List<Order> orders = [
            new(1, "Alice", 120m, "Paid"),
            new(2, "Bob",   500m, "Paid"),
            new(3, "Alice",  80m, "Pending"),
            new(4, "Carol", 300m, "Paid"),
            new(5, "Bob",   200m, "Cancelled"),
        ];

        // --- 口诀 #49 — IEnumerable<IGrouping<TKey, TSource>> 双层 ---
        var groups = orders.GroupBy(o => o.Status);
        Console.WriteLine($"📌 groups 的运行时类型: {groups.GetType().Name}\n");

        // --- 双层 foreach ---
        foreach (var g in groups)
        {
            Console.WriteLine($"📦 袋子 Key = \"{g.Key}\" ({g.Count()} 单)");

            // 口诀 #51 验证 — 袋里装的是完整 Order，Status 字段没丢
            foreach (var o in g)
                Console.WriteLine($"     Id={o.Id}, Amount={o.Amount}, Status={o.Status}  ← Status 还在!");

            Console.WriteLine();
        }

        // --- 口诀 #50 — 顺序按"首次出现"序，但接口不承诺 ---
        Console.WriteLine("⚠️  #50 提醒：袋子顺序在 LINQ to Objects 里是\"首次出现\"序，");
        Console.WriteLine("    但接口不承诺。要顺序请显式 .OrderBy(g => g.Key)。\n");
    }

    // =================================================================
    // 毕业题 — Top Customer 综合链
    // 一条链用上 Where + GroupBy + Select + OrderByDescending + ToList
    // =================================================================
    static void Graduation_TopCustomer()
    {
        PrintHeader("🎓 毕业题 — Top Customer 综合链");

        List<Order> orders = [
            new(1,  "Alice", 120m, "Paid"),
            new(2,  "Bob",   500m, "Paid"),
            new(3,  "Alice",  80m, "Pending"),
            new(4,  "Carol", 300m, "Paid"),
            new(5,  "Bob",   200m, "Cancelled"),
            new(6,  "Alice", 150m, "Paid"),
            new(7,  "Dave",  400m, "Paid"),
            new(8,  "Carol", 100m, "Paid"),
            new(9,  "Bob",   250m, "Paid"),
            new(10, "Dave",   50m, "Pending"),
        ];

        var report = orders
            .Where(o => o.Status == "Paid")
            .GroupBy(o => o.CustomerName)
            .Select(g => new
            {
                Customer = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.Amount)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        // 一并演示 OrderByDescending 返回 IOrderedEnumerable
        var ordered = orders
            .Where(o => o.Status == "Paid")
            .OrderByDescending(o => o.Amount);
        Console.WriteLine($"📌 OrderByDescending 的运行时类型: {ordered.GetType().Name}");
        Console.WriteLine("   (注意：含 OrderedEnumerable 字样 —— 这就是为什么能链 ThenBy)\n");

        // --- 打印报告 ---
        Console.WriteLine("Top Customer Report (已付款订单):");
        Console.WriteLine("─────────────────────────────────────");
        Console.WriteLine($"{"Rank",-6}{"Customer",-10}{"Orders",-10}{"TotalSpent",12}");
        Console.WriteLine("─────────────────────────────────────");
        int rank = 1;
        foreach (var row in report)
        {
            var marker = rank == 1 ? "🥇" : (rank == 2 ? "🥈" : (rank == 3 ? "🥉" : "  "));
            Console.WriteLine($"{marker} {rank,-4}{row.Customer,-10}{row.OrderCount,-10}{row.TotalSpent,12:C}");
            rank++;
        }
        Console.WriteLine("─────────────────────────────────────");
        Console.WriteLine($"\n✅ 第一名 = {report[0].Customer} ({report[0].TotalSpent:C}, {report[0].OrderCount} 单)");
    }

    // =================================================================
    // Helper
    // =================================================================
    static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────");
        Console.WriteLine($"  {title}");
        Console.WriteLine("─────────────────────────────────────────────────");
    }
}
