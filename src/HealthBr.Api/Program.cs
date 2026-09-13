var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "HealthBr API");

app.Run();
