using DynamicBandwidth;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddSingleton<DynamicBandwidthManager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DynamicBandwidthManager>());

var redisConnectionString = builder.Configuration.GetConnectionString("REDIS_CONNECTION_STRING");
var provider = new RedisConnectionProvider(redisConnectionString);

var app = builder.Build();

app.MapGet("/", () => "Hello DynamicBandwidthManager!");

app.Run();
