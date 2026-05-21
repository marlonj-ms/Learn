# C# Core Syntax Foundations

Date: 2026-05-19

## Why This Course Exists

The learner understands many C# concepts conceptually, but needs more deliberate practice with basic code-writing mechanics.

This course focuses on code muscle memory:

- Where code is allowed to live
- How methods are declared and called
- When parameters are needed
- How classes and methods relate
- How `static` changes method calls
- How variables, fields, and parameters differ
- How `List<T>` is declared, filled, read, and passed around

## Course Name

`C# Core Syntax Foundations`

Short name: `Core Syntax`

## Learning Style

Small loop:

```text
one syntax idea -> tiny example -> learner explains -> correction -> tiny drill
```

No large runnable files until the learner can explain the syntax shape.

## Module 1 - Code Placement

Goal: Know where different C# members can legally be written.

Topics:

- Class member vs method body
- Regular method declaration belongs in a class/struct/record, not inside another regular method
- Local functions exist, but are a separate feature and not the beginner default
- Statements live inside methods
- Fields live inside classes, outside methods
- Parameters live in method signatures

Key model:

```text
class
  fields
  methods
    statements
    local variables
```

## Module 2 - Method Shape

Goal: Understand method signatures and calls.

Topics:

- Return type
- Method name
- Parameter list
- Method body
- `void` vs returning a value
- Argument vs parameter
- When a method needs parameters
- When a method should return a value

Key shape:

```csharp
returnType MethodName(parameterType parameterName)
{
    statements;
}
```

## Module 3 - Calling Relationships

Goal: Know how one method calls another.

Topics:

- Calling a method in the same class
- Calling a static method
- Calling an instance method through an object
- Why class ownership affects call syntax
- Why object state affects instance methods

Examples to practice later:

```csharp
HelperMethod();
Console.WriteLine("hello");
service.CompleteOrder("A-100");
```

## Module 4 - Variables, Fields, and Parameters

Goal: Know where data lives and how long it lives.

Topics:

- Local variable
- Parameter
- Field
- Static field
- Scope
- Lifetime

Key contrast:

```text
parameter = value received by a method
local variable = value created inside a method
field = value stored in an object or class
```

## Module 5 - List<T> Basics

Goal: Build fluency with common `List<T>` operations.

Topics:

- `List<int>` / `List<string>` declaration
- `new List<T>()`
- Collection initializer
- `Add`
- `Count`
- Index access
- `foreach`
- Passing a list as a parameter
- Returning a list from a method

Key examples:

```csharp
List<int> numbers = new List<int>();
numbers.Add(10);
int first = numbers[0];

foreach (int number in numbers)
{
    Console.WriteLine(number);
}
```

## Module 6 - Static vs Instance Basics

Goal: Understand why some calls use the class name and others use an object variable.

Topics:

- `static` method belongs to the class
- Instance method belongs to an object
- `Main` is static
- Static methods cannot directly use instance fields without an object

## Module 7 - Daily Micro Drills

Each drill should be tiny:

1. Write one class with one method.
2. Add one parameter.
3. Return one value.
4. Call one method from another method.
5. Move a variable from local variable to field and explain the difference.
6. Create a `List<T>` and loop through it.
7. Pass a `List<T>` to a method.
8. Return a `List<T>` from a method.

## Companion Reference

- `2026-05-19/CSharp-Brackets-And-Arrow-Reference.md` covers `<>`, `()`, `{}`, and `=>` in depth.

## Current First Lesson

Start with Module 1: Code Placement.

First question:

```text
In C#, what is the difference between a method declaration and a statement?
```
