using System;
using System.Collections.Generic;

public class NumberBag
{
    private List<int> _numbers = new List<int>();

    public void Add(int x)
    {
        _numbers.Add(x);
    }

    public int Sum()
    {
        int sum = 0;
        foreach (int m in _numbers)
        { sum = sum + m; }
        return sum;
    }

    public void PrintAll()
    {
        foreach (int y in _numbers)
        {
            Console.WriteLine($"{y}");
        }
    }
}

public class TestProgram
{
    public static void Main()
    {
        NumberBag n = new NumberBag();
        n.Add(10);
        n.Add(20);
        n.Add(30);
        n.PrintAll();
        Console.WriteLine($"{n.Sum()}");
    }
}
