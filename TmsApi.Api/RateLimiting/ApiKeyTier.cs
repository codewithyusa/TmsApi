namespace TmsApi.Api.RateLimiting;

public enum ApiKeyTier
{
    Anonymous,
    Free,
    Paid
}


public static class ApiKeyResolver
{
    private static readonly Dictionary<string, ApiKeyTier> Keys =
        new(StringComparer.Ordinal)
        {
            ["tms-free-demo-001"] = ApiKeyTier.Free,
            ["tms-paid-001"] = ApiKeyTier.Paid
        };


    public static (string PartitionKey, ApiKeyTier Tier) Resolve(
        HttpContext ctx)
    {
        var key = ctx.Request.Headers["X-Api-Key"]
            .ToString();


        // No API key: partition by IP
        if (string.IsNullOrWhiteSpace(key))
        {
            return
            (
                ctx.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
                ApiKeyTier.Anonymous
            );
        }


        // Known API key
        if (Keys.TryGetValue(key, out var tier))
        {
            return
            (
                key,
                tier
            );
        }


        // Unknown API key treated as anonymous tier
        return
        (
            key,
            ApiKeyTier.Anonymous
        );
    }
}