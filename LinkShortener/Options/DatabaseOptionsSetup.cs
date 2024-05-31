using Microsoft.Extensions.Options;

namespace LinkShortener.Options
{
    public class DatabaseOptionsSetup : IConfigureOptions<DatabaseOptions>
    {
        private const string ConfigurationSectionName = "DatabaseOptions";
        private readonly IConfiguration _configuration;

        public DatabaseOptionsSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(DatabaseOptions options)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            if (connectionString is null || connectionString.Length == 0)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            options.ConnectionString = connectionString;

            _configuration.GetSection(ConfigurationSectionName).Bind(options);
        }
    }
}
