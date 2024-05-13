using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Models
{
    public record ShortenUrlRequest
    {
        [SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "This is a response message")]
        public required string Url { get; set; }
    }
}
