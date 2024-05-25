namespace LinkShortener.Extensions
{
    public static partial class ApiGetLoggerExtensions
    {
        [LoggerMessage(EventId = 2000,
            EventName = "UrlDecodeInfo",
            Level = LogLevel.Information,
            Message = "Decoded URI: {validateUrl}")]
        public static partial void LogDecodeUrl(this ILogger logger, string validateUrl);
    }
}
