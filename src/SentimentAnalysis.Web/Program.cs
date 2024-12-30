using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SentimentAnalysis.Infrastructure.Services;

Console.WriteLine("Starting application...");

var builder = WebApplication.CreateBuilder(args);

// Ustaw konkretny port
builder.WebHost.UseUrls("http://localhost:5003");

// Dodaj logowanie
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

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
