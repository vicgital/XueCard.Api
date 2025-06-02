using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace XueCard.Api.Business.Infrastructure
{
    public static class AppConfigurationFactory
    {

        public static IConfiguration GetAppConfigurationFromConnectionString()
        {
            // Replace with your Azure App Configuration connection string
            string connectionString = Environment.GetEnvironmentVariable("XUECARD_APP_CONFIG_CNN_STRING") ?? throw new ArgumentNullException("XUECARD_APP_CONFIG_CNN_STRING Not found in Environment Variables");

            var config = new ConfigurationBuilder()
                .AddAzureAppConfiguration(options =>
                {
                    options.Connect(connectionString);
                    options.ConfigureKeyVault(kv => 
                    {
                        kv.SetCredential(new DefaultAzureCredential());
                    });
                })
                .Build();

            return config;
        }

    }
}
