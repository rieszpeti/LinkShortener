﻿using LinkShortener.Database;
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

                await cache.SetStringAsync(shortenedUrl.Code, shortenedUrl.Code, cacheEntryOptions, ct).ConfigureAwait(false);

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
                    var shortenedUrl = await cache.GetAsync(code,
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

                    if (shortenedUrl is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Redirect(shortenedUrl.LongUrl);
                });
        }
    }
}
