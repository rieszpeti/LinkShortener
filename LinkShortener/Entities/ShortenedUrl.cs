using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Entities
{
    public class ShortenedUrl
    {
        public Guid Id { get; set; }

        [SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "This is a DB table field")]
        public string LongUrl { get; set; } = string.Empty;

        [SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "This is a DB table field")]
        public string ShortUrl { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public DateTime CreatedOnUtc { get; set; }
    }
}
