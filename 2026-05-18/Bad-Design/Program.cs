using System;
using System.Collections.Generic;

// ============================================================
// ❌ BAD DESIGN — Invoke 散落在每个业务方法里
// ❌ BAD DESIGN — Invoke duplicated in every business method
// ============================================================
//
// 问题（problems）：
//   1. Invoke(...) 出现 3 次 → 重复代码（DRY violation）。
//   2. 想给所有事件触发加日志，要改 3 个地方。
//   3. 子类无法集中扩展事件触发行为：
//      event 字段本身不能 virtual，
//      没有单一的 OnXxx 方法可以 override。
//
//   1. Invoke(...) appears 3 times → duplicated code (DRY violation).
//   2. To add logging to every raise, you must edit 3 places.
//   3. Subclasses cannot centrally extend event-raising behavior:
//      the event field itself cannot be virtual,
//      there is no single OnXxx method to override.
// ============================================================

namespace BadDesignDemo;

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
// 发布者（publisher）
// ------------------------------------------------------------
public class BadOrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[Bad] Completing single order {orderId}");

        var args = new OrderCompletedEventArgs(orderId, "Single");
        OrderCompleted?.Invoke(this, args);     // ← duplicated #1
    }

    public void CompleteBulkOrders(IEnumerable<string> orderIds)
    {
        foreach (var orderId in orderIds)
        {
            Console.WriteLine($"[Bad] Completing bulk order {orderId}");

            var args = new OrderCompletedEventArgs(orderId, "Bulk");
            OrderCompleted?.Invoke(this, args); // ← duplicated #2
        }
    }

    public void AutoCompleteExpiredOrders()
    {
        var expired = new[] { "EXP-1", "EXP-2" };

        foreach (var orderId in expired)
        {
            Console.WriteLine($"[Bad] Auto-completing expired order {orderId}");

            var args = new OrderCompletedEventArgs(orderId, "Auto");
            OrderCompleted?.Invoke(this, args); // ← duplicated #3
        }
    }
}

// ------------------------------------------------------------
// Program — run and observe the duplicated Invoke pattern
// ------------------------------------------------------------
public class Program
{
    public static void Main()
    {
        Console.WriteLine("================ ❌ Bad design ================");

        var service = new BadOrderService();
        service.OrderCompleted += (sender, e) =>
            Console.WriteLine($"   subscriber heard: {e.OrderId} ({e.Source})");

        service.CompleteOrder("A-100");
        service.CompleteBulkOrders(new[] { "B-200", "B-201" });
        service.AutoCompleteExpiredOrders();

        Console.WriteLine();
        Console.WriteLine("注意：每个业务方法里都重复写了 Invoke(...)。");
        Console.WriteLine("Notice: every business method repeats Invoke(...).");
    }
}
