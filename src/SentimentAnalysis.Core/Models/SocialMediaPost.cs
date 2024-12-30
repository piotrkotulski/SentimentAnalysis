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
        public double Negative { get; set; }
        public double Neutral { get; set; }

        public string Sentiment
        {
            get
            {
                const double VERY_HIGH = 0.8;    // Dla skrajnie pozytywnych/negatywnych
                const double HIGH = 0.6;         // Dla wyraźnie pozytywnych/negatywnych
                const double MODERATE = 0.4;     // Dla umiarkowanych opinii
                const double LOW = 0.2;          // Dla słabych opinii

                // Najpierw sprawdzamy skrajne przypadki
                if (Positive >= VERY_HIGH) return "Bardzo pozytywny";
                if (Negative >= VERY_HIGH) return "Bardzo negatywny";

                // Następnie wyraźne opinie
                if (Positive >= HIGH) return "Pozytywny";
                if (Negative >= HIGH) return "Negatywny";

                // Umiarkowane opinie
                if (Positive >= MODERATE && Positive > Negative) return "Umiarkowanie pozytywny";
                if (Negative >= MODERATE && Negative > Positive) return "Umiarkowanie negatywny";

                // Słabe opinie
                if (Positive >= LOW && Positive > Negative) return "Lekko pozytywny";
                if (Negative >= LOW && Negative > Positive) return "Lekko negatywny";

                // Jeśli żaden z powyższych warunków nie jest spełniony
                if (Math.Abs(Positive - Negative) < 0.1) return "Neutralny";
                if (Positive > Negative) return "Lekko pozytywny";
                return "Lekko negatywny";
            }
        }
    }
} 