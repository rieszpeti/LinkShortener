using LinkShortener.Database;
using LinkShortener.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LinkShortener.Services
{
    public class UrlShorteningService(ApplicationDbContext dbContext)
    {
        public async Task<string> GenerateUniqueCode()
        {
            var codeChars = new char[ShortLinkSettings.Length];
            int maxValue = ShortLinkSettings.Alphabet.Length;

            while (true)
            {
                for (var i = 0; i < ShortLinkSettings.Length; i++)
                {
                    var randomIndex = RandomNumberGenerator.GetInt32(maxValue);

                    codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
                }

                var code = new string(codeChars);

                if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == code).ConfigureAwait(false))
                {
                    return code;
                }
            }
        }
    }
}
