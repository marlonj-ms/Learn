using System;
using System.Collections.Generic;
using System.Linq;

// Lambda Fundamentals (runnable in a Console App)
// - Copy into Program.cs of a new console project, or include this file in any project.

Console.WriteLine("=== 1) Lambdas = inline functions ===");

// Expression lambda: input => expression
Func<int, int> timesTen = n => n * 10;
Console.WriteLine($"timesTen(7) = {timesTen(7)}");

// Statement lambda: input => { statements; return ...; }
Func<int, int> timesTenWithLogging = n =>
{
    Console.WriteLine($"  computing {n} * 10");
    return n * 10;
};
Console.WriteLine($"timesTenWithLogging(3) = {timesTenWithLogging(3)}");

// Action = void-returning lambda
Action<string> print = message => Console.WriteLine(message);
print("Action<string> called");

Console.WriteLine();
Console.WriteLine("=== 2) LINQ uses lambdas heavily (Where/Select/GroupBy) ===");

var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Where: Func<T, bool>
var evens = numbers.Where(n => n % 2 == 0);
Console.WriteLine("Evens: " + string.Join(", ", evens));

// Select: Func<T, TResult>
var tens = numbers.Select(n => n * 10);
Console.WriteLine("x10:   " + string.Join(", ", tens));

Console.WriteLine();
Console.WriteLine("=== 3) Deferred execution (important mental model) ===");

var deferred = numbers.Select(n =>
{
    Console.WriteLine($"Processing {n}"); // side effect to prove WHEN it runs
    return n * 10;
});

Console.WriteLine("Select() has been called, but nothing executed yet.");
Console.WriteLine("Enumerating now:");
foreach (var item in deferred)
{
    Console.WriteLine($"Got: {item}");
}

Console.WriteLine();
Console.WriteLine("=== 4) Fixing your employee example (Length, GroupBy, OrderBy) ===");

var employees = new List<string>
{
    "Alice", "Bob", "Charlie", "Anna", "Brian", "Catherine", "Adam", "Ben"
};

// Your intent looked like:
// 1) filter names longer than 3
// 2) group by first letter
// 3) order groups by key (letter)
var grouped = employees
    .Where(name => name.Length > 3)
    .GroupBy(name => name[0])
    .OrderBy(group => group.Key);

foreach (var group in grouped)
{
    Console.WriteLine($"Group: {group.Key}");
    foreach (var user in group.OrderBy(x => x))
    {
        Console.WriteLine($" - {user}");
    }
}

Console.WriteLine();
Console.WriteLine("=== 5) Closures: lambdas can capture variables ===");

int threshold = 3;
var longerThanThreshold = employees.Where(name => name.Length > threshold);
Console.WriteLine($"Threshold={threshold} => {string.Join(", ", longerThanThreshold)}");

threshold = 5;
Console.WriteLine($"Threshold={threshold} => {string.Join(", ", longerThanThreshold)}");

Console.WriteLine();
Console.WriteLine("Done.");
