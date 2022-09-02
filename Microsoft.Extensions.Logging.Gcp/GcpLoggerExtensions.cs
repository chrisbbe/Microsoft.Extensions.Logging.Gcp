using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace Microsoft.Extensions.Logging.Gcp;

/// <summary>
/// Extensions to configure and add Google Cloud Platform (GCP) to <see cref="ILoggingBuilder"/>.
/// </summary>
public static class GcpLoggerExtensions
{
  /// <summary>
  /// Adds a Google Cloud Platform (GCP) logger named 'GcpLogger' to the factory.
  /// </summary>
  /// <param name="builder">The <see cref="ILoggerFactory"/> to use.</param>
  /// <param name="configure">A delegate to configure the <see cref="GcpLogger"/>.</param>
  /// <returns></returns>
  public static ILoggingBuilder AddGcpLogger(
    this ILoggingBuilder builder,
    Action<GcpLoggerOptions> configure
  )
  {
    builder.AddConfiguration();
    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, GcpLoggerProvider>());
    LoggerProviderOptions.RegisterProviderOptions<GcpLoggerOptions, GcpLoggerProvider>(builder.Services);
    builder.Services.Configure(configure);
    return builder;
  }
}
