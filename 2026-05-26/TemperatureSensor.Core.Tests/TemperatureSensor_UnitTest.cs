using TemperatureSensor.Core;
namespace TemperatureSensor.Core.Tests;


public class TemperatureSensorTests
{
    [Fact]
    public void RecordReading_BelowThreshold_DoesNotFireEvent()
    {
        // Arrange  ← 准备：构造被测对象（SUT）+ 测试数据
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (s, e) => {eventFired = true;};

        // Act      ← 行动：调用被测的那一个方法
        
        sensor.RecordReading(25.0);
        
        // Assert   ← 断言：检查结果是否符合预期
        Assert.False(eventFired, "ThresholdExceeded event should not have fired for reading below threshold.");
    }


    [Fact]
    public void RecordReading_AboveThreshold_FiresEvent()
    {
        // Arrange  ← 准备：构造被测对象（SUT）+ 测试数据
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (s, e) => {eventFired = true;};

        // Act      ← 行动：调用被测的那一个方法
        
        sensor.RecordReading(35.0);
        
        // Assert   ← 断言：检查结果是否符合预期
        Assert.True(eventFired, "ThresholdExceeded event should have fired for reading above threshold.");
    }


    [Fact]
    public void RecordReading_EqualThreshold_DoesNotFireEvent()
    {
        // Arrange  ← 准备：构造被测对象（SUT）+ 测试数据
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        bool eventFired = false;
        sensor.ThresholdExceeded += (s, e) => {eventFired = true;};

        // Act      ← 行动：调用被测的那一个方法
        
        sensor.RecordReading(30.0);
        
        // Assert   ← 断言：检查结果是否符合预期
        Assert.False(eventFired, "ThresholdExceeded event should not have fired for reading equal to threshold.");
    }


    [Fact]
    public void RecordReading_AboveThreshold_PassesCorrectPayload()
    {
        // Arrange  ← 准备：构造被测对象（SUT）+ 测试数据
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        ThresholdExceededEventArgs? captured = null;  
        sensor.ThresholdExceeded += (s, e) => { captured = e;};

        // Act      ← 行动：调用被测的那一个方法
        
        sensor.RecordReading(35.0);
        
        // Assert   ← 断言：检查结果是否符合预期
        Assert.NotNull(captured);
        Assert.Equal(35.0, captured!.Reading);
        Assert.Equal(30.0, captured!.Threshold);   
    }


    [Fact]
    public void RecordReading_AboveThreshold_NotifiesAllSubscribers()
    {
        // Arrange
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        int sub1=0;
        int sub2=0;
        int sub3=0;
        sensor.ThresholdExceeded += (s, e) => {sub1++;};
        sensor.ThresholdExceeded += (s, e) => {sub2++;};
        sensor.ThresholdExceeded += (s, e) => {sub3++;};

        // Act
        sensor.RecordReading(35.0);

        // Assert
        Assert.Equal(1, sub1);
        Assert.Equal(1, sub2);
        Assert.Equal(1, sub3);

    }



    [Fact]
    public void RecordReading_SubscriberThrows_StopsLaterSubscribers()
    {
        // Arrange
        TemperatureSensor sensor = new TemperatureSensor(30.0);
        int sub1=0;
     
        int sub3=0;
        sensor.ThresholdExceeded += (s, e) => {sub1++;};
        sensor.ThresholdExceeded += (s, e) => {throw new InvalidOperationException("Subscriber error");};
        sensor.ThresholdExceeded += (s, e) => {sub3++;};

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sensor.RecordReading(35.0));
        Assert.Equal(1, sub1);
        Assert.Equal(0, sub3);  // 后续订阅者未被调用
   
    }



    [Fact]
    public void RecordReading_NaN_ThrowsArgumentException()
    {
        // Arrange
        TemperatureSensor sensor = new TemperatureSensor(30.0);

        // Act + Assert (合并)
        var ex = Assert.Throws<ArgumentException>(() => sensor.RecordReading(double.NaN));
        Assert.Equal("celsius", ex.ParamName);
    }




    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RecordReading_InvalidValues_ThrowsArgumentException(double invalidValue)
    {
        // Arrange
        TemperatureSensor sensor = new TemperatureSensor(30.0);

        // Act + Assert (合并)
        var ex = Assert.Throws<ArgumentException>(() => sensor.RecordReading(invalidValue));
        Assert.Equal("celsius", ex.ParamName);
    }


public static TheoryData<double> InvalidReading=> new()
{
    double.NaN,
    double.PositiveInfinity,
    double.NegativeInfinity
};

    [Theory]
    [MemberData(nameof(InvalidReading))]
    public void RecordReading_InvalidValues_ThrowsArgumentException1(double invalidValue)
    {
        // Arrange
        TemperatureSensor sensor = new TemperatureSensor(30.0);

        // Act + Assert (合并)
        var ex = Assert.Throws<ArgumentException>(() => sensor.RecordReading(invalidValue));
        Assert.Equal("celsius", ex.ParamName);
    }


}