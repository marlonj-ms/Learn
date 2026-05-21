using System;
using System.Collections.Generic;

// ============================================================
// ✅ GOOD DESIGN — 用 OnOrderCompleted 集中触发事件
// ✅ GOOD DESIGN — Centralize raising via OnOrderCompleted
// ============================================================
//
// 优势（advantages）：
//   1. Invoke(...) 只写一次 → DRY 原则满足。
//   2. 想给所有事件触发加日志，只改一处。
//   3. 子类可以 override OnOrderCompleted，
//      一次 override 影响所有业务方法的事件触发行为。
//
//   1. Invoke(...) is written only once → DRY satisfied.
//   2. To add logging to every raise, edit just one place.
//   3. Subclasses can override OnOrderCompleted to affect
//      the event raising behavior of ALL business methods at once.
// ============================================================

namespace GoodDesignDemo;

// ------------------------------------------------------------
// 事件数据类（event args class）
// ------------------------------------------------------------
public sealed class OrderCompletedEventArgs : EventArgs
{
    public string OrderId { get; }
    public string Source  { get; }   // "Single" / "Bulk" / "Auto"

    public OrderCompletedEventArgs(string orderId, string source)
    {
        OrderId = orderId;
        Source  = source;
    }
}

// ------------------------------------------------------------
// 发布者（publisher） — 使用集中的事件触发方法
// publisher — uses a centralized event raising method
// ------------------------------------------------------------
public class GoodOrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    // 业务方法 1（business method 1）
    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[Good] Completing single order {orderId}");

        OnOrderCompleted(new OrderCompletedEventArgs(orderId, "Single"));
    }

    // 业务方法 2（business method 2）
    public void CompleteBulkOrders(IEnumerable<string> orderIds)
    {
        foreach (var orderId in orderIds)
        {
            Console.WriteLine($"[Good] Completing bulk order {orderId}");

            OnOrderCompleted(new OrderCompletedEventArgs(orderId, "Bulk"));
        }
    }

    // 业务方法 3（business method 3）
    public void AutoCompleteExpiredOrders()
    {
        var expired = new[] { "EXP-1", "EXP-2" };

        foreach (var orderId in expired)
        {
            Console.WriteLine($"[Good] Auto-completing expired order {orderId}");

            OnOrderCompleted(new OrderCompletedEventArgs(orderId, "Auto"));
        }
    }

    // ★ 唯一的事件触发方法（the ONE raising method）
    // protected = 外部无法滥用，子类可以访问
    // virtual   = 子类可以 override 来自定义事件触发行为
    protected virtual void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        OrderCompleted?.Invoke(this, e);
    }
}

// ------------------------------------------------------------
// 🚀 子类扩展 — 一次 override 影响所有业务方法
// 🚀 Subclass extension — one override affects all business methods
// ------------------------------------------------------------
public class AuditingOrderService : GoodOrderService
{
    private readonly List<string> _auditLog = new();

    protected override void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        // 1. 事件触发前的逻辑（pre-event logic）
        var entry = $"[AUDIT] About to raise OrderCompleted: {e.OrderId} (source={e.Source})";
        _auditLog.Add(entry);
        Console.WriteLine(entry);

        // 2. 调用父类的版本，真正触发事件
        base.OnOrderCompleted(e);

        // 3. 事件触发后的逻辑（post-event logic）
        Console.WriteLine($"[AUDIT] Done raising OrderCompleted for {e.OrderId}");
    }

    public void DumpAuditLog()
    {
        Console.WriteLine();
        Console.WriteLine("=== Audit Log ===");
        foreach (var entry in _auditLog)
        {
            Console.WriteLine(entry);
        }
    }
}

// ------------------------------------------------------------
// Program — run base good design, then subclass extension
// ------------------------------------------------------------
public class Program
{
    public static void Main()
    {
        Console.WriteLine("================ ✅ Good design ================");
        RunGoodDesign();

        Console.WriteLine();
        Console.WriteLine("================ 🚀 Subclass extends ================");
        RunSubclassDesign();
    }

    private static void RunGoodDesign()
    {
        var service = new GoodOrderService();
        service.OrderCompleted += (sender, e) =>
            Console.WriteLine($"   subscriber heard: {e.OrderId} ({e.Source})");

        service.CompleteOrder("A-100");
        service.CompleteBulkOrders(new[] { "B-200", "B-201" });
        service.AutoCompleteExpiredOrders();
    }

    private static void RunSubclassDesign()
    {
        var service = new AuditingOrderService();
        service.OrderCompleted += (sender, e) =>
            Console.WriteLine($"   subscriber heard: {e.OrderId} ({e.Source})");

        // 调用方代码完全没变，但 [AUDIT] 自动出现在所有事件触发周围。
        // The calling code is identical, yet [AUDIT] appears around every event raise.
        service.CompleteOrder("A-100");
        service.CompleteBulkOrders(new[] { "B-200", "B-201" });
        service.AutoCompleteExpiredOrders();

        service.DumpAuditLog();
    }
}
