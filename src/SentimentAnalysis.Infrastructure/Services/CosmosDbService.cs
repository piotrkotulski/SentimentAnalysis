using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SentimentAnalysis.Core.Models;

namespace SentimentAnalysis.Infrastructure.Services
{
    public class CosmosDbService
    {
        private readonly Container _container;
        private readonly ILogger<CosmosDbService> _logger;
        private readonly string _endpointUri;
        private readonly string _primaryKey;
        private readonly string _databaseName;
        private readonly string _containerName;

        public CosmosDbService(
            string endpointUri,
            string primaryKey,
            string databaseName,
            string containerName,
            ILogger<CosmosDbService> logger)
        {
            _logger = logger;
            _endpointUri = endpointUri;
            _primaryKey = primaryKey;
            _databaseName = databaseName;
            _containerName = containerName;

            try
            {
                var client = new CosmosClient(endpointUri, primaryKey);
                // Upewnij się, że baza i kontener istnieją
                EnsureDatabaseAndContainerExistAsync().GetAwaiter().GetResult();
                _container = client.GetContainer(databaseName, containerName);
                _logger.LogInformation($"Successfully connected to Cosmos DB: {databaseName}/{containerName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to Cosmos DB: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureDatabaseAndContainerExistAsync()
        {
            try
            {
                var client = new CosmosClient(_endpointUri, _primaryKey);
                
                // Utwórz bazę danych jeśli nie istnieje
                var database = await client.CreateDatabaseIfNotExistsAsync(_databaseName);
                _logger.LogInformation($"Database ready: {_databaseName}");

                // Utwórz kontener jeśli nie istnieje
                var containerProperties = new ContainerProperties(_containerName, "/Campaign");
                var container = await database.Database.CreateContainerIfNotExistsAsync(containerProperties);
                _logger.LogInformation($"Container ready: {_containerName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to ensure database and container exist: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<SocialMediaPost>> GetPostsAsync(
            string[] keywords, 
            DateTime startDate, 
            DateTime endDate)
        {
            try
            {
                _logger.LogInformation($"Querying posts between {startDate} and {endDate} with keywords: {string.Join(", ", keywords)}");
                
                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.CreatedAt >= @startDate AND c.CreatedAt <= @endDate")
                    .WithParameter("@startDate", startDate)
                    .WithParameter("@endDate", endDate);

                if (keywords != null && keywords.Length > 0)
                {
                    // Zmienione zapytanie - sprawdzamy czy treść zawiera którekolwiek ze słów kluczowych
                    var conditions = string.Join(" OR ", keywords.Select((k, i) => $"CONTAINS(LOWER(c.Content), @keyword{i})"));
                    query = new QueryDefinition(
                        $"SELECT * FROM c WHERE c.CreatedAt >= @startDate AND c.CreatedAt <= @endDate AND ({conditions})")
                        .WithParameter("@startDate", startDate)
                        .WithParameter("@endDate", endDate);

                    // Dodaj parametry dla każdego słowa kluczowego
                    for (int i = 0; i < keywords.Length; i++)
                    {
                        query = query.WithParameter($"@keyword{i}", keywords[i].ToLower());
                    }
                }

                var posts = new List<SocialMediaPost>();
                var iterator = _container.GetItemQueryIterator<SocialMediaPost>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    posts.AddRange(response);
                    _logger.LogInformation($"Retrieved {response.Count} posts");
                }

                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error querying posts: {ex.Message}");
                throw;
            }
        }

        public async Task SavePostAsync(SocialMediaPost post)
        {
            try
            {
                _logger.LogInformation($"Saving post: {post.Id}");
                await _container.CreateItemAsync(post, new PartitionKey(post.Campaign));
                _logger.LogInformation($"Successfully saved post: {post.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save post {post.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task SavePostsAsync(IEnumerable<SocialMediaPost> posts)
        {
            foreach (var post in posts)
            {
                await SavePostAsync(post);
            }
        }
    }
} 