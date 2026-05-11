using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Bind JwtSettings from appsettings.json
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if(jwtSettings == null)
        {
            throw new Exception("Failed to load JWT settings from configuration.");
        }

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateIssuerSigningKey = true
                };
            }
        );

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
        });

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/hello", () => "Hello, World!");
        app.MapPost("/echo", (Message msg) => msg);

        app.MapPost("/login", (UserLogin login) =>
            {
                var roles = new List<string>();
                if (login.Username == "admin")
                {
                    roles.Add("Admin");
                }
                else
                {
                    roles.Add("User");
                }

                // Add roles as claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, login.Username)
                };
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                if (login.Password == "password")
                {
                    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var tokenKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes),
                        Issuer = jwtSettings.Issuer,
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var jwtToken = tokenHandler.WriteToken(token);

                    return Results.Ok(new { token = jwtToken });
                }

                return Results.Unauthorized();
            }
        );

        app.MapGet("/secret", () => "This is a protected resource")
            .RequireAuthorization();

        app.MapGet("/admin", () => "This is admin only")
            .RequireAuthorization("AdminOnly");

        app.MapGet("/user", () => "This is user or admin")
            .RequireAuthorization("UserOrAdmin");

        app.MapGet("/profile", (ClaimsPrincipal user) =>
        {
            string username = user.Identity?.Name ?? "Unknown";
            string role = user.FindFirst(ClaimTypes.Role)?.Value ?? "No role";
            return $"User: {username}, Role: {role}";
        }).RequireAuthorization();

        app.Run();
    }
    public record Message(string Content);

    public record UserLogin(string Username, string Password);

    public class JwtSettings
    {
        public string SecretKey { get; set; } = "";
        public string Issuer { get; set; } = "";
        public int ExpirationMinutes { get; set; }
    }
}