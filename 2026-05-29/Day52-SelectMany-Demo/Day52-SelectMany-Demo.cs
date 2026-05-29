// Day 52 — SelectMany Demo
// 目标：把"嵌套结构（nested / 二维）"拍平成"扁平序列（flat / 一维）"
// 核心对比：Select  → 投影器返回啥就吐啥（一对一）
//          SelectMany → 投影器必须返回集合，自动拼接（一对多 + flatten）

namespace SelectManyDemo;

public record Customer(string Name, List<int> OrderAmounts);
public record Order(int OrderId, decimal Amount);
public record CustomerWithOrders(string Name, List<Order> Orders);

public static class Program
{
    public static void Main()
    {
        // ===== 数据集 1：客户带订单金额（每客户是 List<int>）=====
        var customers = new List<Customer>
        {
            new("Alice", new() { 200, 100 }),
            new("Bob",   new() { 300, 450, 50 }),
            new("Carol", new() { })   // 空 List：故意制造"无子元素"案例
        };

        // ----- 场景 1：Select 的尴尬 — 拿到嵌套结构 -----
        Console.WriteLine("=== 场景 1：Select 返回嵌套 IEnumerable<List<int>> ===");
        var nested = customers.Select(c => c.OrderAmounts);
        // 类型：IEnumerable<List<int>>  ← 二维
        Console.WriteLine($"外层元素个数：{nested.Count()}");   // 3（每个 customer 一项）
        foreach (var sub in nested)
        {
            Console.WriteLine($"  子集合：[{string.Join(", ", sub)}]  (Count = {sub.Count})");
        }
        // 输出说明：你得手写 foreach-of-foreach 才能拿到单个数字 → 难受

        // ----- 场景 2：SelectMany 一刀解决 — 拍平 -----
        Console.WriteLine("\n=== 场景 2：SelectMany 返回扁平 IEnumerable<int> ===");
        var flat = customers.SelectMany(c => c.OrderAmounts);
        // 类型：IEnumerable<int>  ← 一维！
        Console.WriteLine($"扁平后元素个数：{flat.Count()}");  // 5 (200,100,300,450,50)
        Console.WriteLine($"全部金额：[{string.Join(", ", flat)}]");
        Console.WriteLine($"总额：{flat.Sum()}");              // 1100
        // 关键观察：Carol 的空 List 没有报错，也没有 null —— 0 个元素静默贡献

        // ----- 场景 3：string 是 IEnumerable<char> 的隐藏属性 -----
        Console.WriteLine("\n=== 场景 3：string 本身就是 char 子集合 ===");
        var words = new List<string> { "hello", "world" };

        // 3a) Select：类型 IEnumerable<string>，2 个元素
        var stringsKept = words.Select(w => w);
        Console.WriteLine($"Select 元素个数：{stringsKept.Count()}");  // 2
        Console.Write("Select 打印：");
        foreach (var s in stringsKept) Console.Write(s);
        Console.WriteLine();  // helloworld（但其实是两个 string 拼起来）

        // 3b) SelectMany：类型 IEnumerable<char>，10 个元素 ← 这才是真正展开
        var chars = words.SelectMany(w => w);
        Console.WriteLine($"SelectMany 元素个数：{chars.Count()}");    // 10  ← 露馅！
        Console.Write("SelectMany 打印：");
        foreach (var c in chars) Console.Write(c);
        Console.WriteLine();  // helloworld（每个字符独立元素）

        // ----- 场景 4：SelectMany 两参数版本 — 父子配对（像 Join 的兄弟）-----
        Console.WriteLine("\n=== 场景 4：SelectMany 双参版 — 父子配对投影 ===");
        var customersV2 = new List<CustomerWithOrders>
        {
            new("Alice", new() { new(101, 200m), new(104, 100m) }),
            new("Bob",   new() { new(102, 300m), new(103, 450m), new(105, 50m) }),
            new("Carol", new() { })  // 依然没有订单
        };

        var report = customersV2.SelectMany(
            c => c.Orders,                                       // ① 子集合在哪
            (c, o) => new { c.Name, o.OrderId, o.Amount });      // ② 父+子怎么配对

        foreach (var row in report)
            Console.WriteLine($"  {row.Name,-6} | #{row.OrderId} | ¥{row.Amount}");
        // Alice  | #101 | ¥200
        // Alice  | #104 | ¥100
        // Bob    | #102 | ¥300
        // Bob    | #103 | ¥450
        // Bob    | #105 | ¥50
        // Carol 自动消失（空子集合 = 0 行贡献）

        Console.WriteLine("\n✅ 三连击锁定：");
        Console.WriteLine("  口诀：Select 一对一，SelectMany 一对多 + 自动 flatten");
        Console.WriteLine("  陷阱：投影器返回集合才能用 SelectMany；返回单值用 Select");
        Console.WriteLine("  彩蛋：string 是 IEnumerable<char>，能被 SelectMany 拆成单字符");
    }
}
