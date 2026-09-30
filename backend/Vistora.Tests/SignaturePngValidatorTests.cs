using Vistora.Api;
using Xunit;

namespace Vistora.Tests;

public sealed class SignaturePngValidatorTests
{
    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+c3qoAAAAASUVORK5CYII=";

    [Fact]
    public void Accepts_a_complete_png_data_url()
    {
        var valid = SignaturePngValidator.TryDecode($"data:image/png;base64,{OnePixelPng}", out var png);

        Assert.True(valid);
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, png[..4]);
    }

    [Theory]
    [InlineData("data:image/jpeg;base64,AAAA")]
    [InlineData("data:image/png;base64,not-base64")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    public void Rejects_wrong_content_malformed_data_or_truncated_png(string dataUrl)
    {
        Assert.False(SignaturePngValidator.TryDecode(dataUrl, out _));
    }

    [Fact]
    public void Rejects_payloads_over_the_signature_limit()
    {
        var oversized = Convert.ToBase64String(new byte[256 * 1024 + 1]);

        Assert.False(SignaturePngValidator.TryDecode($"data:image/png;base64,{oversized}", out _));
    }
}
