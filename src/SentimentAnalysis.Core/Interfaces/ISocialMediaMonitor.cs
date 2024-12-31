using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SentimentAnalysis.Core.Models;

namespace SentimentAnalysis.Core.Interfaces
{
    public interface ISocialMediaMonitor
    {
        Task<IEnumerable<SocialMediaPost>> GetPostsAsync(string searchPhrase, int daysBack, string source);
        Task<IEnumerable<string>> GetAvailableSourcesAsync();
        Task<Dictionary<DateTime, int>> GetPostsTimelineAsync(string searchPhrase, int daysBack, string source);
    }
} 