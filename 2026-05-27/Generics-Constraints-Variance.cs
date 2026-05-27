using System;
using System.Collections.Generic;

namespace GenericsDeepDive;

public interface IAuditable
{
    DateTime CreatedAt { get; }
}

public class Order : IAuditable
{
    public required string Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public decimal Amount { get; init; }

    public override string ToString() => $"Order(Id={Id}, Amount={Amount})";
}

public class Repository<T> where T : class, IAuditable//, new()
{
    private readonly List<T> _items = new();

    public void Add(T item)
    {
        _items.Add(item);
    }

    public IReadOnlyList<T> All() => _items;

    //public T CreateDefault()
    //{
        // new() constraint allows parameterless construction.
    //    return new T();
    //}
}

public interface IProducer<out T>
{
    T Produce();
}

public interface IConsumer<in T>
{
    void Consume(T value);
}

public class StringProducer : IProducer<string>
{
    public string Produce() => "hello from producer";
}

public class ObjectConsumer : IConsumer<object>
{
    public void Consume(object value)
    {
        Console.WriteLine($"[ObjectConsumer] {value}");
    }
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Generics: Constraints + Variance ===");

        var repo = new Repository<Order>();
        repo.Add(new Order { Id = "A-100", Amount = 88.8m });
        repo.Add(new Order { Id = "A-101", Amount = 128.5m });

        foreach (var order in repo.All())
        {
            Console.WriteLine($"Stored: {order} | CreatedAt={order.CreatedAt:O}");
        }

        // Covariance: IProducer<string> can be used as IProducer<object> because of out T.
        IProducer<object> producer = new StringProducer();
        object text = producer.Produce();
        Console.WriteLine($"Covariance output: {text}");

        // Contravariance: IConsumer<object> can be used as IConsumer<string> because of in T.
        IConsumer<string> consumer = new ObjectConsumer();
        consumer.Consume("contravariance in action");

        Console.WriteLine();
        Console.WriteLine("=== Mini Drill ===");
        Console.WriteLine("1) Add Customer : IAuditable and store it with Repository<Customer>.");
        Console.WriteLine("2) Try removing new() from Repository constraints and see what breaks.");
        Console.WriteLine("3) Explain why covariance works for producer but not consumer.");
    }
}
