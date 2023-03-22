var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.MapGet("/", () => "Hello from Dynamic Bandwidth Sender!");

app.Run();