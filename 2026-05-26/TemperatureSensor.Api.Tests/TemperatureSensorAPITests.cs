namespace TemperatureSensor.Api.Tests;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;        // ← Encoding 来自这里
using System.Text.Json;   // ← JsonSerializer 来自这里
using System.Net.Http.Json; 

public class TemperatureSensorAPITests: IClassFixture<WebApplicationFactory<Program>>
{

     private readonly HttpClient _client; 

    public TemperatureSensorAPITests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsHelloWorld()
    {
    // Act
    var response = await _client.GetAsync("/");

    // Assert
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    Assert.Equal("Hello World!", body);

    }

[Fact]
    public async Task PostReading_WithValidValue_Returns200OK()
    {
        // Arrange
        var reading = new { Celsius = 25.0 };
   

        // Act
        var response = await _client.PostAsJsonAsync("/readings", reading);

        // Assert
        response.EnsureSuccessStatusCode();

    }

    [Fact]
    public async Task PostReading_WithMalformedBody_Returns400BadRequest()
    {
        // Arrange
        var badPayload = new { Celsius = "not-a-number" };

        // Act
        var response = await _client.PostAsJsonAsync("/readings",badPayload);

        // Assert — 这次不能用 EnsureSuccessStatusCode（它会因为 400 而抛）
        //          要直接断言状态码
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

}