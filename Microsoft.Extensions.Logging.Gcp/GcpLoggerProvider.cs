using System.Threading.Channels;
using Google.Api;
using Google.Api.Gax.Grpc;
using Google.Cloud.Logging.V2;
using Microsoft.Extensions.Logging.Gcp.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Gcp;

/// <inheritdoc cref="Microsoft.Extensions.Logging.ILoggerProvider" />
/// Provides <see cref="ILogger"/> instances targeting Google Cloud Platform Logging.
[ProviderAlias("GcpLogger")]
public sealed class GcpLoggerProvider : ILoggerProvider, ISupportExternalScope
{
  private GcpLoggerOptions _currentConfig;
  private IExternalScopeProvider? _scopeProvider;
  private readonly LoggingServiceV2Client _client;

  private readonly Channel<LogEntry> _channel;

  private readonly LogName _logName;
  private readonly MonitoredResource _monitoredResource;
  private readonly Dictionary<string, string> _labels = new(0);
  private readonly Task _outputThread;
  private CancellationTokenSource _cancellationTokenSource = new();

  private static readonly Action<LogEntry> DroppingLogEntryLogFunc = droppedLogEntry =>
    Console.Error.WriteLine($"Channel is full, dropping new logs.");

  /// <summary>
  /// Initializes a new instance of the <see cref="GcpLoggerProvider"/> class.
  /// </summary>
  /// <param name="config"></param>
  public GcpLoggerProvider(IOptionsMonitor<GcpLoggerOptions> config)
  {
    _currentConfig = config.CurrentValue;
    _logName = _currentConfig.BuildLogName();

    _client = LoggingServiceV2Client.Create();
    _monitoredResource = MonitoredResourceBuilder.FromPlatform();

    var channelConfig = new BoundedChannelOptions(100_000) {FullMode = BoundedChannelFullMode.DropNewest};
    _channel = Channel.CreateBounded(channelConfig, DroppingLogEntryLogFunc);

    // Start GCP message queue processor
    _outputThread = Task.Run(ProcessLogQueueAsync);
  }

  /// <inheritdoc/>
  public ILogger CreateLogger(string categoryName) => new GcpLogger(_channel.Writer, _scopeProvider);

  /// <inheritdoc/>
  public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

  private async Task ProcessLogQueueAsync()
  {
    var timeout = TimeSpan.FromSeconds(5); // Number of seconds between each batch emit when batch size > 0.

    await foreach (var batch in _channel.Reader.ReadAllBatches(1000, timeout, _cancellationTokenSource.Token))
    {
      await _client.WriteLogEntriesAsync(
        _logName,
        _monitoredResource,
        _labels,
        batch,
        _cancellationTokenSource.Token
      );
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    _channel.Writer.TryComplete();
    _cancellationTokenSource.Cancel();
    _outputThread.Dispose();
  }
}
