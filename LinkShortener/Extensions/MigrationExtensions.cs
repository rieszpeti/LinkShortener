using LinkShortener.Database;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Extensions
{
    public static class MigrationExtensions
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app?.ApplicationServices.CreateScope();

            using var dbContext = scope?.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext?.Database.Migrate();
        }
    }
}
