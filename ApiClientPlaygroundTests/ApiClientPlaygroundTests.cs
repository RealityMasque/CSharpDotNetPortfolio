using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace ApiClientPlaygroundTests;

public class ApiClientPlaygroundTests
{
    // Helper to create a mocked HttpClient for a specific endpoint
    private HttpClient CreateMockHttpClient(string urlContains, string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri.Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("http://localhost:5159")
        };
    }

    [Fact]
    public async Task PostLocalLogin_User_ReturnsToken()
    {
        var mockClient = CreateMockHttpClient("/login", "{\"token\":\"FAKE_USER_TOKEN\"}");
        var apiClient = new ApiClient(mockClient);

        string token = await apiClient.PostLocalLogin("user");

        Assert.Equal("FAKE_USER_TOKEN", token);
    }

    [Fact]
    public async Task PostLocalLogin_Admin_ReturnsToken()
    {
        var mockClient = CreateMockHttpClient("/login", "{\"token\":\"FAKE_ADMIN_TOKEN\"}");
        var apiClient = new ApiClient(mockClient);

        string token = await apiClient.PostLocalLogin("admin");

        Assert.Equal("FAKE_ADMIN_TOKEN", token);
    }

    [Fact]
    public async Task GetLocalProfile_ReturnsProfileJson()
    {
        string expectedJson = "{\"username\":\"user\",\"role\":\"User\"}";
        var mockClient = CreateMockHttpClient("/profile", expectedJson);
        var apiClient = new ApiClient(mockClient);

        string token = "FAKE_TOKEN";
        string result = await apiClient.GetLocalProfile(token);

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task GetLocalUser_ReturnsUserJson()
    {
        string expectedJson = "{\"username\":\"user\",\"role\":\"User\"}";
        var mockClient = CreateMockHttpClient("/user", expectedJson);
        var apiClient = new ApiClient(mockClient);

        string result = await apiClient.GetLocalUser("FAKE_TOKEN");

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task GetLocalAdmin_ReturnsAdminJson()
    {
        string expectedJson = "{\"username\":\"admin\",\"role\":\"Admin\"}";
        var mockClient = CreateMockHttpClient("/admin", expectedJson);
        var apiClient = new ApiClient(mockClient);

        string result = await apiClient.GetLocalAdmin("FAKE_ADMIN_TOKEN");

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task GetLocalSecret_ReturnsSecret()
    {
        string expectedJson = "This is a protected resource";
        var mockClient = CreateMockHttpClient("/secret", expectedJson);
        var apiClient = new ApiClient(mockClient);

        string result = await apiClient.GetLocalSecret("FAKE_TOKEN");

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task HandlesHttpError_ThrowsException()
    {
        var mockClient = CreateMockHttpClient("/login", "Unauthorized", HttpStatusCode.Unauthorized);
        var apiClient = new ApiClient(mockClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => apiClient.PostLocalLogin("user"));
    }
}
