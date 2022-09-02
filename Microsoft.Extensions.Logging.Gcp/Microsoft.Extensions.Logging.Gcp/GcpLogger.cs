using System.Threading.Channels;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Gcp.Internal;

namespace Microsoft.Extensions.Logging.Gcp;

/// <summary>
///
/// </summary>
public sealed class GcpLogger : ILogger
{
  private readonly ChannelWriter<LogEntry> _channel;
  private readonly IExternalScopeProvider? _externalScopeProvider;

  /// <summary>
  /// Initialize a new instance of the <see cref="GcpLogger"/> class.
  /// </summary>
  /// <param name="channel">Channel used to write log events to.</param>
  /// <param name="externalScopeProvider">Storage for scopes.</param>
  public GcpLogger(ChannelWriter<LogEntry> channel, IExternalScopeProvider? externalScopeProvider)
  {
    _channel = channel;
    _externalScopeProvider = externalScopeProvider;
  }

  /// <inheritdoc/>
  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  )
  {
    var timeStamp = DateTime.UtcNow;
    var logEntry = new LogEntry
    {
      InsertId = eventId.ToString(),
      Severity = MapLogSeverity(logLevel),
      Timestamp = Timestamp.FromDateTime(timeStamp)
    };

    var jsonPayload = new Struct();
    jsonPayload.Fields.Add("message", Value.ForString(formatter(state, exception)));

    // Append state properties to properties.
    if (state is IEnumerable<KeyValuePair<string, object?>> stateDict)
      jsonPayload.Fields.Add("properties", Value.ForStruct(Map(stateDict)));

    logEntry.JsonPayload = jsonPayload;

    // Append scope's to log entry.
    _externalScopeProvider?.ForEachScope((scope, _) =>
    {
      if (scope is Dictionary<string, string?> activeScopeDict) logEntry.Labels.Add(activeScopeDict);
    }, state);

    // 256KB approximate max size for LogEntry in GCP quotas: https://cloud.google.com/logging/quotas
    // using 250KB here to be safe
    if (logEntry.CalculateSize() >= 250_000)
      Console.Error.WriteLine($"Log entry {logEntry} is to large for Google Cloud Logging.");

    if (_channel.TryWrite(logEntry) is false)
      Console.Error.WriteLine($"Failed to write log entry {logEntry} to channel.");
  }

  /// <inheritdoc/>
  public bool IsEnabled(LogLevel logLevel) => true;

  /// <inheritdoc/>
  public IDisposable BeginScope<TState>(TState state) => _externalScopeProvider?.Push(state) ?? NullScope.Instance;

  private static LogSeverity MapLogSeverity(LogLevel level) => level switch
  {
    LogLevel.Debug => LogSeverity.Debug,
    LogLevel.Information => LogSeverity.Info,
    LogLevel.Warning => LogSeverity.Warning,
    LogLevel.Error => LogSeverity.Error,
    LogLevel.Critical => LogSeverity.Critical,
    _ => LogSeverity.Default
  };

  private static Struct Map(IEnumerable<KeyValuePair<string, object?>> stateDict)
  {
    var jsonStruct = new Struct();

    foreach (var (key, value) in stateDict)
    {
      if ("{OriginalFormat}".Equals(key)) continue;
      switch (value)
      {
        case null:
          jsonStruct.Fields.Add(key, Value.ForNull());
          break;
        case bool boolValue:
          jsonStruct.Fields.Add(key, Value.ForBool(boolValue));
          break;
        case short or ushort or int or uint or long or ulong or float or double or decimal:
          jsonStruct.Fields.Add(key, Value.ForNumber(Convert.ToDouble(value)));
          break;
        case string stringValue:
          jsonStruct.Fields.Add(key, Value.ForString(stringValue));
          break;
        case Guid guidValue:
          jsonStruct.Fields.Add(key, Value.ForString(guidValue.ToString()));
          break;
      }
    }

    return jsonStruct;
  }
}
