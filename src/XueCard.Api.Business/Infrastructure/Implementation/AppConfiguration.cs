using Microsoft.Extensions.Configuration;
using XueCard.Api.Business.Infrastructure.Definition;

namespace XueCard.Api.Business.Infrastructure.Implementation
{
    public class AppConfiguration(IConfiguration config) : IAppConfiguration
    {
        private readonly IConfiguration _config = config;

        public string GetValue(string key)
        {
            var value = _config[key] ?? throw new ArgumentException($"{key} was not found in App Configuration");
            return value;
        }

        public string GetValue(string key, string defaultValue)
        {
            var value = _config[key] ?? defaultValue;
            return value;
        }
    }
}
