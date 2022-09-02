using Google.Cloud.Logging.V2;

namespace Microsoft.Extensions.Logging.Gcp;

/// <summary>
/// Configuration for Google Cloud Platform logger.
/// </summary>
public sealed class GcpLoggerOptions
{
  /// <summary>
  /// ID (not name) of Google Cloud Platform project where logs will be sent.
  /// </summary>
  public string ProjectId { get; set; } = null!;

  /// <summary>
  /// Name of individual log.
  /// Optional, set to "Default" by default.
  /// </summary>
  public string LogName { get; set; } = "Default";

  /// <summary>
  /// Initializes a new instance of the <see cref="GcpLoggerOptions"/> class.
  /// </summary>
  /// <param name="projectId">Google Cloud Platform project id.</param>
  public GcpLoggerOptions(string projectId) => ProjectId = projectId;

  /// <summary>
  /// Initializes a new instance of the <see cref="GcpLoggerOptions"/> class.
  /// </summary>
  public GcpLoggerOptions()
  {
    /* NOOP */
  }

  /// <summary>
  /// Build GCP log name from <see cref="ProjectId"/> and <see cref="LogName"/>
  /// </summary>
  /// <returns>Log name to attach each log event sent to GCP.</returns>
  internal LogName BuildLogName() => new(ProjectId, LogName);
}
