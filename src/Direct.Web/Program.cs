using Direct.Web.Hubs;
using Direct.Web.Models;
using Direct.Web.Repositories;
using Direct.Web.Services;
using Marten;
using Marten.Services.Json;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddSingleton<IRepository, Repository>();
builder.Services.AddSingleton<IChatService, ChatService>();

builder.Services.AddOptions<AppConfiguration>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations();

builder.Services.AddMarten(options =>
{
    // Establish the connection string to your Marten database
    options.Connection(builder.Configuration.GetConnectionString("Marten")!);

    options.UseDefaultSerialization(serializerType: SerializerType.SystemTextJson);
}).OptimizeArtifactWorkflow();

builder.WebHost.UseUrls("http://localhost:5250");

var app = builder.Build();

app.MapHub<ChatHub>("/chatHub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
});

app.Run();
