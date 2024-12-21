using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SentimentAnalysis.Core.Models;
using SentimentAnalysis.Core.Interfaces;

namespace SentimentAnalysis.Infrastructure.Services
{
    public class DemoSocialMediaMonitor : ISocialMediaMonitor
    {
        private static readonly string[] DemoAuthors = new[] { "Użytkownik1", "Użytkownik2", "Użytkownik3", "Użytkownik4", "Użytkownik5" };
        private static readonly string[] DemoSources = new[] { "Twitter", "Facebook", "Instagram" };
        
        public async Task<IEnumerable<SocialMediaPost>> GetPostsAsync(
            string[] keywords, 
            DateTime startDate, 
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            // Symulacja opóźnienia sieciowego
            await Task.Delay(1000, cancellationToken);

            var random = new Random();
            var posts = new List<SocialMediaPost>();

            for (int i = 0; i < 20; i++)
            {
                var post = new SocialMediaPost
                {
                    Id = Guid.NewGuid().ToString(),
                    Author = DemoAuthors[random.Next(DemoAuthors.Length)],
                    Content = GenerateDemoContent(keywords, random),
                    CreatedAt = startDate.AddDays(random.Next((endDate - startDate).Days)),
                    Source = DemoSources[random.Next(DemoSources.Length)],
                    Campaign = "DemoCampaign",
                    Metadata = new Dictionary<string, string>(),
                    SentimentScore = GenerateDemoSentiment(random)
                };

                posts.Add(post);
            }

            return posts;
        }

        private string GenerateDemoContent(string[] keywords, Random random)
        {
            var templates = new[]
            {
                "Naprawdę lubię {0}! Świetny produkt!",
                "Nie jestem przekonany do {0}, wymaga poprawy",
                "Właśnie kupiłem {0} i jestem zachwycony!",
                "Mam problemy z {0}, ktoś jeszcze?",
                "Nie mogę się doczekać, żeby wypróbować {0}!"
            };

            var template = templates[random.Next(templates.Length)];
            var keyword = keywords[random.Next(keywords.Length)];
            return string.Format(template, keyword);
        }

        private SentimentScore GenerateDemoSentiment(Random random)
        {
            var positive = random.NextDouble();
            var negative = random.NextDouble() * (1 - positive);
            var neutral = 1 - positive - negative;

            return new SentimentScore
            {
                Positive = positive,
                Negative = negative,
                Neutral = neutral
            };
        }
    }
} 