using Direct.Web.Hubs;
using Direct.Web.Models;
using Direct.Web.Services;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddSingleton<IChatService, ChatService>();

builder.Services.AddOptions<AppConfiguration>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations();

builder.WebHost.UseUrls("http://localhost:5250");

var app = builder.Build();

app.MapHub<ChatHub>("/chatHub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
});

app.Run();
