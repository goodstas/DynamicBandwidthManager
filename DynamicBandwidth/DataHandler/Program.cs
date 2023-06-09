using DataHandlerBL;
using DynamicBandwidthCommon;
using Redis.OM;
using StackExchange.Redis;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddSingleton<RedisMessageUtility>();
builder.Services.AddSingleton<Manager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<Manager>());

string redisConnectionString = builder.Configuration.GetConnectionString("REDIS_CONNECTION_STRING");
var provider = new RedisConnectionProvider(redisConnectionString);

ConnectionMultiplexer connectMulti  = ConnectionMultiplexer.Connect(redisConnectionString);
//builder.Services
//.AddOptions<DynamicBandwidthManagerConfiguration>()
//.BindConfiguration("DynamicBandwidthManagerConfiguration");

var app = builder.Build();

app.MapGet("/", () => "Hello DataHandler!");



app.Run();
