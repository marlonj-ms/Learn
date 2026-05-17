# Events Fundamentals Progress Summary — May 15, 2026

## Session Focus

今天在完成 delegate 深度学习后，开始进入 事件（event）。重点不是一次讲完整个 event 系统，而是建立正确的第一层模型：

```text
delegate = 能存方法的类型/对象机制
event = 受限制的 delegate，提供受控订阅入口
```

明天继续学习 event，并且开场先做 event 考核。

---

## 1. public delegate field vs private delegate field vs event

今天最核心的问题：

> 如果把 delegate 设置成 private 不就可以了吗？为什么还需要 event？

对比：

```csharp
public Action<string>? OrderCompleted;        // 太危险
private Action<string>? orderCompleted;       // 太封闭
public event Action<string>? OrderCompleted;  // 刚好
```

### public delegate field 太危险

```csharp
public Action<string>? OrderCompleted;
```

外部可以做太多事：

```csharp
service.OrderCompleted = null;
service.OrderCompleted("fake");
service.OrderCompleted = SomeHandler;
```

风险：

- 外部可以清空所有订阅者。
- 外部可以覆盖原来的调用列表。
- 外部可以伪造事件触发。

### private delegate field 太封闭

```csharp
private Action<string>? orderCompleted;
```

它保护了字段，但外部不能直接订阅：

```csharp
service.orderCompleted += Handler; // 不允许
```

如果不用 event，就需要自己写：

```csharp
public void Subscribe(Action<string> handler)
{
    orderCompleted += handler;
}

public void Unsubscribe(Action<string> handler)
{
    orderCompleted -= handler;
}
```

### event 刚好

```csharp
public event Action<string>? OrderCompleted;
```

外部只允许：

```csharp
service.OrderCompleted += Handler;
service.OrderCompleted -= Handler;
```

外部不允许：

```csharp
service.OrderCompleted = null;
service.OrderCompleted("fake");
service.OrderCompleted?.Invoke("fake");
```

关键句：

> private delegate 太封闭，public delegate 太危险，event 正好提供“外部能订阅，但不能乱触发/清空”的权限边界。

---

## 2. event 是 class 里的 event member

今天澄清了一个重要成员模型：

```csharp
public event Action<string>? OrderCompleted;
```

它不是 property，也不是普通 variable。它是 class 里的 事件成员（event member）。

对比：

```csharp
public class OrderService
{
    private int count;                         // field
    public string Name { get; set; }           // property
    public void CompleteOrder(string orderId)  // method
    {
    }
    public event Action<string>? OrderCompleted; // event member
}
```

重要规则：

```text
Action<string> 是类型。
Action<string> x 写在方法里：x 是局部变量（local variable）。
Action<string> x 写在 class 里：x 是字段（field）。
event Action<string> x 写在 class 里：x 是事件成员（event member）。
event 不能写在方法内部。
```

错误例子：

```csharp
public void Demo()
{
    event Action<string>? SomethingHappened; // 编译错误
}
```

---

## 3. event 的执行流程

例子：

```csharp
public class OrderService
{
    public event Action<string>? OrderCompleted;

    public void CompleteOrder(string orderId)
    {
        Console.WriteLine("Completing " + orderId);
        OrderCompleted?.Invoke(orderId);
    }
}
```

外部：

```csharp
var service = new OrderService();

service.OrderCompleted += id => Console.WriteLine("Email: " + id);
service.OrderCompleted += id => Console.WriteLine("Audit: " + id);

service.CompleteOrder("A100");
```

执行顺序：

```text
1. 创建 service。
2. 用 += 把两个 lambda 存进 OrderCompleted 的调用列表。
3. 调用 service.CompleteOrder("A100")。
4. CompleteOrder 内部执行 OrderCompleted?.Invoke("A100")。
5. Invoke 找到调用列表里的 lambda，并依次执行。
```

输出：

```text
Completing A100
Email: A100
Audit: A100
```

关键句：

> `+=` 只是把 lambda 存进事件的调用列表，不会立刻执行。真正执行发生在类内部调用 `Invoke` 时。

---

## 4. 匿名 lambda 取消订阅的坑

例子：

```csharp
alarm.Ring += () => Console.WriteLine("Wake up");
alarm.Ring -= () => Console.WriteLine("Wake up");
```

第二行不会移除第一行订阅的 lambda。

原因：两个 lambda 看起来一样，但它们是不同的委托实例。

正确写法：

```csharp
Action wakeUp = () => Console.WriteLine("Wake up");

alarm.Ring += wakeUp;
alarm.Ring -= wakeUp;
```

生产规则：

> 如果以后需要取消订阅，不要直接用匿名 lambda 订阅；保存 handler 或使用命名方法。

---

## 5. EventHandler<TEventArgs> 起步

今天开始接触标准 .NET 事件模式：

```csharp
public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;
```

已经明确：

```text
EventHandler<OrderCompletedEventArgs> = 委托类型（delegate type）
OrderCompleted = 事件成员（event member）
```

.NET 里 `EventHandler<TEventArgs>` 大致是：

```csharp
public delegate void EventHandler<TEventArgs>(
    object? sender,
    TEventArgs e
);
```

### Bracket mental model

```text
<>  放类型（type），比如 OrderCompletedEventArgs
()  放参数列表（parameter list），比如 object? sender, TEventArgs e
{}  放代码块 / class body
```

### 泛型替换

```csharp
EventHandler<OrderCompletedEventArgs>
```

表示：

```text
TEventArgs = OrderCompletedEventArgs
```

所以 handler 形状是：

