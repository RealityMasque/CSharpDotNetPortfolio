public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/hello", () => "Hello, World!");
        app.MapPost("/echo", (Message msg) => msg);

        app.Run();
    }
    public record Message(string Content);
}