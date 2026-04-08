var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProductAStack();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", product = "ProductA" }));

app.Run();
