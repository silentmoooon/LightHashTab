using LightHashTab.Hashing;
using LightHashTab.Interop;
using Xunit;

namespace LightHashTab.Tests;

public class UiScaleTests
{
    [Theory]
    [InlineData(10, 1.0f, 10)]
    [InlineData(10, 1.25f, 13)]
    [InlineData(10, 1.5f, 15)]
    [InlineData(10, 2.0f, 20)]
    [InlineData(85, 1.5f, 128)]
    [InlineData(110, 1.5f, 165)]
    public void Scale_CalculatesCorrectDpiPixel(int value, float scale, int expected)
    {
        int result = Win32.Scale(value, scale);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlgorithmConfig_AllAlgorithms_ContainsNineStandardAlgorithms()
    {
        Assert.Equal(9, AlgorithmConfig.AllAlgorithms.Length);
        var defs = AlgorithmConfig.DefaultAlgorithms;
        Assert.Contains(HashAlgorithmType.Blake3, defs);
        Assert.Contains(HashAlgorithmType.Sha256, defs);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(2048, "2 KB")]
    [InlineData(326236, "318.59 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(334069760, "318.59 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatFileSize_CalculatesCorrectUnits(long bytes, string expected)
    {
        string result = UI.PropertySheetPage.FormatFileSize(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1024, "  ·  < 1毫秒")]
    [InlineData(0.5, 1024, "  ·  < 1毫秒")]
    [InlineData(2.4, 1024, "  ·  2毫秒")]
    [InlineData(50.0, 1024, "  ·  50毫秒")]
    public void FormatComputationSpeed_FastCalculation_ShowsMilliseconds(double elapsedMs, long fileSize, string expected)
    {
        string result = UI.PropertySheetPage.FormatComputationSpeed(elapsedMs, fileSize);
        Assert.Equal(expected, result);
    }
}
