using DataHandlerBL;
using DynamicBandwidthCommon;
using Redis.OM;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);
string port1 = "http://localhost:500" + args[0];
int suffixPortNumber = (Int32.Parse(args[0]) + 100);
string port2 = "http://localhost:5" + suffixPortNumber.ToString();
builder.WebHost.UseUrls(port1, port2);

builder.Services.AddLogging();
builder.Services.AddSingleton<RedisMessageUtility>();
builder.Services.AddSingleton<Manager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<Manager>());

string redisConnectionString = builder.Configuration.GetConnectionString("REDIS_CONNECTION_STRING");
var redisProvider = new RedisConnectionProvider(redisConnectionString);
builder.Services.AddSingleton(redisProvider);

DataHandlerConfig dataHandlerConfig = builder.Configuration.GetSection("DataHandlerConfig")
                                                .Get<DataHandlerConfig>();
dataHandlerConfig.RunningChannelID = Int32.Parse(args[0]);
builder.Services.AddSingleton(dataHandlerConfig);

var app = builder.Build();


//app.MapGet("/", () => "Hello DataHandler!");



app.Run();
