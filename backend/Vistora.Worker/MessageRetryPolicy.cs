namespace Vistora.Worker;

public static class MessageRetryPolicy
{
    public const string RetryCountHeader = "vistora-retry-count";
    public const int MaximumRetries = 5;
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    // Metadados inválidos vão para a fila de falhas para evitar tentativas infinitas.
    public static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCountHeader, out var value))
        {
            return 0;
        }

        return value switch
        {
            byte count => count,
            short count when count >= 0 => count,
            int count when count >= 0 => count,
            long count when count >= 0 && count <= int.MaxValue => (int)count,
            _ => MaximumRetries
        };
    }

    // O limite conta novas tentativas depois da entrega original.
    public static bool ShouldRetry(int retryCount) => retryCount is >= 0 and < MaximumRetries;
}
