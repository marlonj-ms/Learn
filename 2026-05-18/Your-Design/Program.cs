using System;

// ============================================================
// 你的设计（your design）— 用真正可编译的语法写出来
// Your design — written in real compilable syntax
// ============================================================
//
// 角色映射（role mapping）:
//   P  = publisher                       发布者
//   A1 = business method on P            发布者的业务方法
//   S1, S2 = subscriber classes          订阅者类
//   B1, B2 = handler methods             处理方法
//   event1 = event member on P           事件成员
//   A1CompletedEventArgs = payload       事件数据
// ============================================================

namespace YourDesignDemo;

// ─── 1. 事件数据类（event args class） ───────────────────────
public sealed class A1CompletedEventArgs : EventArgs
{
    public string   OrderId    { get; }
    public DateTime FinishedAt { get; }

    public A1CompletedEventArgs(string orderId, DateTime finishedAt)
    {
        OrderId    = orderId;
        FinishedAt = finishedAt;
    }
}

// ─── 2. P (publisher) ────────────────────────────────────────
public class P
{
    // event 的"模板"就是 EventHandler<A1CompletedEventArgs>
    // 它规定了:
    //   - 返回 void
    //   - 第一个参数: object? sender
    //   - 第二个参数: A1CompletedEventArgs e
    public event EventHandler<A1CompletedEventArgs>? event1;

    // A1 = 业务方法（business method）
    public void A1(string orderId)
    {
        // (a) 做业务工作
        Console.WriteLine($"[P.A1] doing business work for {orderId}");

        // (b) 创建事件数据
        var args = new A1CompletedEventArgs(orderId, DateTime.Now);

        // (c) 通知所有订阅者
        event1?.Invoke(this, args);

        // (d) 业务工作的最后一步（如果有）
        Console.WriteLine($"[P.A1] A1 finished for {orderId}");
    }
}

// ─── 3. S1 + S2 (subscribers) ────────────────────────────────
public class S1
{
    public void B1(object? sender, A1CompletedEventArgs e)
    {
        Console.WriteLine($"  [S1.B1] reacted to order {e.OrderId} at {e.FinishedAt:HH:mm:ss}");
    }
}

public class S2
{
    public void B2(object? sender, A1CompletedEventArgs e)
    {
        Console.WriteLine($"  [S2.B2] reacted to order {e.OrderId} at {e.FinishedAt:HH:mm:ss}");
    }
}

// ─── 4. Main — wiring + trigger ──────────────────────────────
public class Program
{
    public static void Main()
    {
        var p  = new P();
        var s1 = new S1();
        var s2 = new S2();

        // PHASE 1: wire (registration)
        p.event1 += s1.B1;
        p.event1 += s2.B2;

        Console.WriteLine("=== First call: both subscribed ===");
        // PHASE 2: trigger
        p.A1("A-100");

        Console.WriteLine();
        Console.WriteLine("=== Unsubscribe S2.B2, then call again ===");
        p.event1 -= s2.B2;
        p.A1("A-101");

        Console.WriteLine();
        Console.WriteLine("=== No subscribers anymore ===");
        p.event1 -= s1.B1;
        p.A1("A-102");   // ?.Invoke handles the null case silently
    }
}
