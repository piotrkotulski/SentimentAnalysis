namespace SentimentAnalysis.Core.Models
{
    public class SocialMediaPost
    {
        public SocialMediaPost()
        {
            Id = Guid.NewGuid().ToString();
            Metadata = new Dictionary<string, string>();
            Content = string.Empty;
            Author = string.Empty;
            Source = string.Empty;
            Campaign = string.Empty;
            SentimentScore = new SentimentScore();
        }

        public string Id { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; }
        public string Campaign { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public SentimentScore SentimentScore { get; set; }
    }

    public class SentimentScore
    {
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
        public string Sentiment => 
            Positive > Neutral && Positive > Negative ? "Pozytywny" :
            Negative > Neutral && Negative > Positive ? "Negatywny" : "Neutralny";
    }
} 