using System.Threading.Channels;
using FakeItEasy;
using FluentAssertions;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;

namespace Microsoft.Extensions.Logging.Gcp.Tests;

public sealed class GcpLoggerTests
{
  private readonly GcpLogger _logger;

  private readonly IExternalScopeProvider _fakeScopeProvider = A.Fake<IExternalScopeProvider>();
  private readonly Channel<LogEntry> _channel;

  public GcpLoggerTests()
  {
    _channel = Channel.CreateBounded<LogEntry>(10);
    _logger = new GcpLogger(_channel.Writer, _fakeScopeProvider);
  }

  [Fact]
  public void CanLog()
  {
    // Act.
    _logger.LogInformation("Something");

    // Assert.
    _channel.Reader.TryRead(out var logEntry).Should().BeTrue();
    logEntry?.InsertId.Should().Be("0");
    logEntry?.Severity.Should().Be(LogSeverity.Info);
    // logEntry?.Timestamp. // How to assert?
    logEntry?.PayloadCase.Should().Be(LogEntry.PayloadOneofCase.JsonPayload);
    logEntry?.Labels.Should().BeEmpty();
    logEntry?.JsonPayload.Fields.Should().HaveCount(2)
      .And.Contain("message", Value.ForString("Something"))
      .And.Contain("properties", Value.ForStruct(new Struct()));
  }
}
