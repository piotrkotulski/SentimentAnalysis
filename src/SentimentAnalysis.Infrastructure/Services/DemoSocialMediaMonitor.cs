using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using SentimentAnalysis.Core.Models;
using SentimentAnalysis.Core.Interfaces;
using SentimentAnalysis.Infrastructure.Services;

namespace SentimentAnalysis.Infrastructure.Services
{
    public class DemoSocialMediaMonitor : ISocialMediaMonitor
    {
        private readonly Random _random = new Random();
        private readonly string[] _authors = { "User123", "SocialGuru", "TechFan", "Influencer", "RegularUser" };
        private readonly CosmosDbService _cosmosDbService;
        private readonly ILogger<DemoSocialMediaMonitor> _logger;

        public DemoSocialMediaMonitor(CosmosDbService cosmosDbService, ILogger<DemoSocialMediaMonitor> logger)
        {
            _cosmosDbService = cosmosDbService;
            _logger = logger;
        }

        public async Task<IEnumerable<SocialMediaPost>> GetPostsAsync(string searchPhrase, int daysBack, string source)
        {
            try
            {
                Console.WriteLine("\n=== DemoSocialMediaMonitor.GetPostsAsync ===");
                Console.WriteLine($"Searching for posts with phrase: {searchPhrase}, daysBack: {daysBack}, source: {source}");
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-daysBack);
                Console.WriteLine($"Date range: {startDate} to {endDate}");
                
                Console.WriteLine("Calling CosmosDbService.GetPostsAsync...");
                var existingPosts = await _cosmosDbService.GetPostsAsync(searchPhrase, startDate, endDate);
                Console.WriteLine($"Found {existingPosts.Count()} existing posts");

                if (!existingPosts.Any())
                {
                    Console.WriteLine("No existing posts found, generating new ones");
                    var newPosts = GenerateNewPosts(searchPhrase, daysBack, source).ToList();
                    Console.WriteLine($"Generated {newPosts.Count} new posts");
                    
                    Console.WriteLine("Attempting to save posts to Cosmos DB...");
                    await _cosmosDbService.SavePostsAsync(newPosts);
                    Console.WriteLine("Posts saved successfully");
                    return newPosts;
                }

                return existingPosts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Error in DemoSocialMediaMonitor ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=== Error Details End ===\n");
                throw;
            }
        }

        private IEnumerable<SocialMediaPost> GenerateNewPosts(string searchPhrase, int daysBack, string source)
        {
            var endDate = DateTime.Now;
            var random = new Random();
            var posts = new List<SocialMediaPost>();

            for (int i = 0; i < 12; i++)
            {
                var post = new SocialMediaPost
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = GenerateContent(searchPhrase),
                    Source = source == "all" ? (random.Next(2) == 0 ? "Twitter" : "Facebook") : source,
                    CreatedAt = endDate.AddDays(-random.Next(daysBack)),
                    Campaign = searchPhrase?.ToUpper() ?? "DEFAULT"
                };
                posts.Add(post);
            }

            return posts;
        }

        public async Task<IEnumerable<string>> GetAvailableSourcesAsync()
        {
            return await Task.FromResult(new[] { "all", "twitter", "facebook" });
        }

        public async Task<Dictionary<DateTime, int>> GetPostsTimelineAsync(string searchPhrase, int daysBack, string source)
        {
            var timeline = new Dictionary<DateTime, int>();
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-daysBack);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                timeline[date] = _random.Next(5, 50);
            }

            return await Task.FromResult(timeline);
        }

        private string GenerateContent(string searchPhrase)
        {
            var templates = new[]
            {
                $"Kupiłem właśnie {searchPhrase} i jestem zachwycony! Świetna jakość, wygodne i stylowe. Polecam każdemu! #zadowolony",
                $"Te {searchPhrase} to najlepszy zakup w tym roku! Obsługa klienta na 5+, szybka dostawa. Super produkt!",
                $"{searchPhrase} spełniły wszystkie moje oczekiwania. Komfort noszenia rewelacyjny, cena adekwatna do jakości.",
                $"Używam {searchPhrase} od miesiąca i jestem pod wrażeniem. Świetny design i wykonanie.",
                $"Standardowe {searchPhrase}, nic specjalnego. Spełniają swoją funkcję.",
                $"{searchPhrase} są ok, ale mogłyby być lepsze. Przeciętna jakość jak na tę cenę.",
                $"Trudno powiedzieć coś więcej o {searchPhrase}, zwykły produkt jak wiele innych.",
                $"Mam mieszane uczucia co do {searchPhrase}. Są plusy i minusy.",
                $"Rozczarowanie roku - {searchPhrase}. Jakość pozostawia wiele do życzenia, nie polecam!",
                $"Zdecydowanie odradzam {searchPhrase}. Słaba jakość, wysokie ceny, fatalna obsługa klienta.",
                $"{searchPhrase} to porażka. Rozpadają się po kilku użyciach, wyrzucone pieniądze.",
                $"Nigdy więcej nie kupię {searchPhrase}. Totalne rozczarowanie i strata pieniędzy."
            };

            return templates[_random.Next(templates.Length)];
        }

        public async Task TestDatabaseConnection()
        {
            try
            {
                Console.WriteLine("\n=== Testing Database Connection ===");
                
                // Pobierz przykładowy dokument
                var samplePost = await _cosmosDbService.GetSamplePostAsync();
                
                if (samplePost != null)
                {
                    Console.WriteLine("Successfully retrieved sample post");
                    Console.WriteLine($"Sample post campaign: {samplePost.Campaign}");
                }
                else
                {
                    Console.WriteLine("No documents found in database");
                    
                    // Spróbuj zapisać testowy dokument
                    var testPost = new SocialMediaPost
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "Test post",
                        Source = "Test",
                        CreatedAt = DateTime.Now,
                        Campaign = "TEST"
                    };
                    
                    await _cosmosDbService.SavePostAsync(testPost);
                    Console.WriteLine("Successfully saved test post");
                }
                
                Console.WriteLine("=== Database Test Complete ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Database Test Failed ===");
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
} 