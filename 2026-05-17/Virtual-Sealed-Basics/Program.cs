using System;

// ============================================================
// 2026-05-17 学习内容：virtual / sealed 基础
// ============================================================
// 今天目标：
//   1. virtual = 允许子类重写（override）
//   2. sealed class = 禁止别人继承这个类
//   3. sealed override = 允许继承类，但禁止继续重写这个方法
//
// 和事件的关系：
//   如果类可以被继承，事件触发方法常写成 protected virtual。
//   如果类是 sealed，事件触发方法通常 private 就够了。
// ============================================================

namespace VirtualSealedBasics;

public sealed class OrderCompletedEventArgs : EventArgs
{
    public string OrderId { get; }

    public OrderCompletedEventArgs(string orderId)
    {
        OrderId = orderId;
    }
}

// ============================================================
// 第 1 部分：可继承类 + protected virtual 方法
// ============================================================
public class InheritableOrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[{GetType().Name}] Completing order {orderId}");

        var eventArgs = new OrderCompletedEventArgs(orderId);
        OnOrderCompleted(eventArgs);
    }

    // protected = 子类可以访问，外部对象不能访问。
    // virtual   = 子类可以 override 这个方法。
    protected virtual void OnOrderCompleted(OrderCompletedEventArgs eventArgs)
    {
        Console.WriteLine($"[{GetType().Name}] Base event raising method runs.");
        OrderCompleted?.Invoke(this, eventArgs);
    }
}

// ============================================================
// 第 2 部分：override virtual 方法
// ============================================================
public class PriorityOrderService : InheritableOrderService
{
    protected override void OnOrderCompleted(OrderCompletedEventArgs eventArgs)
    {
        Console.WriteLine($"[{GetType().Name}] Priority-specific logic runs before subscribers are notified.");

        // base.OnOrderCompleted(...) = 调用父类原本的事件触发逻辑。
        base.OnOrderCompleted(eventArgs);
    }
}

// ============================================================
// 第 3 部分：sealed override
// ============================================================
public class AuditLockedOrderService : InheritableOrderService
{
    protected sealed override void OnOrderCompleted(OrderCompletedEventArgs eventArgs)
    {
        Console.WriteLine($"[{GetType().Name}] Required audit logic runs and cannot be overridden further.");
        base.OnOrderCompleted(eventArgs);
    }
}

// This would be allowed because AuditLockedOrderService is not a sealed class:
public class ChildAuditLockedOrderService : AuditLockedOrderService
{
    // But this would be a compile error because OnOrderCompleted is sealed override:
    // protected override void OnOrderCompleted(OrderCompletedEventArgs eventArgs)
    // {
    // }
}

// ============================================================
// 第 4 部分：sealed class
// ============================================================
public sealed class FinalOrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[{GetType().Name}] Completing order {orderId}");

        var eventArgs = new OrderCompletedEventArgs(orderId);
        OnOrderCompleted(eventArgs);
    }

    // 因为 FinalOrderService 是 sealed class，不会有子类。
    // 所以这里不需要 protected virtual，private 就够了。
    private void OnOrderCompleted(OrderCompletedEventArgs eventArgs)
    {
        Console.WriteLine($"[{GetType().Name}] Private event raising method runs.");
        OrderCompleted?.Invoke(this, eventArgs);
    }
}

// This would be a compile error because FinalOrderService is sealed:
// public class CannotInheritFinalOrderService : FinalOrderService
// {
// }

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== virtual + override ===");
        var priorityService = new PriorityOrderService();
        priorityService.OrderCompleted += HandleOrderCompleted;
        priorityService.CompleteOrder("P100");

        Console.WriteLine();
        Console.WriteLine("=== sealed override ===");
        var auditLockedService = new AuditLockedOrderService();
        auditLockedService.OrderCompleted += HandleOrderCompleted;
        auditLockedService.CompleteOrder("A200");

        Console.WriteLine();
        Console.WriteLine("=== sealed class ===");
        var finalService = new FinalOrderService();
        finalService.OrderCompleted += HandleOrderCompleted;
        finalService.CompleteOrder("F300");
    }

    private static void HandleOrderCompleted(object? sender, OrderCompletedEventArgs eventArgs)
    {
        Console.WriteLine($"[Subscriber] Received order {eventArgs.OrderId} from {sender?.GetType().Name}");
    }
}

// ============================================================
// 记忆模型（memory model）
// ============================================================
// virtual:
//   父类说：子类可以改写这个方法。
//
// override:
//   子类说：我要改写父类允许我改写的方法。
//
// sealed class:
//   这个类到此为止，不能被继承。
//
// sealed override:
//   这个方法到此为止，子类不能继续 override 它。
//
// protected virtual OnOrderCompleted(...):
//   生产代码里常见，因为子类可能需要在事件触发前后加逻辑。
//
// private OnOrderCompleted(...):
//   sealed class 里常见，因为没有子类需要 override。
// ============================================================
