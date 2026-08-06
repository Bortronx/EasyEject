using EasyEject.Core.Services;
using Xunit;

namespace EasyEject.Tests;

/// <summary>
/// Tests for <see cref="CapacityFormatter"/>.
/// </summary>
public class CapacityFormatterTests
{
    [Theory]
    [InlineData(null, "—")]
    [InlineData(0UL, "—")]
    [InlineData(16_000_000_000UL, "14.9 GB")]
    [InlineData(64_000_000_000UL, "59.6 GB")]
    [InlineData(1_000_000_000_000UL, "931.3 GB")]
    [InlineData(2_000_000_000_000UL, "1.8 TB")]
    [InlineData(2_097_152UL, "2.0 MB")]
    [InlineData(4096UL, "4.0 KB")]
    public void Format_ReturnsExpectedText(ulong? bytes, string expected)
    {
        Assert.Equal(expected, CapacityFormatter.Format(bytes));
    }
}
