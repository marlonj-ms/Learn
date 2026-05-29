namespace JoinDemo;

// Day 52 — LINQ Join 全家桶第一个：等值连接 (equi-join)
//
// 关键 4 铁律：
//   1. Join 是 INNER JOIN —— 任一边没匹配的行直接丢掉 (Carol 会蒸发)
//   2. 一条 outer 可匹配多条 inner —— Bob 有 2 张订单都各自配对
//   3. 结果行数 = 每条 outer 能匹配的 inner 数之和 (这里 = 4)
//   4. 顺序跟随 outer (LINQ to Objects 的实现承诺)
//
// 口诀复用：
//   #43 读 Func<...> 弹到最后一个泛型参数 = 返回类型 → Join 返回 IEnumerable<TResult>
//   lambda 体里访问成员永远带参数名前缀 (o.CustomerId 不是 CustomerId)
//   Join 不靠 column 名字，靠两个 keySelector 各自吐出的值是否相等 (equi-join)

public record Customer(int Id, string Name);
public record Order(int OrderId, int CustomerId, decimal Amount);

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  Day 52 — LINQ Join Demo (INNER JOIN / equi-join)");
        Console.WriteLine("=================================================\n");

        var customers = new List<Customer>
        {
            new(1, "Alice"),
            new(2, "Bob"),
            new(3, "Carol"),   // 没订单 — 看她会不会蒸发
        };

        var orders = new List<Order>
        {
            new(101, CustomerId: 1, Amount: 200m),
            new(102, CustomerId: 2, Amount: 300m),
            new(103, CustomerId: 2, Amount: 450m),
            new(104, CustomerId: 1, Amount: 100m),
        };

        // orders = outer, customers = inner
        //   o => o.CustomerId   外键 (int)
        //   c => c.Id           主键 (int)  ← 名字不同但类型相同 → equi-join 合法
        //   (o, c) => ...       resultSelector：自由造型车间
        var report = orders.Join(
            customers,
            o => o.CustomerId,
            c => c.Id,
            (o, c) => new { c.Name, o.OrderId, o.Amount });

        Console.WriteLine("预测：4 行 (Carol 蒸发，Bob 出现 2 次)\n");

        int rowCount = 0;
        foreach (var row in report)
        {
            rowCount++;
            Console.WriteLine($"  {row.Name,-6} | Order #{row.OrderId} | ¥{row.Amount}");
        }

        Console.WriteLine($"\n实际行数 = {rowCount}");
        Console.WriteLine(rowCount == 4 ? "✅ 预测命中！" : "❌ 预测落空");

        // 证明 Carol(Id=3) 真的被 INNER JOIN 丢掉了
        bool carolAppeared = report.Any(r => r.Name == "Carol");
        Console.WriteLine($"\nCarol 出现了吗？{(carolAppeared ? "是" : "否 —— INNER JOIN 丢掉了没匹配的行 💀")}");
    }
}
