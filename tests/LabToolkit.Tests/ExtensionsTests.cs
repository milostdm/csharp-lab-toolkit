using LabToolkit.Extensions;

namespace LabToolkit.Tests;

public class DictionaryExtensionsTests
{
    [Fact]
    public void GetOrAdd_CallsFactoryOnlyWhenMissing()
    {
        var cache = new Dictionary<string, int>();
        int calls = 0;
        Assert.Equal(3, cache.GetOrAdd("abc", k => { calls++; return k.Length; }));
        Assert.Equal(3, cache.GetOrAdd("abc", k => { calls++; return -1; }));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void AddToBucket_CreatesListsLazily()
    {
        var buckets = new Dictionary<char, List<string>>();
        foreach (var word in new[] { "kot", "koc", "pies" })
            buckets.AddToBucket(word[0], word);
        Assert.Equal(new[] { "kot", "koc" }, buckets['k']);
        Assert.Equal(new[] { "pies" }, buckets['p']);
    }

    [Fact]
    public void ToReverseLookup_GroupsKeysInSourceOrder()
    {
        var colors = new Dictionary<string, string> { ["apple"] = "red", ["sky"] = "blue", ["cherry"] = "red" };
        var reverse = colors.ToReverseLookup();
        Assert.Equal(2, reverse.Count);
        Assert.Equal(new[] { "apple", "cherry" }, reverse["red"]);
        Assert.Equal(new[] { "sky" }, reverse["blue"]);

        var linq = colors.ToReverseLookupLinq();
        Assert.Equal(reverse["red"], linq["red"]);

        // Kolejność = kolejność foreach źródła (SortedDictionary -> rosnąco po kluczu)
        var sorted = new SortedDictionary<int, char> { [3] = 'x', [1] = 'x', [2] = 'y' };
        Assert.Equal(new[] { 1, 3 }, sorted.ToReverseLookup()['x']);
    }

    [Fact]
    public void Frequencies_CountsOccurrences()
    {
        var f = "mississippi".Frequencies();
        Assert.Equal(1, f['m']);
        Assert.Equal(4, f['i']);
        Assert.Equal(4, f['s']);
        Assert.Equal(2, f['p']);
    }

    [Fact]
    public void GroupToDictionary_WhereEntries_MapValues_MergeFrom()
    {
        var byLetter = new[] { "ala", "as", "bob" }.GroupToDictionary(w => w[0], w => w.Length);
        Assert.Equal(new[] { 3, 2 }, byLetter['a']);

        var prices = new Dictionary<string, int> { ["a"] = 5, ["b"] = 50, ["c"] = 500 };
        Assert.Equal(new[] { "b", "c" }, prices.WhereEntries((_, v) => v > 10).Keys.Order());
        Assert.Equal(10, prices.MapValues(v => v * 2)["a"]);

        var stock = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        stock.MergeFrom(new Dictionary<string, int> { ["b"] = 10, ["c"] = 3 }, (old, add) => old + add);
        Assert.Equal(12, stock["b"]);
        Assert.Equal(3, stock["c"]);
    }
}

public class EnumerableExtensionsTests
{
    [Fact]
    public void ToSortedArray_LeavesSourceUntouched()
    {
        var source = new[] { 3, 1, 2 };
        var sorted = source.ToSortedArray();
        Assert.Equal(new[] { 1, 2, 3 }, sorted);
        Assert.Equal(new[] { 3, 1, 2 }, source);
        Assert.NotSame(source, sorted);
    }

    [Fact]
    public void Materialize_ReusesListsButMaterializesLazySources()
    {
        var list = new List<int> { 1 };
        Assert.Same(list, list.Materialize());
        Assert.IsType<List<int>>(Enumerable.Range(0, 3).Select(x => x).Materialize());
    }

    [Fact]
    public void IndexHelpers()
    {
        Assert.Equal(new[] { (0, 'a'), (1, 'b') }, "ab".WithIndex());
        var log = new List<string>();
        new[] { "x", "y" }.ForEach((i, s) => log.Add($"{i}{s}"));
        Assert.Equal(new[] { "0x", "1y" }, log);
    }

    [Fact]
    public void Pairwise_AndIsSorted()
    {
        Assert.Equal(new[] { (1, 2), (2, 3) }, new[] { 1, 2, 3 }.Pairwise());
        Assert.Empty(new[] { 1 }.Pairwise());
        Assert.True(new[] { 1, 1, 2 }.IsSorted());
        Assert.False(new[] { 2, 1 }.IsSorted());
    }

    [Fact]
    public void TopBy_IsStableOnTies()
    {
        var words = new[] { "aa", "b", "cc", "ddd" };
        Assert.Equal(new[] { "ddd", "aa", "cc" }, words.TopBy(3, w => w.Length));
        Assert.Equal(new[] { "b", "aa" }, words.TopBy(2, w => w.Length, descending: false));
    }

    [Fact]
    public void EveryNth_MinMax_Flatten()
    {
        Assert.Equal(new[] { 0, 3, 6 }, Enumerable.Range(0, 8).EveryNth(3));
        Assert.Equal((-2, 9), new[] { 4, -2, 9, 0 }.MinMax());
        Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().MinMax());
        Assert.Equal(new[] { 1, 2, 3 }, new[] { new[] { 1 }, new[] { 2, 3 } }.Flatten());
    }
}

public class StringExtensionsTests
{
    [Fact]
    public void Sequential_Cascades_Simultaneous_DoesNot()
    {
        Assert.Equal("33", "12".ReplaceSequential(("1", "2"), ("2", "3")));
        Assert.Equal("23", "12".ReplaceSimultaneous(new Dictionary<string, string> { ["1"] = "2", ["2"] = "3" }));
    }

    [Fact]
    public void Simultaneous_PrefersLongestPattern() =>
        Assert.Equal("22 1", "abab a".ReplaceSimultaneous(new Dictionary<string, string> { ["a"] = "1", ["ab"] = "2" }));

    [Fact]
    public void MapChars_TransformsEachCharacter() =>
        Assert.Equal("a<1>b<2>", "a1b2".MapChars(c => char.IsDigit(c) ? $"<{c}>" : c.ToString()));

    [Theory]
    [InlineData(1, "I")]
    [InlineData(4, "IV")]
    [InlineData(9, "IX")]
    [InlineData(14, "XIV")]
    [InlineData(40, "XL")]
    [InlineData(90, "XC")]
    [InlineData(400, "CD")]
    [InlineData(1994, "MCMXCIV")]
    [InlineData(3999, "MMMCMXCIX")]
    public void ToRoman(int number, string expected) => Assert.Equal(expected, number.ToRoman());

    [Fact]
    public void ToRoman_OutOfRangeThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 0.ToRoman());
        Assert.Throws<ArgumentOutOfRangeException>(() => 4000.ToRoman());
    }

    [Fact]
    public void SmallHelpers()
    {
        Assert.Equal("ababab", "ab".Repeat(3));
        Assert.Equal("1-2-3", new[] { 1, 2, 3 }.JoinWith("-"));
        Assert.Equal("[x]", "x".Surround("[", "]"));
        Assert.Equal("abc…", "abcdef".Truncate(3));
        Assert.Equal("ab", "ab".Truncate(3));
    }
}
