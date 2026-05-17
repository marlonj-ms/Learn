using System;
using System.Collections.Generic;
using System.Linq;

// Note: This file contains C# code (despite the .c extension).

{
    var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    foreach (var number in numbers)
    {
        if (number % 2 == 0)
        {
            Console.WriteLine(number);
        }
    }
}

{
    var numbers = new List<int> { 1, 2, 3, 4, 5 };

    var result = numbers.Select(n =>
    {
        Console.WriteLine($"Processing {n}"); // side effect to prove when it runs
        return n * 10;
    });

    Console.WriteLine("--- Select was called, but watch: ---");

    foreach (var item in result)
    {
        Console.WriteLine($"Got: {item}");
    }
}

{
    var employees = new List<string>
    {
        "Alice", "Bob", "Charlie", "Anna", "Brian", "Catherine", "Adam", "Ben"
    };

    var result = employees
        .Where(n => n.Length > 3)
        .GroupBy(m => m[0])
        .OrderBy(g => g.Key);

    foreach (var groupuser in result)
    {
        Console.WriteLine($"Group: {groupuser.Key}");
        foreach (var user in groupuser)
        {
            Console.WriteLine($" - {user}");
        }
    }
}