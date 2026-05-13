using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

public class InputValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public InputValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("", "password")]      // Empty username
    [InlineData("us", "password")]    // Username too short
    [InlineData("user", "")]          // Empty password
    [InlineData("user", "123")]       // Password too short
    [InlineData(null, "password")]    // null username
    [InlineData("user", null)]        // null password
    [InlineData("123456789012345678901234567890123456789012345678901", "password")]        // Username too long
    [InlineData("user", "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901")]        // Password too long
    public async Task Login_InvalidModel_ShouldReturnBadRequest(string username, string password)
    {
        var payload = new { Username = username, Password = password };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync("/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}