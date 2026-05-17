using System;

// ============================================================
// 2026-05-17 学习内容：标准 .NET 事件模式
// ============================================================
// 前置理解：
//   event = 受限制的 delegate，外部只能 += / -=。
//
// 今天目标：
//   1. 理解 EventHandler<TEventArgs>
//   2. 理解 sender 和 e
//   3. 理解为什么生产代码常用 OnOrderCompleted(...) 方法来触发事件
// ============================================================

namespace EventsStandardPattern;

// ============================================================
// 第 1 部分：事件数据类（event args class）
// ============================================================
public sealed class OrderCompletedEventArgs : EventArgs
{
    public string OrderId { get; }
    public DateTime CompletedAt { get; }

    public OrderCompletedEventArgs(string orderId, DateTime completedAt)
    {
        OrderId = orderId;
        CompletedAt = completedAt;
    }
}

// ============================================================
// 第 2 部分：发布事件的类（event publisher）
// ============================================================
public sealed class OrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[OrderService] Completing order {orderId}");

        var args = new OrderCompletedEventArgs(
            orderId,
            DateTime.Now);

        OnOrderCompleted(args);
    }

    private void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        // this = 当前 OrderService 实例，也就是事件发送者（event sender）。
        // e    = 事件数据对象（event args）。
        OrderCompleted?.Invoke(this, e);
    }
}

// ============================================================
// 第 3 部分：订阅事件的类（event subscriber）
// ============================================================
public sealed class EmailNotifier
{
    public void HandleOrderCompleted(object? sender, OrderCompletedEventArgs e)
    {
        Console.WriteLine($"[EmailNotifier] Email sent for order {e.OrderId}");
        Console.WriteLine($"[EmailNotifier] Sender type: {sender?.GetType().Name}");
    }
}

public sealed class AuditLogger
{
    public void HandleOrderCompleted(object? sender, OrderCompletedEventArgs e)
    {
        Console.WriteLine($"[AuditLogger] Audit log written for order {e.OrderId} at {e.CompletedAt:HH:mm:ss}");
    }
}

public class Program
{
    public static void Main()
    {
        var orderService = new OrderService();
        var emailNotifier = new EmailNotifier();
        var auditLogger = new AuditLogger();

        orderService.OrderCompleted += emailNotifier.HandleOrderCompleted;
        orderService.OrderCompleted += auditLogger.HandleOrderCompleted;

        orderService.CompleteOrder("A100");

        Console.WriteLine();
        Console.WriteLine("Unsubscribe audit logger, then complete another order.");

        orderService.OrderCompleted -= auditLogger.HandleOrderCompleted;
        orderService.CompleteOrder("A101");
    }
}

// ============================================================
// 执行流程（execution flow）
// ============================================================
// 1. new OrderService() 创建发布者（publisher）。
// 2. new EmailNotifier() / new AuditLogger() 创建订阅者（subscribers）。
// 3. += 把 handler method 加入 OrderCompleted 的调用列表。
// 4. CompleteOrder("A100") 执行业务逻辑。
// 5. CompleteOrder 创建 OrderCompletedEventArgs。
// 6. CompleteOrder 调用 OnOrderCompleted(args)。
// 7. OnOrderCompleted 内部执行 OrderCompleted?.Invoke(this, e)。
// 8. 所有订阅者的 handler method 依次执行。
//
// 关键关系：
//   OrderCompleted = event member
//   EventHandler<OrderCompletedEventArgs> = delegate type
//   HandleOrderCompleted = event handler method
//   this = sender
//   e = event data
// ============================================================
