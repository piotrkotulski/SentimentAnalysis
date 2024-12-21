using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class MonitorSocialMedia
{
    private readonly ISocialMediaMonitor _monitor;
    private readonly AzureSentimentAnalyzer _analyzer;
    private readonly CosmosClient _cosmosClient;

    public MonitorSocialMedia(
        ISocialMediaMonitor monitor,
        AzureSentimentAnalyzer analyzer,
        CosmosClient cosmosClient)
    {
        _monitor = monitor;
        _analyzer = analyzer;
        _cosmosClient = cosmosClient;
    }

    [FunctionName("MonitorSocialMedia")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timer,
        ILogger log)
    {
        try
        {
            var container = _cosmosClient.GetContainer("SentimentDB", "Posts");
            var keywords = await GetKeywordsFromConfiguration();
            var posts = await _monitor.GetPostsAsync(
                keywords,
                DateTime.UtcNow.AddMinutes(-15),
                DateTime.UtcNow);

            foreach (var post in posts)
            {
                post.SentimentScore = await _analyzer.AnalyzeSentimentAsync(post.Content);
                await container.CreateItemAsync(post, new PartitionKey(post.Campaign));
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error monitoring social media");
            throw;
        }
    }

    private async Task<string[]> GetKeywordsFromConfiguration()
    {
        // TODO: Implement loading keywords from configuration
        return new[] { "ProductX", "#ProductXLaunch" };
    }
} 