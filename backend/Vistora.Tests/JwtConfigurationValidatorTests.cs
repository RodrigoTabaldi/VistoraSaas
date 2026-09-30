using Vistora.Api;
using Xunit;

namespace Vistora.Tests;

public sealed class JwtConfigurationValidatorTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("https://identity.example", "vistora-api")]
    public void Validate_accepts_cookie_only_or_complete_bearer_configuration(string? authority, string? audience)
    {
        var exception = Record.Exception(() => JwtConfigurationValidator.Validate(authority, audience));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_requires_an_audience_when_authority_is_configured(string? audience)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.Validate("https://identity.example", audience));

        Assert.Contains("Authentication:Audience", exception.Message);
    }
}
