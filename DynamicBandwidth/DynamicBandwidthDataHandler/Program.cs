using DataHandlerBL;
using DynamicBandwidthCommon;
using DynamicBandwidthDataHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DynamicBandwidthDataHandlerConfiguration>()
    .BindConfiguration("DynamicBandwidthDataHandlerConfiguration");

builder.Services.AddOptions<DynamicBandwidthDataHandlerCommandArgs>()
                .Configure(_ => _.CommandArgs = args);

builder.Services.AddLogging();
builder.Services.AddSingleton<RedisMessageUtility>();
builder.Services.AddSingleton<DynamicBandwidthDataHandlerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DynamicBandwidthDataHandlerService>());

builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("ApplicationUrl"));

var app = builder.Build();

app.MapGet("/", () => "Hello DynamicBandwidthDataHandler!");

app.Run();
