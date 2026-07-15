using Workbencher.HubPlace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<ChatHub>("/chatHub");
app.MapHub<ProjectHub>("/projectHub");
app.MapHub<TaskHub>("/taskHub");
app.Run();
