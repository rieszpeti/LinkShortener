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
            ArgumentNullException.ThrowIfNull(options);

            var connectionString = _configuration.GetConnectionString("Database");

            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);

            options.ConnectionString = connectionString;

            _configuration.GetSection(ConfigurationSectionName).Bind(options);
        }
    }
}
