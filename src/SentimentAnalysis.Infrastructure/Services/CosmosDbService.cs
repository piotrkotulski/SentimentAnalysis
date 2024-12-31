using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SentimentAnalysis.Core.Models;
using System.Threading;
using Newtonsoft.Json;

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
            Console.WriteLine("\n=== Initializing CosmosDB Service ===");
            Console.WriteLine($"EndpointUri: {endpointUri}");
            Console.WriteLine($"DatabaseName: {databaseName}");
            Console.WriteLine($"ContainerName: {containerName}");
            
            _logger = logger;
            _endpointUri = endpointUri;
            _primaryKey = primaryKey;
            _databaseName = databaseName;
            _containerName = containerName;

            try
            {
                Console.WriteLine("Creating CosmosClient...");
                var clientOptions = new CosmosClientOptions 
                { 
                    ConnectionMode = ConnectionMode.Gateway,
                    SerializerOptions = new CosmosSerializationOptions 
                    { 
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                        IgnoreNullValues = true
                    }
                };
                
                var client = new CosmosClient(endpointUri, primaryKey, clientOptions);
                
                Console.WriteLine($"Creating database if not exists: {databaseName}");
                var database = client.CreateDatabaseIfNotExistsAsync(
                    databaseName
                ).GetAwaiter().GetResult();

                Console.WriteLine("Checking if container exists...");
                var container = client.GetContainer(databaseName, containerName);
                bool containerExists = true;
                try
                {
                    var containerProperties = container.ReadContainerAsync()
                        .GetAwaiter().GetResult().Resource;
                    Console.WriteLine($"Found existing container with partition key: {containerProperties.PartitionKeyPath}");
                    
                    if (containerProperties.PartitionKeyPath != "/campaign")
                    {
                        Console.WriteLine("Container has incorrect partition key, deleting...");
                        container.DeleteContainerAsync().GetAwaiter().GetResult();
                        containerExists = false;
                    }
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    containerExists = false;
                }

                if (!containerExists)
                {
                    Console.WriteLine($"Creating new container: {containerName}");
                    var containerProperties = new ContainerProperties(containerName, "/campaign");
                    container = database.Database.CreateContainerIfNotExistsAsync(
                        containerProperties
                    ).GetAwaiter().GetResult();
                    Console.WriteLine("Container created successfully");
                }

                _container = client.GetContainer(databaseName, containerName);
                Console.WriteLine($"Successfully connected to Cosmos DB: {databaseName}/{containerName}");
                Console.WriteLine("=== CosmosDB Service Initialized ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Error Initializing CosmosDB Service ===");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                Console.WriteLine("=== Error Details End ===\n");
                throw;
            }
        }

        public async Task<IEnumerable<SocialMediaPost>> GetPostsAsync(string searchPhrase, DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"\n=== Querying Cosmos DB ===");
                Console.WriteLine($"Search Phrase: {searchPhrase}");
                Console.WriteLine($"Date Range: {startDate} to {endDate}");
                
                var queryText = @"
                    SELECT * FROM c 
                    WHERE c.CreatedAt >= @startDate 
                    AND c.CreatedAt <= @endDate 
                    AND CONTAINS(LOWER(c.Content), LOWER(@searchPhrase))";
                    
                Console.WriteLine($"Query: {queryText}");
                
                var query = new QueryDefinition(queryText)
                    .WithParameter("@startDate", startDate.ToString("o"))
                    .WithParameter("@endDate", endDate.ToString("o"))
                    .WithParameter("@searchPhrase", searchPhrase.ToLower());

                var posts = new List<SocialMediaPost>();
                var iterator = _container.GetItemQueryIterator<SocialMediaPost>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    Console.WriteLine($"Retrieved batch of {response.Count} posts");
                    posts.AddRange(response);
                }

                Console.WriteLine($"Total posts retrieved: {posts.Count}");
                Console.WriteLine("=== Query Complete ===\n");
                return posts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Error Querying Cosmos DB ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=== Error Details End ===\n");
                throw;
            }
        }

        public async Task SavePostAsync(SocialMediaPost post)
        {
            try
            {
                Console.WriteLine($"=== Saving Post to Cosmos DB ===");
                
                if (string.IsNullOrEmpty(post.Campaign))
                {
                    post.Campaign = "DEFAULT";
                }

                var documentToSave = new
                {
                    id = post.Id,
                    content = post.Content,
                    source = post.Source,
                    createdAt = post.CreatedAt,
                    sentiment = post.Sentiment,
                    campaign = post.Campaign.ToUpper()
                };

                var jsonSettings = new JsonSerializerSettings 
                { 
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                
                var jsonDocument = JsonConvert.SerializeObject(documentToSave, jsonSettings);
                Console.WriteLine($"Document to save:\n{jsonDocument}");

                var partitionKey = new PartitionKey(documentToSave.campaign);
                Console.WriteLine($"Using partition key: {documentToSave.campaign}");

                var options = new ItemRequestOptions 
                { 
                    EnableContentResponseOnWrite = false
                };

                await _container.CreateItemAsync(documentToSave, partitionKey, options);
                
                Console.WriteLine($"Successfully saved post: {post.Id}");
                Console.WriteLine("=== Save Complete ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Error Saving Post ===");
                Console.WriteLine($"Error: {ex.Message}");
                if (ex is CosmosException cosmosEx)
                {
                    Console.WriteLine($"Cosmos Status Code: {cosmosEx.StatusCode}");
                    Console.WriteLine($"Cosmos SubStatus Code: {cosmosEx.SubStatusCode}");
                    Console.WriteLine($"Cosmos Activity ID: {cosmosEx.ActivityId}");
                    Console.WriteLine($"Cosmos Request Charge: {cosmosEx.RequestCharge}");
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=== Error Details End ===\n");
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

        public async Task<SocialMediaPost> GetSamplePostAsync()
        {
            try
            {
                Console.WriteLine("\n=== Getting Sample Post ===");
                var query = new QueryDefinition("SELECT TOP 1 * FROM c");
                var iterator = _container.GetItemQueryIterator<SocialMediaPost>(query);
                
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var post = response.FirstOrDefault();
                    if (post != null)
                    {
                        Console.WriteLine("Sample document from database:");
                        Console.WriteLine(JsonConvert.SerializeObject(post, Formatting.Indented));
                    }
                    return post;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Error Getting Sample Post ===");
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
} 