using ProductA.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddProductAStack();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
