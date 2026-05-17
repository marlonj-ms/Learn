# Events, Virtual, Sealed Summary - May 17, 2026

## Today's Focus

今天继续学习 事件（event），重点从基础 event 进入 .NET 标准事件模式（standard .NET event pattern）。

同时补充了理解事件触发方法时需要的继承知识：继承（inheritance）、virtual、override、sealed、base。

---

## 1. EventHandler<TEventArgs> 标准事件模式

标准事件写法：

```csharp
public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;
```

拆解：

```text
EventHandler<OrderCompletedEventArgs> = 委托类型（delegate type）
OrderCompleted = 事件成员（event member）
```

事件处理方法（event handler method）通常长这样：

```csharp
private static void OnOrderCompleted(object? sender, OrderCompletedEventArgs e)
{
    Console.WriteLine(e.OrderId);
}
```

参数含义：

```text
sender = 事件发送者（event sender），通常是触发事件的对象
e      = 事件数据对象（event data object / event args）
```

---

## 2. sender 和 e

事件通常这样触发：

```csharp
OrderCompleted?.Invoke(this, args);
```

含义：

```text
this = 当前对象实例，例如当前 OrderService 对象
args = 事件数据对象，类型通常是 OrderCompletedEventArgs
```

如果在 `OrderService` 内部调用：

```csharp
OrderCompleted?.Invoke(this, args);
```

那么：

```text
sender = 当前 OrderService 实例
 e     = args，也就是 OrderCompletedEventArgs 对象
```

---

## 3. EventArgs 自定义事件数据类

例子：

```csharp
public sealed class OrderCompletedEventArgs : EventArgs
{
    public string OrderId { get; }

    public OrderCompletedEventArgs(string orderId)
    {
        OrderId = orderId;
    }
}
```

理解：

```text
OrderCompletedEventArgs 是事件数据类（event args class）。
它通过构造函数接收 orderId。
它通过 OrderId 属性把数据提供给订阅者。
```

---

## 4. Event、Handler、+= 的关系

例子：

```csharp
orderService.OrderCompleted += emailNotifier.HandleOrderCompleted;
```

你的正确理解：

```text
OrderCompleted = event / 事件成员（event member）
HandleOrderCompleted = event handler method / 事件处理方法
+= = 把 handler method 订阅/register 到事件调用列表中
```

重要纠正：

```text
HandleOrderCompleted 不是一个新 event。
它是订阅到 event 上的 handler method。
```

---

## 5. OnOrderCompleted 事件触发方法

生产代码常见模式：

```csharp
protected virtual void OnOrderCompleted(OrderCompletedEventArgs e)
{
    OrderCompleted?.Invoke(this, e);
}
```

理解：

```text
CompleteOrder = 业务动作（business action）
OnOrderCompleted = 事件触发方法（event raising method）
```

这样可以把两件事分开：

```text
1. CompleteOrder 完成订单业务逻辑。
2. OnOrderCompleted 通知订阅者事件发生了。
```

---

## 6. 继承（Inheritance）

例子：

```csharp
public class OrderService
{
}

public class PriorityOrderService : OrderService
{
}
```

含义：

```text
OrderService = 基类（base class）/ 父类（parent class）
PriorityOrderService = 派生类（derived class）/ 子类（child class）
: OrderService = inherits from OrderService
```

重要规则：

```text
Every PriorityOrderService is an OrderService.
But not every OrderService is a PriorityOrderService.
```

所以：

```csharp
OrderService service1 = new PriorityOrderService(); // OK
PriorityOrderService service2 = new OrderService(); // Compile error
```

---

## 7. Variable Type vs Real Object Type

例子：

```csharp
OrderService service = new PriorityOrderService();
```

拆解：

```text
variable type = OrderService
real object type = PriorityOrderService
```

如果方法是 virtual 并且被 override：

```csharp
service.CompleteOrder("P100");
```

实际运行的是：

```text
PriorityOrderService.CompleteOrder
```

这叫 多态（polymorphism）。

记忆句：

```text
variable type decides what members you are allowed to call.
real object type decides which overridden method actually runs.
```

---

## 8. virtual 和 override

例子：

```csharp
public class OrderService
{
    public virtual void CompleteOrder(string orderId)
    {
        Console.WriteLine("Base complete");
    }
}

public class PriorityOrderService : OrderService
{
    public override void CompleteOrder(string orderId)
    {
        Console.WriteLine("Priority complete");
    }
}
```

含义：

```text
virtual = base class allows child classes to override this method
override = child class provides its own implementation
```

注意用词：

```text
C# 里说 override / 重写，不说 overwrite。
```

---

## 9. sealed class

例子：

```csharp
public sealed class OrderService
{
}
```

含义：

```text
sealed class = no other class can inherit from this class
```

错误例子：

```csharp
public class BookOrderService : OrderService
{
}
```

如果 `OrderService` 是 sealed class，上面会编译错误。

生产理解：

```text
sealed class means inheritance stops here.
```

因为 sealed class 不能被继承，所以通常不需要 `protected virtual` 方法。

---

## 10. sealed override

`sealed override` 是方法层级（method level），不是类层级。

例子：

```csharp
public class OrderService
{
    public virtual void CompleteOrder()
    {
    }
}

public class PriorityOrderService : OrderService
{
    public sealed override void CompleteOrder()
    {
    }
}
```

含义：

```text
OrderService allows overriding.
PriorityOrderService overrides the method, but seals it here.
Future child classes can inherit from PriorityOrderService,
but they cannot override CompleteOrder again.
```

对比：

```text
sealed class = stop inheritance of the whole class
sealed override = stop further overriding of one method
```

