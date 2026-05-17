using System;
using System.Collections.Generic;
using System.Linq;

// Delegates Fundamentals (runnable in a Console App)
// Copy this into Program.cs of a console project, or include it in a project that supports top-level statements.

Console.WriteLine("=== 1) A delegate is a variable that can hold a method ===");

int AddOne(int number)
{
    return number + 1;
}

// Func<int, int> means: a method/lambda that takes an int and returns an int.
Func<int, int> operation = AddOne;

Console.WriteLine($"operation(5) = {operation(5)}");

operation = number => number * 10;
Console.WriteLine($"operation(5) after reassignment = {operation(5)}");

Console.WriteLine();
Console.WriteLine("=== 2) Passing behavior into a method ===");

int ApplyOperation(int value, Func<int, int> transform)
{
    return transform(value);
}

Console.WriteLine($"ApplyOperation(4, AddOne) = {ApplyOperation(4, AddOne)}");
Console.WriteLine($"ApplyOperation(4, n => n * n) = {ApplyOperation(4, n => n * n)}");

Console.WriteLine();
Console.WriteLine("=== 3) Delegates make code extensible ===");

List<int> prices = new() { 100, 250, 500 };

IEnumerable<int> ApplyDiscount(IEnumerable<int> originalPrices, Func<int, int> discountRule)
{
    foreach (int price in originalPrices)
    {
        yield return discountRule(price);
    }
}

var tenPercentOff = ApplyDiscount(prices, price => price - (price / 10));
var flatTwentyOff = ApplyDiscount(prices, price => price - 20);

Console.WriteLine("10% off: " + string.Join(", ", tenPercentOff));
Console.WriteLine("$20 off: " + string.Join(", ", flatTwentyOff));

Console.WriteLine();
Console.WriteLine("=== 4) Action<T> is for void-returning behavior ===");

void ProcessOrders(IEnumerable<int> orderIds, Action<int> onOrderProcessed)
{
    foreach (int orderId in orderIds)
    {
        Console.WriteLine($"Processing order {orderId}");
        onOrderProcessed(orderId);
    }
}

ProcessOrders(
    new[] { 101, 102, 103 },
    orderId => Console.WriteLine($"  Audit log: order {orderId} was processed"));

Console.WriteLine();
Console.WriteLine("=== 5) Predicate<T> is for true/false checks ===");

List<string> names = new() { "Alice", "Bob", "Charlie", "Anna" };

IEnumerable<string> FilterNames(IEnumerable<string> source, Predicate<string> rule)
{
    foreach (string name in source)
    {
        if (rule(name))
        {
            yield return name;
        }
    }
}

var namesStartingWithA = FilterNames(names, name => name.StartsWith("A"));
Console.WriteLine("Names starting with A: " + string.Join(", ", namesStartingWithA));

Console.WriteLine();
Console.WriteLine("=== 6) How this connects to LINQ ===");

var longNames = names
    .Where(name => name.Length > 3)
    .Select(name => name.ToUpper());

Console.WriteLine("LINQ result: " + string.Join(", ", longNames));

Console.WriteLine();
Console.WriteLine("Done.");
