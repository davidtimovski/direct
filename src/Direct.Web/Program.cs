using Direct.Web;
using Direct.Web.Hubs;
using Direct.Web.Services;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<IPullService, PullService>();

builder.Services.AddHostedService<PullOperationsCleaner>();

builder.WebHost.UseUrls("http://localhost:5250");

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

app.MapHub<ChatHub>("/chatHub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
});

app.Run();
