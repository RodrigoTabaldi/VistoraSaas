namespace Vistora.Api;

public static class JwtConfigurationValidator
{
    // Exige um público JWT sempre que a autenticação por autoridade estiver ativa.
    public static void Validate(string? authority, string? audience)
    {
        if (!string.IsNullOrWhiteSpace(authority) && string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Authentication:Audience is required when Authentication:Authority is configured.");
        }
    }
}
