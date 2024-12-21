using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SentimentAnalysis.Core.Models;

namespace SentimentAnalysis.Core.Interfaces
{
    public interface ISocialMediaMonitor
    {
        Task<IEnumerable<SocialMediaPost>> GetPostsAsync(
            string[] keywords, 
            DateTime startDate, 
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
} 