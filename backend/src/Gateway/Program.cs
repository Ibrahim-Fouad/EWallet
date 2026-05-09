using EWallet.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdStampingMiddleware>();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();
