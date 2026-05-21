using DI_Logging_MinHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1. Build the registration list.
var services = new ServiceCollection();

// 2. Register the logging infrastructure (console output).
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// 3. Register our own business service.
services.AddTransient<TemperatureSensorService>();

// 4. Build the DI container.
using var provider = services.BuildServiceProvider();

// 5. Resolve the service from the container.
var sensor = provider.GetRequiredService<TemperatureSensorService>();

// 6. Call business methods.
sensor.Record(72.5);
sensor.Record(101.3);

// =====================================================================
// 学习笔记（learning notes）—— 保留为注释，不参与编译
// =====================================================================
//
// 先建立一个 ServiceCollection sc，不需要任何输入参数：
//     var services = new ServiceCollection();
//
// sc 提供了一个 AddLogging 的方法，但需要传入“另一个方法（委托）”。
// 这里用 lambda 直接写出来，等价于先定义命名方法再传入：
//
//     services.AddLogging(builder =>
//     {
//         builder.AddConsole();
//         builder.SetMinimumLevel(LogLevel.Information);
//     });
//
// 转换为命名方法形式：
//
//     void Configure(ILoggingBuilder builder)
//     {
//         builder.AddConsole();
//         builder.SetMinimumLevel(LogLevel.Information);
//     }
//     services.AddLogging(Configure);
//
// 重点：
// - builder 不是缺乏定义；它是 lambda 的参数，类型由 Action<ILoggingBuilder> 推断
// - 不需要 return，因为 Action<T> 的返回值是 void（不是 Func）
// - AddLogging 是扩展方法（extension method），this 关键字写在它的定义里，调用时不写
// - lambda 不会自己执行；AddLogging 内部会创建 LoggingBuilder 并调用我们传入的 lambda
