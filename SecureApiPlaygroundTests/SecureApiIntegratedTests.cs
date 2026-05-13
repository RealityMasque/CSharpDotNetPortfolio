using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecureApiPlaygroundTests;

public class SecureApiPlaygroundTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string SecretKey = "super_secret_key_123!_must_be_32_bytes_or_more";
    private const string Issuer = "SecureApiPlayground";

    public SecureApiPlaygroundTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region MemberData for roles

    // endpoint, roles, expected success
    public static IEnumerable<object[]> RoleEndpointTestCases => new List<object[]>
    {
        new object[] { "/admin", new[] { "Admin" }, true },
        new object[] { "/admin", new[] { "User" }, false },
        new object[] { "/user", new[] { "User" }, true },
        new object[] { "/user", new[] { "Admin" }, true },
        new object[] { "/moderator", new[] { "Moderator" }, true },
        new object[] { "/moderator", new[] { "Admin" }, true },
        new object[] { "/moderator", new[] { "User" }, false },
        new object[] { "/guest", new[] { "Guest" }, true },
        new object[] { "/guest", new[] { "User" }, true },
        new object[] { "/guest", new[] { "Admin" }, true },
        new object[] { "/guest", new[] { "Moderator" }, false },
        // multi-role tests
        new object[] { "/admin", new[] { "Admin", "User" }, true },
        new object[] { "/user", new[] { "User", "Guest" }, true },
        new object[] { "/moderator", new[] { "User", "Admin" }, true },
        new object[] { "/guest", new[] { "Moderator", "Guest" }, true }
    };

    public static IEnumerable<object[]> UserRolesTestCases => new List<object[]>
    {
        new object[] { "admin", new[] { "Admin" } },
        new object[] { "user", new[] { "User" } },
        new object[] { "moderator", new[] { "Moderator" } },
        new object[] { "guest", new[] { "Guest" } },
        new object[] { "multi", new[] { "Admin", "User" } } // multi-role test
    };

    #endregion

    #region Helpers

    private string GenerateJwtTokenWithRoles(string username, IEnumerable<string> roles, int expiresMinutes = 30)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthorizationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    #endregion

    #region Login Tests

    [Theory]
    [MemberData(nameof(UserRolesTestCases))]
    public async Task Login_WithValidUser_ShouldReturnToken(string username, string[] roles)
    {
        var loginPayload = new { Username = username, Password = "password" };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync("/login", content);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("token", json);
    }
    
    [Theory]
    [InlineData("unknownuser")]
    public async Task Login_WithInvalidUser_ShouldReturnUnauthorized(string username)
    {
        var loginPayload = new { Username = username ?? "", Password = "password" };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync("/login", content);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    #endregion

    #region Profile Tests
  
    [Theory]
    [MemberData(nameof(UserRolesTestCases))]
    public async Task Profile_WithValidToken_ShouldReturnUsernameAndRole(string username, string[] roles)
    {
        var token = GenerateJwtTokenWithRoles(username, roles);
        SetAuthorizationHeader(token);

        var resp = await _client.GetAsync("/profile");
        resp.EnsureSuccessStatusCode();

        var content = await resp.Content.ReadAsStringAsync();
        Assert.Contains(username, content);
        foreach(var role in roles)
        {
            Assert.Contains(role, content);
        }
    }
    
    [Fact]
    public async Task Profile_WithoutToken_ShouldReturnUnauthorized()
    {
        var resp = await _client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    #endregion
    
    #region Role-based endpoints

    [Theory]
    [MemberData(nameof(RoleEndpointTestCases))]
    public async Task Endpoint_WithRoles_ShouldReturnExpectedStatus(string endpoint, string[] roles, bool shouldSucceed)
    {
        var token = GenerateJwtTokenWithRoles("user123", roles);
        SetAuthorizationHeader(token);

        var resp = await _client.GetAsync(endpoint);
        Assert.Equal(shouldSucceed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, resp.StatusCode);
    }
    
    #endregion
    
    #region Expired Token Tests

    [Fact]
    public async Task Endpoint_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var token = GenerateJwtTokenWithRoles("user123", new[] { "User" }, expiresMinutes: -10);
        SetAuthorizationHeader(token);

        var resp = await _client.GetAsync("/user");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    #endregion
    
    #region Invalid / Malformed / Wrong Key JWT

    [Fact]
    public async Task Endpoint_WithTamperedToken_ShouldReturnUnauthorized()
    {
        var validToken = GenerateJwtTokenWithRoles("user123", new[] { "User" });
        var tamperedToken = validToken.Substring(0, validToken.Length - 1) + (validToken[^1] == 'A' ? 'B' : 'A');

        SetAuthorizationHeader(tamperedToken);

        var resp = await _client.GetAsync("/user");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Endpoint_WithMalformedToken_ShouldReturnUnauthorized()
    {
        SetAuthorizationHeader("not_a_jwt");
        var resp = await _client.GetAsync("/user");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Endpoint_WithWrongSigningKey_ShouldReturnUnauthorized()
    {
        var wrongKey = Encoding.UTF8.GetBytes("12345678901234567890123456789013");
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "user123") };
        claims.Add(new Claim(ClaimTypes.Role, "User"));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(wrongKey), SecurityAlgorithms.HmacSha256)
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        SetAuthorizationHeader(tokenString);

        var resp = await _client.GetAsync("/user");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    #endregion
}