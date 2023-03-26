using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<DynamicBandwidthSenderConfiguration>()
    .BindConfiguration(nameof(DynamicBandwidthSenderConfiguration));

// add logging service
builder.Services.AddLogging();

// register sender service
builder.Services.AddSingleton<DynamicBandwidthSender.DynamicBandwidthSender>();
builder.Services.AddHostedService<DynamicBandwidthSender.DynamicBandwidthSender>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("ApplicationUrl"));

app.UseRouting();

app.MapGet("/", () => "Hello from Dynamic Bandwidth Sender!");


app.UseEndpoints(endpoints =>
{
    // Enable the /metrics page to export Prometheus metrics.
    // Open http://localhost:8082/metrics to see the metrics.
    //
    // Metrics published in this sample:
    // * built-in process metrics giving basic information about the .NET runtime (enabled by default)
    // * metrics from .NET Event Counters (enabled by default, updated every 10 seconds)
    // * metrics from .NET Meters (enabled by default)
    // * metrics about requests made by registered HTTP clients used in SampleService (configured above)
    // * metrics about requests handled by the web app (configured above)
    // * ASP.NET health check statuses (configured above)
    // * custom business logic metrics published by the SampleService class
    endpoints.MapMetrics();
});

app.Run();