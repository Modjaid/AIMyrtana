using Adapters.TcpTest;
using AgentForSite.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAgentForSiteStack();
builder.Services.Configure<TcpTestMessagingOptions>(_ => { });
builder.Services.AddHostedService<TcpTestAgentChatHostedService>();

var host = builder.Build();
host.Run();

