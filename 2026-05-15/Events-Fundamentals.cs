using System;

// ============================================================
// 2026-05-15 学习内容：事件基础（Events Fundamentals）
// ============================================================
// 前置知识：delegate 是能存方法的类型。
//
// 今天 event 的核心问题：
//   1. public delegate 字段为什么危险？
//   2. private delegate 字段为什么又太封闭？
//   3. event 为什么刚好？
//
// 一句话：
//   event = 受限制的 delegate。
//   外部只能 += 订阅、-= 取消订阅。
//   外部不能 = null、不能覆盖、不能直接 Invoke。
// ============================================================

namespace EventsFundamentals;

// ============================================================
// 第 1 部分：public delegate 字段 — 太危险
// ============================================================
public class DangerousOrderService
{
    // 这是一个 public delegate 字段，不是 event。
    // 外部权限太大。
    public Action<string>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[Dangerous] Completing order {orderId}");
        OrderCompleted?.Invoke(orderId);
    }
}

// ============================================================
// 第 2 部分：private delegate 字段 — 太封闭
// ============================================================
public class TooPrivateOrderService
{
    // private 可以保护字段，但外部完全不能订阅。
    private Action<string>? orderCompleted;

    // 如果不用 event，就需要自己写 Subscribe / Unsubscribe。
    public void Subscribe(Action<string> handler)
    {
        orderCompleted += handler;
    }

    public void Unsubscribe(Action<string> handler)
    {
        orderCompleted -= handler;
    }

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[TooPrivate] Completing order {orderId}");
        orderCompleted?.Invoke(orderId);
    }
}

// ============================================================
// 第 3 部分：event — 刚好的权限边界
// ============================================================
public class OrderService
{
    // event 的底层仍然基于 delegate。
    // 但外部只能 += 和 -=。
    public event Action<string>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine($"[Event] Completing order {orderId}");

        // 只有类内部可以触发事件。
        OrderCompleted?.Invoke(orderId);
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== 1) public delegate field is dangerous ===");

        var dangerous = new DangerousOrderService();
        dangerous.OrderCompleted += SendEmail;
        dangerous.OrderCompleted += WriteAuditLog;

        dangerous.CompleteOrder("A100");

        Console.WriteLine();
        Console.WriteLine("外部可以清空所有订阅者：");
        dangerous.OrderCompleted = null;       // 这就是危险点
        dangerous.CompleteOrder("A101");       // 没有人收到通知

        Console.WriteLine();
        Console.WriteLine("外部甚至可以假装订单完成：");
        dangerous.OrderCompleted = SendEmail;
        dangerous.OrderCompleted("FAKE-ORDER"); // 外部直接触发，危险

        Console.WriteLine();
        Console.WriteLine("=== 2) private delegate field is protected but too closed ===");

        var tooPrivate = new TooPrivateOrderService();
        tooPrivate.Subscribe(SendEmail);
        tooPrivate.Subscribe(WriteAuditLog);
        tooPrivate.CompleteOrder("B200");
        tooPrivate.Unsubscribe(WriteAuditLog);
        tooPrivate.CompleteOrder("B201");

        Console.WriteLine();
        Console.WriteLine("=== 3) event gives the right boundary ===");

        var service = new OrderService();

        // 外部可以订阅。
        service.OrderCompleted += SendEmail;
        service.OrderCompleted += WriteAuditLog;

        service.CompleteOrder("C300");

        // 外部可以取消订阅。
        service.OrderCompleted -= WriteAuditLog;
        service.CompleteOrder("C301");

        // 下面这些如果取消注释，会编译错误：
        // service.OrderCompleted = null;
        // service.OrderCompleted("FAKE-ORDER");
        // service.OrderCompleted?.Invoke("FAKE-ORDER");

        Console.WriteLine();
        Console.WriteLine("Done.");
    }

    static void SendEmail(string orderId)
    {
        Console.WriteLine($"  Email sent for order {orderId}");
    }

    static void WriteAuditLog(string orderId)
    {
        Console.WriteLine($"  Audit log written for order {orderId}");
    }
}

// ============================================================
// 复习题
// ============================================================
// Q1. public Action<string>? OrderCompleted; 为什么危险？
// Q2. private Action<string>? orderCompleted; 为什么太封闭？
// Q3. event 允许外部做哪两个操作？
// Q4. event 禁止外部做哪些操作？
// Q5. 为什么只有类内部应该能 Invoke 一个事件？
// Q6. event 和 delegate 的关系是什么？
