using LinkShortener.Database;
using LinkShortener.Entities;
using LinkShortener.Models;
using LinkShortener.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace LinkShortener.Endpoints
{
    public static class UrlShortenerEndpoints
    {
        public static void MapUrlShortenerEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("shorten", async (
            ShortenUrlRequest shortenUrlRequest,
            UrlShorteningService urlShorteningService,
            ApplicationDbContext dbContext,
            HttpContext httpContext,
            IDistributedCache cache,
            CancellationToken ct) =>
            {
                if (!Uri.TryCreate(shortenUrlRequest.Url, UriKind.Absolute, out _))
                {
                    return Results.BadRequest("The specified URL is invalid.");
                }

                var code = await urlShorteningService.GenerateUniqueCode().ConfigureAwait(false);

                var request = httpContext.Request;

                var shortenedUrl = new ShortenedUrl
                {
                    Id = Guid.NewGuid(),
                    LongUrl = shortenUrlRequest.Url,
                    Code = code,
                    ShortUrl = $"{request.Scheme}://{request.Host}/{code}",
                    CreatedOnUtc = DateTime.UtcNow
                };

                dbContext.ShortenedUrls.Add(shortenedUrl);

                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);


#pragma warning disable S125
                var cacheKey = $"shortenedUrl-{code}";
                var cacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };

                var serializedShortenedUrl = System.Text.Json.JsonSerializer.Serialize(shortenedUrl);
                await cache.SetStringAsync(cacheKey, serializedShortenedUrl, cacheEntryOptions, ct).ConfigureAwait(false);

                return Results.Ok(shortenedUrl.ShortUrl);
#pragma warning restore S125
            });

            app.MapGet(
                "{code}", async ( 
                    string code,
                    ApplicationDbContext dbContext,
                    IDistributedCache cache,
                    CancellationToken ct) =>
                {
#pragma warning disable S125
                    var shortenedUrl = await cache.GetAsync($"shortenedUrl-{code}",
                        async token =>
                                {
                                    var shortenedUrl = await dbContext
                                        .ShortenedUrls
                                        .SingleOrDefaultAsync(s => s.Code == code, ct)
                                        .ConfigureAwait(false);

                                    return shortenedUrl;
                                },
                        CacheOptions.DefaultExpiration,
                        ct).ConfigureAwait(false);

                    //var shortenedUrl = await dbContext
                    //    .ShortenedUrls
                    //    .SingleOrDefaultAsync(s => s.Code == code, ct)
                    //    .ConfigureAwait(false);
#pragma warning restore S125

                    if (shortenedUrl is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Redirect(shortenedUrl.LongUrl);
                });
        }
    }
}
