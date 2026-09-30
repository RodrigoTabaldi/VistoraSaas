using Vistora.Api.Endpoints;
using Xunit;

namespace Vistora.Tests;

public sealed class InvitationTokenTests
{
    [Fact]
    public void Created_tokens_are_url_safe_and_hashes_are_stable()
    {
        var token = InvitationToken.Create();

        Assert.True(InvitationToken.IsValid(token));
        Assert.Equal(64, InvitationToken.Hash(token).Length);
        Assert.Equal(InvitationToken.Hash(token), InvitationToken.Hash(token));
        Assert.NotEqual(InvitationToken.Hash(token), InvitationToken.Hash(token + "x"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a token")]
    [InlineData("0123456789012345678901234567890123456789012=")]
    public void Invalid_token_shapes_are_rejected(string token) => Assert.False(InvitationToken.IsValid(token));
}
