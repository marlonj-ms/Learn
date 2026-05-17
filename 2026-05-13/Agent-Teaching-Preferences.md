# Agent Teaching Preferences — May 13, 2026

## Explanation Style

- When explaining C# concepts, write important technical nouns in Chinese plus English.
- Preferred format: `中文术语（English term）`.
- Example: `泛型委托类型（generic delegate type）`, `返回值类型（return type）`, `参数类型（parameter type）`, `委托变量（delegate variable）`.
- The learner prefers this because English technical terms help connect C# syntax with official documentation and real production code.

## Callback Teaching Note

- When teaching 回调（callback）, start from the ordinary parameter mental model: passing a value vs passing an action.
- Avoid introducing result object / Result Pattern too early; first make clear that the method internally invokes the delegate passed from outside.
- Preferred phrasing: callback is not a result automatically flowing back out; it is the method actively calling the function/action that was passed in.

## Quiz Preference

- The learner found this delegate quiz especially useful because it covers delegate type, method signature, delegate variable/object, target/method reference, and invocation result in one compact example:

```csharp
public delegate int MathOperation(int a, int b);

int Add(int a, int b) => a + b;

MathOperation op = Add;
int result = op(5, 6);
```

## Event Teaching Note

- Reinforce member placement when teaching events: `Action<T>` / `Func<T>` / custom delegate names are types, not fields by themselves.
- If declared inside a method, `Action<T> x` is a local variable. If declared inside a class but outside methods, `Action<T> x` is a field. If declared as `event Action<T> x` inside a class, it is an event member.
- `event` cannot be declared inside a method; it is a type member used as a long-lived subscription entry point on an object.
- Helpful short form: `public delegate field` is too open, `private delegate field` is too closed, `public event` is the controlled subscription boundary.
