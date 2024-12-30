using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using SentimentAnalysis.Core.Models;
using SentimentAnalysis.Core.Interfaces;

namespace SentimentAnalysis.Infrastructure.Services
{
    public class DemoSocialMediaMonitor : ISocialMediaMonitor
    {
        private static readonly string[] DemoAuthors = new[] { "Użytkownik1", "Użytkownik2", "Użytkownik3", "Użytkownik4", "Użytkownik5" };
        private static readonly string[] DemoSources = new[] { "Twitter", "Facebook", "Instagram" };
        private readonly AzureSentimentAnalyzer _sentimentAnalyzer;
        private readonly CosmosDbService _cosmosDb;
        private readonly ILogger<DemoSocialMediaMonitor> _logger;

        public DemoSocialMediaMonitor(
            AzureSentimentAnalyzer sentimentAnalyzer, 
            CosmosDbService cosmosDb,
            ILogger<DemoSocialMediaMonitor> logger)
        {
            _sentimentAnalyzer = sentimentAnalyzer;
            _cosmosDb = cosmosDb;
            _logger = logger;
        }

        public async Task<IEnumerable<SocialMediaPost>> GetPostsAsync(
            string[] keywords, 
            DateTime startDate, 
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (keywords == null || !keywords.Any())
                {
                    _logger.LogWarning("No keywords provided");
                    return new List<SocialMediaPost>();
                }

                _logger.LogInformation($"Searching for posts with keywords: {string.Join(", ", keywords)}");
                
                var existingPosts = await _cosmosDb.GetPostsAsync(keywords, startDate, endDate);
                _logger.LogInformation($"Found {existingPosts.Count()} existing posts");
                
                if (existingPosts.Any())
                {
                    return existingPosts;
                }

                _logger.LogInformation("Generating demo posts");
                var posts = new List<SocialMediaPost>();
                var random = new Random();

                foreach (var keyword in keywords)
                {
                    _logger.LogInformation($"Generating posts for keyword: {keyword}");
                    
                    // Generuj 5 postów dla każdego słowa kluczowego
                    for (int i = 0; i < 5; i++)
                    {
                        var content = GenerateDemoContent(new[] { keyword }, random);
                        _logger.LogInformation($"Generated content: {content}");
                        
                        var sentimentScore = await _sentimentAnalyzer.AnalyzeSentimentAsync(content);
                        var post = new SocialMediaPost
                        {
                            Id = Guid.NewGuid().ToString(),
                            Author = DemoAuthors[random.Next(DemoAuthors.Length)],
                            Content = content,
                            CreatedAt = startDate.AddDays(random.Next((endDate - startDate).Days)),
                            Source = DemoSources[random.Next(DemoSources.Length)],
                            Campaign = "DemoCampaign",
                            Metadata = new Dictionary<string, string>(),
                            SentimentScore = sentimentScore
                        };

                        posts.Add(post);
                    }
                }

                _logger.LogInformation($"Generated {posts.Count} demo posts");
                await _cosmosDb.SavePostsAsync(posts);

                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPostsAsync: {ex.Message}");
                throw;
            }
        }

        private string GenerateDemoContent(string[] keywords, Random random)
        {
            var templates = new[]
            {
                "Jestem zachwycony produktem {0}! Świetna jakość i obsługa klienta 👍",
                "Niestety {0} mnie rozczarował. Słaba jakość i wysokie ceny 😞",
                "Produkt {0} spełnia moje oczekiwania, choć są pewne drobne niedociągnięcia",
                "Absolutnie nie polecam {0}. Strata czasu i pieniędzy! 😠",
                "Super doświadczenie z {0}! Polecam wszystkim! ❤️",
                "Średnie wrażenia z {0}. Nic specjalnego, ale też nie jest źle",
                "Tragedia! {0} to najgorszy produkt jaki używałem! Omijać szerokim łukiem! 😡",
                "{0} działa świetnie, jestem bardzo zadowolony z zakupu 😊",
                "Mam mieszane uczucia co do {0}. Są plusy i minusy",
                "Nie mogę się nachwalić {0}! Najlepszy zakup w tym roku! 🌟"
            };

            var template = templates[random.Next(templates.Length)];
            var keyword = keywords[random.Next(keywords.Length)];
            return string.Format(template, keyword);
        }
    }
} 