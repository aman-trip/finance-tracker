namespace FinanceTracker.Api.Options;

public sealed class CorsOptions
{
    public string AllowedOrigins { get; init; } = "http://localhost:4173,http://localhost:5173,http://127.0.0.1:4173,http://127.0.0.1:5173";
}
