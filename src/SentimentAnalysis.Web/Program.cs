using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SentimentAnalysis.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using SentimentAnalysis.Core.Interfaces;

Console.WriteLine("Starting application...");

var builder = WebApplication.CreateBuilder(args);

// Ustaw konkretny port
builder.WebHost.UseUrls("http://localhost:5003");

// Dodaj logowanie
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Możemy też dodać bardziej szczegółowe poziomy dla konkretnych kategorii
builder.Logging.AddFilter("SentimentAnalysis.Infrastructure", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.Azure.Cosmos", LogLevel.Debug);

Console.WriteLine("Reading configuration...");
var azureEndpoint = builder.Configuration["Azure:CognitiveServices:Endpoint"];
var azureApiKey = builder.Configuration["Azure:CognitiveServices:ApiKey"];

Console.WriteLine($"Azure Endpoint: {azureEndpoint}");
Console.WriteLine("Configuration loaded");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register Azure Sentiment Analyzer
Console.WriteLine("Registering services...");
builder.Services.AddScoped<AzureSentimentAnalyzer>(sp => 
{
    Console.WriteLine("Creating AzureSentimentAnalyzer instance");
    return new AzureSentimentAnalyzer(
        azureEndpoint ?? "https://sentiment-analysis-cognitive.cognitiveservices.azure.com/",
        azureApiKey ?? "n2W2ufiooQkJlNWpSKAImv4KZmbcJQhdpQDVstU6X6qPNtzp8xw7JQQJ99ALACYeBjFXJ3w3AAAaACOGgO0V"
    );
});

Console.WriteLine("\n=== Checking CosmosDB Configuration ===");
var cosmosEndpoint = builder.Configuration["Azure:CosmosDb:EndpointUri"];
var cosmosKey = builder.Configuration["Azure:CosmosDb:PrimaryKey"];
var cosmosDb = builder.Configuration["Azure:CosmosDb:DatabaseName"];
var cosmosContainer = builder.Configuration["Azure:CosmosDb:ContainerName"];

Console.WriteLine($"CosmosDB Endpoint: {cosmosEndpoint}");
Console.WriteLine($"DatabaseName: {cosmosDb}");
Console.WriteLine($"ContainerName: {cosmosContainer}");
Console.WriteLine("=== Configuration Check Complete ===\n");

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<CosmosDbService>>();
    return new CosmosDbService(
        configuration["Azure:CosmosDb:EndpointUri"],
        configuration["Azure:CosmosDb:PrimaryKey"],
        configuration["Azure:CosmosDb:DatabaseName"],
        configuration["Azure:CosmosDb:ContainerName"],
        logger);
});

builder.Services.AddScoped<ISocialMediaMonitor, DemoSocialMediaMonitor>();

Console.WriteLine("Building application...");
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

Console.WriteLine("Configuring middleware...");
app.UseStaticFiles();
app.UseRouting();

// Dodaj endpoint testowy
app.MapGet("/test", () => "Server is running!");

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Console.WriteLine("Starting server on http://localhost:5003");
try
{
    await app.RunAsync();
    Console.WriteLine("Server started successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to start server: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}

// Po zarejestrowaniu serwisów
var monitor = app.Services.GetRequiredService<ISocialMediaMonitor>();
if (monitor is DemoSocialMediaMonitor demoMonitor)
{
    await demoMonitor.TestDatabaseConnection();
}