```csharp
void Handler(object? sender, OrderCompletedEventArgs e)
{
}
```

或者 lambda：

```csharp
(sender, e) =>
{
}
```

---

## 6. Handler 是什么

今天澄清：

> handler 不是 C# 关键字，只是“事件发生时要执行的方法”。

命名方法：

```csharp
void PrintOrder(object? sender, OrderCompletedEventArgs e)
{
    Console.WriteLine(e.OrderId);
}

service.OrderCompleted += PrintOrder;
```

匿名 lambda：

```csharp
service.OrderCompleted += (sender, e) =>
{
    Console.WriteLine(e.OrderId);
};
```

这两种都是 handler。

---

## 7. sender 和 e

标准事件触发：

```csharp
OrderCompleted?.Invoke(this, new OrderCompletedEventArgs(orderId));
```

含义：

```text
sender = this，也就是触发事件的对象
e = 一个 EventArgs 对象，里面装事件数据
```

如果：

```csharp
orderId = "A100"
```

则：

```csharp
var eventData = new OrderCompletedEventArgs("A100");
OrderCompleted?.Invoke(this, eventData);
```

handler 里：

```text
sender = service 对象
e = eventData 对象
e.OrderId = "A100"
```

重要修正：

> `e` 收到的不是字符串 `"A100"` 本身，而是一个 `OrderCompletedEventArgs` 对象；这个对象的 `OrderId` 属性是 `"A100"`。

---

## 8. Event flow diagram

例子：

```csharp
public class Downloader
{
    public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

    public void Download()
    {
        var data = new DownloadCompletedEventArgs("report.pdf");
        DownloadCompleted?.Invoke(this, data);
    }
}
```

外部：

```csharp
var downloader = new Downloader();

downloader.DownloadCompleted += (sender, e) =>
{
    Console.WriteLine("Downloaded: " + e.FileName);
};

downloader.Download();
```

流程：

```text
Step 1: 创建对象
var downloader = new Downloader();

Downloader 对象内部有事件入口 DownloadCompleted。
调用列表目前为空。

Step 2: 订阅事件
downloader.DownloadCompleted += lambda;

把 lambda 存进 DownloadCompleted 的调用列表。
此时 lambda 没有执行。

Step 3: 调用 Download()
downloader.Download();

进入 Downloader.Download()。

Step 4: 创建事件数据
var data = new DownloadCompletedEventArgs("report.pdf");

data.FileName = "report.pdf"。

Step 5: 触发事件
DownloadCompleted?.Invoke(this, data);

从调用列表里找到所有订阅者，并依次调用。

Step 6: 执行 lambda
sender = downloader
e = data
e.FileName = "report.pdf"

Step 7: 输出
Downloaded: report.pdf
```

关键模型：

```text
Downloader 定义事件入口。
外部 += 把处理方法存进去。
Download() 内部 Invoke 触发事件。
Invoke 把 sender 和 data 传给处理方法。
处理方法执行。
```

---

## Key Takeaways

1. event 不是普通变量，也不是 property；它是 class 里的 event member。
2. `Action<T>` / `Func<T>` / custom delegate 名字是类型，用在哪里才决定是 local variable、field、parameter 还是 event。
3. event 只能写在 class/struct/interface 这类类型里面，不能写在方法内部。
4. `+=` 只是订阅，把 handler 存进调用列表，不会马上执行。
5. `Invoke(...)` 才会真正执行调用列表里的 handler。
6. `EventHandler<TEventArgs>` 是 .NET 标准事件委托类型。
7. `sender` 表示谁触发事件，通常传 `this`。
8. `e` 是事件数据对象，不是裸字符串/数字本身。
9. class 定义事件入口，外部定义收到事件后要做什么。

---

## Tomorrow Start: Event Quiz

明天开始先考核以下内容，再继续学习 event。

### Quiz 1: public/private/event

1. 为什么 `public Action<string>? OrderCompleted;` 危险？
2. 为什么 `private Action<string>? orderCompleted;` 太封闭？
3. `public event Action<string>? OrderCompleted;` 外部允许哪两个操作？禁止哪些操作？

### Quiz 2: member placement

4. `Action<string> x` 写在方法内部时，`x` 是什么？
5. `Action<string> x` 写在 class 内、method 外时，`x` 是什么？
6. `event Action<string> x` 写在 class 内时，`x` 是什么？
7. event 能不能写在方法内部？为什么？

### Quiz 3: event execution

8. `service.OrderCompleted += handler;` 会不会立刻执行 handler？
9. handler 什么时候真正执行？
10. `OrderCompleted?.Invoke(orderId);` 做了什么？

### Quiz 4: EventHandler<TEventArgs>

11. 在 `public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;` 里，哪个是 delegate type？哪个是 event member？
12. `EventHandler<TEventArgs>` 要求 handler 接收几个参数？
13. `sender` 是什么？
14. `e` 是什么？
15. 如果 `DownloadCompleted?.Invoke(this, data);`，handler 里的 `sender` 和 `e` 分别收到什么？

### Quiz 5: unsubscribe

16. 为什么下面代码移除不了第一次订阅的 lambda？

```csharp
alarm.Ring += () => Console.WriteLine("Wake up");
alarm.Ring -= () => Console.WriteLine("Wake up");
```

17. 如果以后需要取消订阅，应该怎么写？

---

## Next Lesson

Tomorrow:

```text
1. Event quiz first
2. Review weak points
3. Continue EventHandler<TEventArgs>
4. Learn OnEventName pattern
5. Practice with a runnable event example
```
