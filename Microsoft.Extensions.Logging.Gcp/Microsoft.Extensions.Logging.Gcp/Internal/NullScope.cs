namespace Microsoft.Extensions.Logging.Gcp.Internal;

/// <summary>
/// An empty scope without any logic.
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    private NullScope()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        /* NOOP */
    }
}