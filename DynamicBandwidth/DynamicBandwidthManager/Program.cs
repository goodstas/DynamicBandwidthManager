using DynamicBandwidth;
using DynamicBandwidthCommon;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("REDIS_CONNECTION_STRING");
var provider = new RedisConnectionProvider(redisConnectionString);

builder.Services.AddSingleton(provider);

builder.Services.AddLogging();
builder.Services.AddSingleton<RedisMessageUtility>();
builder.Services.AddSingleton<DynamicBandwidthManager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DynamicBandwidthManager>());

builder.Services
    .AddOptions<DynamicBandwidthManagerConfiguration>()
    .BindConfiguration("DynamicBandwidthManagerConfiguration");

var app = builder.Build();

app.MapGet("/", () => "Hello from DynamicBandwidthManager!");

app.Run();
