using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LocalApiSandboxTests
{
    public class LocalApiSandboxTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LocalApiSandboxTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        // Helper to login and get a JWT token
        private async Task<string> GetJwtTokenAsync(string username, string password)
        {
            var client = _factory.CreateClient();
            var payload = new { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/login", content);
            if (response.StatusCode != HttpStatusCode.OK) return null!;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("token").GetString()!;
        }

        [Fact]
        public async Task HelloEndpoint_ReturnsHelloWorld()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/hello");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, World!", content);
        }

        [Fact]
        public async Task EchoEndpoint_ReturnsPostedMessage()
        {
            var client = _factory.CreateClient();
            var payload = new { Content = "Test message" };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/echo", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Test message", responseString);
        }

        [Theory]
        [InlineData("admin", "password", HttpStatusCode.OK)]
        [InlineData("user", "password", HttpStatusCode.OK)]
        [InlineData("invalid", "wrong", HttpStatusCode.Unauthorized)]
        public async Task LoginEndpoint_ReturnsExpectedStatus(string username, string password, HttpStatusCode expected)
        {
            var client = _factory.CreateClient();
            var payload = new { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/login", content);
            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task SecretEndpoint_RequiresAuthorization()
        {
            var client = _factory.CreateClient();

            // Without token
            var response = await client.GetAsync("/secret");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // With valid token
            string token = await GetJwtTokenAsync("user", "password");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await client.GetAsync("/secret");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("protected resource", content);
        }

        [Fact]
        public async Task AdminEndpoint_AllowsOnlyAdmin()
        {
            var client = _factory.CreateClient();

            // Admin token
            string adminToken = await GetJwtTokenAsync("admin", "password");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await client.GetAsync("/admin");
            response.EnsureSuccessStatusCode();
            Assert.Contains("admin only", await response.Content.ReadAsStringAsync());

            // User token should fail
            string userToken = await GetJwtTokenAsync("user", "password");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            response = await client.GetAsync("/admin");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UserOrAdminEndpoint_AllowsUserAndAdmin()
        {
            var client = _factory.CreateClient();

            string adminToken = await GetJwtTokenAsync("admin", "password");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await client.GetAsync("/user");
            response.EnsureSuccessStatusCode();

            string userToken = await GetJwtTokenAsync("user", "password");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            response = await client.GetAsync("/user");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ProfileEndpoint_ReturnsUsernameAndRole()
        {
            var client = _factory.CreateClient();
            string adminToken = await GetJwtTokenAsync("admin", "password");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await client.GetAsync("/profile");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("User: admin", content);
            Assert.Contains("Role: Admin", content);
        }
    }
}