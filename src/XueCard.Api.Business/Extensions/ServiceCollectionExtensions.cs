using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

namespace XueCard.Api.Business.Extensions
{
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds Serilog Logging
        /// </summary>
        /// <param name="services">instance of IServiceCollection</param>
        /// <param name="configuration">current app's IConfiguration</param>
        /// <returns></returns>
        public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
        {

            var provider = services.BuildServiceProvider();

            Log.Logger = new LoggerConfiguration()
              .WriteTo.Console(new CompactJsonFormatter())
              .ReadFrom.Configuration(configuration)
              .Enrich.WithSpan()
              .CreateLogger();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddDebug();
                loggingBuilder.AddSerilog();
            });

            return services;
        }

    }
}
