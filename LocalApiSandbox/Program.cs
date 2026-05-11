using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;

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

        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/login", (UserLogin login) =>
            {
                if (login.Username == "user" && login.Password == "password")
                {
                    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var tokenKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new System.Security.Claims.ClaimsIdentity(new[] {
                            new System.Security.Claims.Claim("username", login.Username)
                        }),
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

        app.MapGet("/hello", () => "Hello, World!");
        app.MapPost("/echo", (Message msg) => msg);

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