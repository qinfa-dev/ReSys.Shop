using ReSys.API;
using ReSys.Core;
using ReSys.Infrastructure;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Host.UseSerilog();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services
    .AddCore()
    .AddInfrastructure(configuration: builder.Configuration, environment: builder.Environment);

builder.Services.AddPresentation(configuration: builder.Configuration, builderEnvironment: builder.Environment);

var app = builder.Build();

app
    .UseCore()
    .UseInfrastructure(appConfiguration: app.Configuration, environment: app.Environment);

app.UsePresentation();

await app.RunAsync();