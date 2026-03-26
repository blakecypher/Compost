using NLog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add Orchard Core CMS
builder.Services.AddOrchardCms()
    .AddMvc();

// Add logging with NLog
builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// Add Application Insights if configured
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:InstrumentationKey"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseOrchardCore();

app.Run();
