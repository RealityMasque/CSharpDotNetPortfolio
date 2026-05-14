using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
//using MiniValidation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if(jwtSettings == null)
{
    throw new Exception("Failed to load JWT settings from configuration.");
}

builder.Services.AddControllers(); // required for FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UserLoginValidator>();

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
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole(UserRoles.User, UserRoles.Admin));
    options.AddPolicy("ModeratorOrAdmin", policy => policy.RequireRole(UserRoles.Moderator, UserRoles.Admin));
    options.AddPolicy("GuestOrUserOrAdmin", policy => policy.RequireRole(UserRoles.Guest, UserRoles.User, UserRoles.Admin));
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/login", (UserLoginDto login) =>
{
    //if (!MiniValidator.TryValidate(login, out var errors))
    //    return Results.BadRequest(errors);

    var validator = new UserLoginValidator();
    var result = validator.Validate(login);
    if (!result.IsValid)
        return Results.BadRequest(result.Errors);

    var roles = login.Username switch
    {
        "admin" => new[] { UserRoles.Admin },
        "user" => new[] { UserRoles.User },
        "moderator" => new[] { UserRoles.Moderator },
        "guest" => new[] { UserRoles.Guest },
        "multi" => new[] { UserRoles.Admin, UserRoles.User },
        _ => Array.Empty<string>()
    };
    
    if(roles.Length == 0)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, login.Username) };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtSettings.Issuer,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes),
        signingCredentials: creds
    );

    var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = jwtToken });
});

app.MapGet("/admin", () => "Admin only endpoint")
   .RequireAuthorization("AdminOnly");

app.MapGet("/user", () => "User or Admin endpoint")
   .RequireAuthorization("UserOrAdmin");

app.MapGet("/moderator", () => "Moderator or Admin endpoint")
   .RequireAuthorization("ModeratorOrAdmin");

app.MapGet("/guest", () => "Guest or User or Admin endpoint")
   .RequireAuthorization("GuestOrUserOrAdmin");

app.MapGet("/profile", (ClaimsPrincipal user) =>
{
    string username = user.Identity?.Name ?? "Unknown";
    string role = UserRoles.GetUserRole(user);
    return $"User: {username}, Role: {role}";
}).RequireAuthorization();

app.Run();