# Avalonia.Hosting

### Simple asp.net core like avalonia (desktop for now) application builder

```csharp
var builder = App.CreateBuilder();

builder.Services.AddSingleton<IService, Service>();

builder.Configuration.AddJsonFile("appsettings.json");

builder.Logging.AddConsole();

var app = builder.Build();

await app.RunAsync();
```
