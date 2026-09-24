using Xunit;

namespace ShopDocsV2.Application.Tests;

public class ColorMathTests
{
    [Theory]
    [InlineData("#EDEAE0", "#EDEAE0")]
    [InlineData("edeae0", "#EDEAE0")]
    [InlineData("  #edeae0 ", "#EDEAE0")]
    [InlineData("#EEE", "#EEEEEE")]
    [InlineData("abc", "#AABBCC")]
    public void ParseHexInput_ValidInput_NormalizesToHashUpperSixDigits(string input, string expected)
    {
        Assert.Equal(expected, ColorMath.ParseHexInput(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#EDEAE")]
    [InlineData("#GGGGGG")]
    [InlineData("237, 234, 224")]
    public void ParseHexInput_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(ColorMath.ParseHexInput(input));
    }

    [Theory]
    [InlineData("237, 234, 224", "#EDEAE0")]
    [InlineData("237,234,224", "#EDEAE0")]
    [InlineData("237 234 224", "#EDEAE0")]
    [InlineData("RGB 237, 234, 224", "#EDEAE0")]
    [InlineData("rgb(237, 234, 224)", "#EDEAE0")]
    [InlineData("0, 0, 0", "#000000")]
    [InlineData("255, 255, 255", "#FFFFFF")]
    public void ParseRgbInput_ValidInput_ConvertsToHex(string input, string expected)
    {
        Assert.Equal(expected, ColorMath.ParseRgbInput(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("256, 0, 0")]
    [InlineData("237, 234")]
    [InlineData("#EDEAE0")]
    public void ParseRgbInput_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(ColorMath.ParseRgbInput(input));
    }

    [Fact]
    public void ParseRgbInput_RoundTripsWithToRgbLabel()
    {
        var hex = ColorMath.ParseRgbInput(ColorMath.ToRgbLabel("#1A2B3C"));
        Assert.Equal("#1A2B3C", hex);
    }
}
