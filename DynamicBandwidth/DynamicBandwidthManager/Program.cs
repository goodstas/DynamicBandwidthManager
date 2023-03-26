using DynamicBandwidth;
using DynamicBandwidthCommon;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddOptions<DynamicBandwidthManagerConfiguration>()
    .BindConfiguration("DynamicBandwidthManagerConfiguration");

builder.Services.AddLogging();
builder.Services.AddSingleton<RedisMessageUtility>();
builder.Services.AddSingleton<DynamicBandwidthManager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DynamicBandwidthManager>());

var app = builder.Build();

app.MapGet("/", () => "Hello from DynamicBandwidthManager!");

app.Run();
