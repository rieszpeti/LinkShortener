using LinkShortener.Database;
using LinkShortener.Entities;
using LinkShortener.Extensions;
using LinkShortener.Models;
using LinkShortener.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;

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

                var cachedUrl = await cache.GetStringAsync(shortenUrlRequest.Url, ct).ConfigureAwait(false);

                if (cachedUrl is not null)
                {
                    return Results.Ok(cachedUrl);
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

                var cacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                };

                await cache.SetStringAsync(shortenUrlRequest.Url, shortenedUrl.ShortUrl, cacheEntryOptions, ct).ConfigureAwait(false);

                return Results.Ok(shortenedUrl.ShortUrl);
            });

            //Redirect to url
            app.MapGet(
                "{code}", async ( 
                    string code,
                    ApplicationDbContext dbContext,
                    IDistributedCache cache,
                    //ILogger logger,
                    CancellationToken ct) =>
                {
#pragma warning disable S125
                    var uri = System.Net.WebUtility.UrlDecode(code);
                    //logger.LogDecodeUrl(uri);

                    if (uri is null)
                    {
                        return Results.NotFound("URL is invalid!");
                    }
                    if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
                    {
                        return Results.BadRequest("The specified URL is invalid.");
                    }

                    //var shortenedUrl = await cache.GetAsync(uri,
                    //    async token =>
                    //            {
                    //                var shortenedUrl = await dbContext
                    //                    .ShortenedUrls
                    //                    .SingleOrDefaultAsync(s => s.LongUrl == uri, ct)
                    //                    .ConfigureAwait(false);

                    //                return shortenedUrl;
                    //            },
                    //    CacheOptions.DefaultExpiration,
                    //    ct).ConfigureAwait(false);

                    var shortenedUrl = await dbContext
                        .ShortenedUrls
                        .SingleOrDefaultAsync(s => s.LongUrl == uri, ct)
                        .ConfigureAwait(false);
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
