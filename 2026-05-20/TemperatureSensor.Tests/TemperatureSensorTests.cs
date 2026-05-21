using TemperatureSensor.Core;
// Note: no `using Xunit;` here — the .csproj already declares
// <Using Include="Xunit" /> as a global using.

namespace TemperatureSensor.Tests;

// ----------------------------------------------------------------------
// Test class.
// Convention: class name = <ClassUnderTest>Tests.
// xUnit creates a NEW instance of this class for EACH [Fact] method,
// so test methods cannot accidentally share state.
// ----------------------------------------------------------------------
public class TemperatureSensorTests
{
    // ------------------------------------------------------------------
    // Test 1 (model answer — read this carefully).
    //
    // Naming convention: <Scenario>_<ExpectedBehavior>
    // ------------------------------------------------------------------
    [Fact]
    public void AboveThreshold_RaisesEvent()
    {
        // ---- Arrange ----
        var sensor = new TemperatureSensor.Core.TemperatureSensor(threshold: 100.0);
        bool eventFired = false;
        ThresholdExceededEventArgs? captured = null;
        sensor.ThresholdExceeded += (sender, e) => { eventFired = true; captured = e; };

        // ---- Act ----
        sensor.RecordReading(101);

        // ---- Assert ----
        Assert.True(eventFired, "ThresholdExceeded should fire when reading > threshold");
        Assert.NotNull(captured);
        Assert.Equal(101, captured!.Reading);
        Assert.Equal(100, captured.Threshold);
        }

    [Fact]
    public void AtOrBelowThreshold_DoesNotRaiseEvent()
    {
        TemperatureSensor.Core.TemperatureSensor sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (sender, e) => { eventFired = true; };
        sensor.RecordReading(99);

        Assert.False(eventFired, "ThresholdExceeded should not fire when reading <= threshold");
    }

    [Theory]
    [MemberData(nameof(ThresholdTestData))]
    public void RecordReading_RaisesEventOnlyWhenAboveThreshold(double celsius, bool shouldRaise)
    {
        TemperatureSensor.Core.TemperatureSensor sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);

        bool eventFired=false;

        sensor.ThresholdExceeded += (sender, e)=> {eventFired=true;};

        sensor.RecordReading(celsius);
        
        Assert.Equal(shouldRaise, eventFired);
    }

    [Theory]
    [InlineData(200.0, true, 200.0)]
    [InlineData(100.0, false, null)]
    [InlineData(50.0, false, null)]
    public void RecordReading_FullVerification(double celsius, bool shouldRaise, double? expectedReading)
    {
        TemperatureSensor.Core.TemperatureSensor sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);

        bool eventFired=false;
        ThresholdExceededEventArgs? captured = null;

        sensor.ThresholdExceeded += (sender, e)=> {eventFired=true; captured = e;};

        sensor.RecordReading(celsius);
        
        
        if (shouldRaise)
        {
            Assert.NotNull(captured);
            Assert.Equal(expectedReading, captured!.Reading);
            Assert.Equal(100.0, captured.Threshold);
        }
        else
        {
            Assert.False(eventFired, "ThresholdExceeded should not fire when reading <= threshold");
            Assert.Null(captured);
        }
    }

    [Fact]
    public void RecordReading_NaN_ThrowsArgumentException()
    {
        var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
        Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.NaN));
    }

    [Fact]
    public void RecordReading_PositiveInfinity_ThrowsArgumentException()
    {
        var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
        Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.PositiveInfinity));
    }

    [Fact]
    public void RecordReading_NegativeInfinity_ThrowsArgumentException()
    {
        var sensor = new TemperatureSensor.Core.TemperatureSensor(100.0);
        Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.NegativeInfinity));
    }


    public static IEnumerable<object[]> ThresholdTestData =>
        new List<object[]>
        {
            new object[] { 200.0,  true  },
            new object[] { 150.0,  true  },     // 多加一个 above 等价类样本
            new object[] { 100.001, true },     // 边界微高（>100 一点点）
            new object[] { 100.0,  false },     // 边界
            new object[] { 99.999, false },     // 边界微低（<100 一点点）
            new object[] { 50.0,   false },
            new object[] { 0.0,    false },     // 极小值
            new object[] { -40.0,  false },     // 负温度
        };
}
