using Vistora.Worker;
using Xunit;

namespace Vistora.Tests;

public sealed class MessageRetryPolicyTests
{
    [Fact]
    public void Missing_retry_header_starts_at_zero()
    {
        Assert.Equal(0, MessageRetryPolicy.GetRetryCount(null));
    }

    [Theory]
    [InlineData((byte)2, 2)]
    [InlineData((short)3, 3)]
    [InlineData(4, 4)]
    [InlineData(5L, 5)]
    public void Numeric_retry_header_is_read_as_an_attempt_count(object headerValue, int expectedCount)
    {
        var headers = new Dictionary<string, object?>
        {
            [MessageRetryPolicy.RetryCountHeader] = headerValue
        };

        Assert.Equal(expectedCount, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData(-1)]
    [InlineData(2147483648L)]
    public void Invalid_retry_header_routes_directly_to_failed_queue(object headerValue)
    {
        var headers = new Dictionary<string, object?>
        {
            [MessageRetryPolicy.RetryCountHeader] = headerValue
        };

        Assert.False(MessageRetryPolicy.ShouldRetry(MessageRetryPolicy.GetRetryCount(headers)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(-1, false)]
    public void Retry_limit_is_bounded(int retryCount, bool expected)
    {
        Assert.Equal(expected, MessageRetryPolicy.ShouldRetry(retryCount));
    }
}
