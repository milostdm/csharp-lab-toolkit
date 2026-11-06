using LabToolkit.Iterators;
using LabToolkit.Statistics;

namespace LabToolkit.Tests;

public class StatisticsTests
{
    // Wielokrotności 3 (3, 6, 9, ..., 3n) podane w pomieszanej kolejności — po posortowaniu
    // element o indeksie i ma wartość 3·(i+1).
    private static int[] Multiples(int n) =>
        Enumerable.Range(1, n).Select(k => 3 * k).OrderByDescending(x => x % 4).ToArray();

    [Fact]
    public void SumAndMean_AreGeneric()
    {
        Assert.Equal(6, new[] { 1, 2, 3 }.SumAll());
        Assert.Equal(0.6m, new[] { 0.1m, 0.2m, 0.3m }.SumAll());
        Assert.Equal(1, new[] { 1, 2 }.Mean());          // int: dzielenie całkowite
        Assert.Equal(1.5, new[] { 1, 2 }.MeanAsDouble());
        Assert.Equal(1.5m, new[] { 1m, 2m }.Mean());
        Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Mean());
        Assert.Equal(11, new[] { 4, -2, 9 }.Spread());
    }

    [Theory]
    //          n  Q1  Q3
    [InlineData(4, 6, 12)]
    [InlineData(5, 6, 12)]
    [InlineData(6, 6, 15)]
    [InlineData(7, 6, 18)]
    [InlineData(8, 9, 21)]
    public void Quartiles_CountBelow(int n, int q1, int q3)
    {
        var (a, _, b) = Multiples(n).Quartiles(QuantileMethod.CountBelow);
        Assert.Equal((q1, q3), (a, b));
    }

    [Theory]
    [InlineData(4, 3, 9)]
    [InlineData(5, 6, 12)]
    [InlineData(6, 6, 15)]
    [InlineData(7, 6, 18)]
    [InlineData(8, 6, 18)]
    public void Quartiles_NearestRank(int n, int q1, int q3)
    {
        var (a, _, b) = Multiples(n).Quartiles(QuantileMethod.NearestRank);
        Assert.Equal((q1, q3), (a, b));
    }

    [Theory]
    [InlineData(4, 5.25, 9.75)]
    [InlineData(5, 6.0, 12.0)]
    [InlineData(6, 6.75, 14.25)]
    [InlineData(7, 7.5, 16.5)]
    [InlineData(8, 8.25, 18.75)]
    public void Quartiles_LinearInterpolation(int n, double q1, double q3)
    {
        var data = Multiples(n).Select(x => (double)x).ToArray();
        Assert.Equal(q1, data.QuantileLinear(0.25), 9);
        Assert.Equal(q3, data.QuantileLinear(0.75), 9);
    }

    [Theory]
    [InlineData(4, 4.5, 10.5)]
    [InlineData(5, 4.5, 13.5)]
    [InlineData(6, 6.0, 15.0)]
    [InlineData(7, 6.0, 18.0)]
    [InlineData(8, 7.5, 19.5)]
    public void Quartiles_MedianOfHalves(int n, double q1, double q3) =>
        Assert.Equal((q1, q3), Multiples(n).Select(x => (double)x).QuartilesByHalves());

    [Fact]
    public void InterquartileRange_ConventionsCanDifferALot()
    {
        var data = new[] { 1, 2, 3, 100 };
        Assert.Equal(98, data.InterquartileRange(QuantileMethod.CountBelow));  // 100 - 2
        Assert.Equal(2, data.InterquartileRange(QuantileMethod.NearestRank));  // 3 - 1
    }

    [Fact]
    public void InterquartileRange_MinimumCount_TypesAndSingleEnumeration()
    {
        Assert.Throws<InvalidOperationException>(() => new[] { 1, 2, 3, 4 }.InterquartileRange(minimumCount: 5));
        Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().InterquartileRange());
        Assert.Equal(35L, new[] { 70L, 5L, 25L, 50L, 15L }.InterquartileRange());       // 50 - 15
        Assert.Equal(1.75m, new[] { 1.25m, 0.75m, 2.5m, 4m, 3m }.InterquartileRange()); // 3 - 1.25

        var counted = new EnumerationCounter<int>(new[] { 12, 4, 20, 8, 16 });
        Assert.Equal(8, counted.InterquartileRange());                                  // 16 - 8
        Assert.Equal(1, counted.Enumerations);
    }

    [Fact]
    public void Quantile_FloatingPointGuardAndBounds()
    {
        var data = Enumerable.Range(1, 30).ToArray();
        Assert.Equal(3, data.Quantile(0.1, QuantileMethod.NearestRank)); // ceil(3) - 1 = 2 -> wartość 3
        Assert.Equal(4, data.Quantile(0.1, QuantileMethod.CountBelow));  // floor(3) = 3 -> wartość 4
        Assert.Equal(1, data.Quantile(0, QuantileMethod.NearestRank));
        Assert.Equal(30, data.Quantile(1, QuantileMethod.CountBelow));
        Assert.Throws<ArgumentOutOfRangeException>(() => data.Quantile(1.5));
    }

    [Fact]
    public void Median_OddEvenDecimal()
    {
        Assert.Equal(2.0, new[] { 3.0, 1.0, 2.0 }.Median());
        Assert.Equal(2.5, new[] { 4.0, 1.0, 3.0, 2.0 }.Median());
        Assert.Equal(2.5m, new[] { 1m, 2m, 3m, 4m }.QuantileLinear(0.5));
    }
}
