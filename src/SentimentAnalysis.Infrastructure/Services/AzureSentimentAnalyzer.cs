using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using SentimentAnalysis.Core.Models;

namespace SentimentAnalysis.Infrastructure.Services
{
    public class AzureSentimentAnalyzer
    {
        private readonly TextAnalyticsClient _client;

        public AzureSentimentAnalyzer(string endpoint, string apiKey)
        {
            var credentials = new AzureKeyCredential(apiKey);
            _client = new TextAnalyticsClient(new Uri(endpoint), credentials);
        }

        public async Task<SentimentScore> AnalyzeSentimentAsync(string text)
        {
            var response = await _client.AnalyzeSentimentAsync(text);
            var sentiment = response.Value;

            return new SentimentScore
            {
                Positive = sentiment.ConfidenceScores.Positive,
                Neutral = sentiment.ConfidenceScores.Neutral,
                Negative = sentiment.ConfidenceScores.Negative
            };
        }
    }
} 