using System.Text;
using Vistora.Api;
using Xunit;

namespace Vistora.Tests;

public sealed class UploadSignatureValidatorTests
{
    [Theory]
    [InlineData("image/jpeg", "FFD8FF000000000000000000")]
    [InlineData("image/png", "89504E470D0A1A0A")]
    [InlineData("image/webp", "524946460000000057454250")]
    [InlineData("application/pdf", "255044462D")]
    public async Task Accepted_uploads_have_the_expected_file_signature(string contentType, string hexHeader)
    {
        await using var stream = new MemoryStream(Convert.FromHexString(hexHeader));
        Assert.True(await UploadSignatureValidator.MatchesAsync(stream, contentType, CancellationToken.None));
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("application/pdf")]
    public async Task Html_disguised_as_an_allowed_upload_is_rejected(string contentType)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<html>malicious</html>"));
        Assert.False(await UploadSignatureValidator.MatchesAsync(stream, contentType, CancellationToken.None));
    }
}
