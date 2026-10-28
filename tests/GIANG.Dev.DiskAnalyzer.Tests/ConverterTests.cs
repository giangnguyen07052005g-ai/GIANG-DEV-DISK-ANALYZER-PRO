using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GIANG.Dev.DiskAnalyzer.App.Converters;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests;

public class ConverterTests
{
    [Fact]
    public void StringEqualsConverter_EvaluatesAndConvertsBackCorrectly()
    {
        var converter = new StringEqualsConverter();
        var culture = CultureInfo.InvariantCulture;

        // Equal match
        var matchResult = converter.Convert("Dashboard", typeof(bool), "Dashboard", culture);
        Assert.True((bool)matchResult);

        // Case-insensitive match
        var caseResult = converter.Convert("storageScan", typeof(bool), "STORAGESCAN", culture);
        Assert.True((bool)caseResult);

        // Non-match
        var nonMatch = converter.Convert("BigFiles", typeof(bool), "Dashboard", culture);
        Assert.False((bool)nonMatch);

        // Null handling
        var nullResult = converter.Convert(null, typeof(bool), "Dashboard", culture);
        Assert.False((bool)nullResult);

        // ConvertBack true
        var backTrue = converter.ConvertBack(true, typeof(string), "SafeCleanup", culture);
        Assert.Equal("SafeCleanup", backTrue);

        // ConvertBack false
        var backFalse = converter.ConvertBack(false, typeof(string), "SafeCleanup", culture);
        Assert.Equal(Binding.DoNothing, backFalse);
    }

    [Fact]
    public void IntEqualsConverter_EvaluatesAndConvertsBackCorrectly()
    {
        var converter = new IntEqualsConverter();
        var culture = CultureInfo.InvariantCulture;

        Assert.True((bool)converter.Convert(0, typeof(bool), "0", culture));
        Assert.True((bool)converter.Convert(2, typeof(bool), "2", culture));
        Assert.False((bool)converter.Convert(1, typeof(bool), "2", culture));

        Assert.Equal(3, converter.ConvertBack(true, typeof(int), "3", culture));
        Assert.Equal(Binding.DoNothing, converter.ConvertBack(false, typeof(int), "3", culture));
    }

    [Fact]
    public void EmptyToVisibilityConverter_ReturnsVisibleWhenEmptyOrZero()
    {
        var converter = new EmptyToVisibilityConverter();
        var culture = CultureInfo.InvariantCulture;

        Assert.Equal(Visibility.Visible, converter.Convert(0, typeof(Visibility), null, culture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(10, typeof(Visibility), null, culture));

        Assert.Equal(Visibility.Visible, converter.Convert(new List<string>(), typeof(Visibility), null, culture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(new List<string> { "sample.txt" }, typeof(Visibility), null, culture));

        Assert.Equal(Visibility.Visible, converter.Convert(null, typeof(Visibility), null, culture));
    }

    [Fact]
    public void InverseBoolToVisibilityConverter_FlipsProperly()
    {
        var converter = new InverseBoolToVisibilityConverter();
        var culture = CultureInfo.InvariantCulture;

        Assert.Equal(Visibility.Collapsed, converter.Convert(true, typeof(Visibility), null, culture));
        Assert.Equal(Visibility.Visible, converter.Convert(false, typeof(Visibility), null, culture));
    }
}
