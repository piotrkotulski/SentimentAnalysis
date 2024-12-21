using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SentimentAnalysis.Core.Interfaces;
using SentimentAnalysis.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Dodaj na początku pliku, przed builder.Build()
builder.WebHost.UseUrls("http://localhost:5206"); // Używamy innego portu

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register our services
builder.Services.AddScoped<ISocialMediaMonitor, DemoSocialMediaMonitor>();
builder.Services.AddScoped<AzureSentimentAnalyzer>(sp => 
    new AzureSentimentAnalyzer(
        builder.Configuration["Azure:CognitiveServices:Endpoint"] ?? "https://your-endpoint",
        builder.Configuration["Azure:CognitiveServices:ApiKey"] ?? "your-key"
    ));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
