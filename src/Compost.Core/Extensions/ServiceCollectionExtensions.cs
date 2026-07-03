using Compost.Core.Configuration;
using Compost.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Compost.Core.Extensions;

/// <summary>
/// Extension methods for registering Compost.Core services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ContextCorpusDictionary service with configuration support.
    /// Loads corpus from corpus.json or configuration section, falling back to hardcoded values.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration (optional)</param>
    /// <param name="configureOptions">Optional configuration action for CorpusConfig</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContextCorpus(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<CorpusConfig>? configureOptions = null)
    {
        // Configure CorpusConfig via IOptions if configuration or options action provided
        if (configuration != null)
        {
            services.Configure<CorpusConfig>(corpusConfig =>
            {
                // Bind from configuration section
                configuration.GetSection("Corpus").Bind(corpusConfig);
                
                // Apply custom configuration if provided
                configureOptions?.Invoke(corpusConfig);
            });
        }
        else if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register as singleton since corpus is loaded at startup and rarely changes
        services.AddSingleton<ContextCorpusDictionary>(provider =>
        {
            var logger = provider.GetService<ILogger<ContextCorpusDictionary>>();
            
            // Try to get configuration
            var config = provider.GetService<IConfiguration>();
            if (config != null)
            {
                return new ContextCorpusDictionary(config, logger);
            }
            
            // Fall back to hardcoded corpus if no configuration available
            logger?.LogWarning("No IConfiguration available, using hardcoded corpus");
            return new ContextCorpusDictionary();
        });

        return services;
    }

    /// <summary>
    /// Adds the full Compost.Core service stack including transcript extraction and corpus.
    /// </summary>
    public static IServiceCollection AddCompostCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddContextCorpus(configuration);
        services.AddHttpClient<ITranscriptContextExtractor, TranscriptContextExtractor>();
        
        return services;
    }
}