---

## 11. Nested Class vs Inheritance

嵌套类（nested class）是写在另一个类内部的类。

```csharp
public sealed class Outer
{
    public class Inner
    {
    }
}
```

理解：

```text
Inner is declared inside Outer.
Inner is not inheriting from Outer.
```

继承必须使用冒号：

```csharp
public class Child : Outer
{
}
```

所以：

```text
: Outer = inheritance
inside Outer { } = nested declaration
```

sealed 阻止继承，但不阻止写 nested class。

---

## 12. base

`base` 表示当前对象里的 父类部分（base class part）。

例子：

```csharp
public class BookOrderService : OrderService
{
    public override void CompleteOrder(string orderId)
    {
        Console.WriteLine("Book logic");
        base.CompleteOrder(orderId);
    }
}
```

含义：

```text
base.CompleteOrder(orderId) = call the parent class version of CompleteOrder
```

重要纠正：

```text
base does not create a new parent object.
base means access the parent-class behavior of the current object.
```

记忆模型：

```text
this = current object as the current class
base = current object viewed through its parent class behavior
```

---

## 13. Event + Inheritance Connection

如果类不是 sealed：

```csharp
public class OrderService
{
    public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

    protected virtual void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        OrderCompleted?.Invoke(this, e);
    }
}
```

原因：

```text
Because OrderService is not sealed, other classes can inherit from it.
A derived class may need to customize the event-raising behavior.
protected allows child classes to access the method.
virtual allows child classes to override the method.
```

子类可以这样：

```csharp
public class BookOrderService : OrderService
{
    protected override void OnOrderCompleted(OrderCompletedEventArgs e)
    {
        Console.WriteLine("Book-specific logic before event");
        base.OnOrderCompleted(e);
    }
}
```

`base.OnOrderCompleted(e)` 的作用：

```text
Call the parent event-raising method.
That parent method invokes OrderCompleted.
Then all subscribed event handlers run.
```

---

## Memory Diagrams

### Event flow

```text
orderService.CompleteOrder("A100")
    -> create OrderCompletedEventArgs
    -> call OnOrderCompleted(args)
    -> OrderCompleted?.Invoke(this, args)
    -> subscribed handler methods run
```

### Event roles

```text
OrderService                  = publisher / 发布者
OrderCompleted                = event member / 事件成员
OrderCompletedEventArgs       = event data / 事件数据
EmailNotifier.Handle...       = handler method / 事件处理方法
+=                            = subscribe handler / 订阅 handler
```

### Inheritance direction

```text
OrderService
    ^
    |
PriorityOrderService
```

```text
OrderService service = new PriorityOrderService(); // OK
PriorityOrderService service = new OrderService(); // Not OK
```

### virtual dispatch

```text
variable type:    OrderService
real object type: PriorityOrderService
method called:    overridden PriorityOrderService method
```

---

## Runnable Files Created Today

- `Events-Standard-Pattern.cs`
- `Events-Standard-Pattern.csproj`
- `Virtual-Sealed-Basics/Program.cs`
- `Virtual-Sealed-Basics/Virtual-Sealed-Basics.csproj`

Commands:

```powershell
dotnet run --project .\2026-05-17\Events-Standard-Pattern.csproj
```

```powershell
dotnet run --project .\2026-05-17\Virtual-Sealed-Basics\Virtual-Sealed-Basics.csproj
```

---

## Review Questions For Next Day

### EventHandler<TEventArgs>

1. What is `EventHandler<OrderCompletedEventArgs>`?
2. What is `OrderCompleted` in this line?

```csharp
public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;
```

3. In an event handler, what does `sender` mean?
4. In an event handler, what does `e` mean?
5. What does this line do?

```csharp
OrderCompleted?.Invoke(this, args);
```

### Event Handler Subscription

6. What is `HandleOrderCompleted` in this line?

```csharp
orderService.OrderCompleted += emailNotifier.HandleOrderCompleted;
```

7. What does `+=` do when used with an event?
8. Is `HandleOrderCompleted` a new event or a handler method?

### Event Raising Method

9. Why do production classes often have an `OnOrderCompleted(...)` method?
10. What is the difference between `CompleteOrder(...)` and `OnOrderCompleted(...)`?
11. Why might `OnOrderCompleted(...)` be `protected virtual` in an inheritable class?
12. Why might it be `private` in a sealed class?

### Inheritance

13. What does this syntax mean?

```csharp
public class PriorityOrderService : OrderService
{
}
```

14. Which one is the base class and which one is the derived class?
15. Why is this valid?

```csharp
OrderService service = new PriorityOrderService();
```

16. Why is this invalid?

```csharp
PriorityOrderService service = new OrderService();
```

17. Complete the sentence: Not every `OrderService` is a ______.

### virtual / override / polymorphism

18. What does `virtual` mean?
19. What does `override` mean?
20. In this line, what is the variable type and what is the real object type?

```csharp
OrderService service = new PriorityOrderService();
```

21. If `CompleteOrder` is virtual and overridden, which version runs?
22. What is polymorphism?

### sealed

23. What does `public sealed class OrderService` mean?
24. Why does `protected virtual` usually not make sense inside a sealed class?
25. What is the difference between `sealed class` and `sealed override`?
26. Can a sealed class contain a nested class?
27. Does a nested class inherit from the outer class automatically?

### base

28. What does `base.CompleteOrder(orderId)` mean?
29. Does `base` create a new parent object?
30. Why should an override sometimes call `base.OnOrderCompleted(e)`?
31. If the child override does not call `base.OnOrderCompleted(e)`, what may happen to event subscribers?
