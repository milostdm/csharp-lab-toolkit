using LabToolkit.Linq;

namespace LabToolkit.Tests;

public class LinqRecipesTests
{
    private readonly MusicCatalog _c = MusicCatalog.Sample();

    [Fact]
    public void TopTracks_FilterSortTakeProject()
    {
        Assert.Equal(new[] { ("Neon Rain", 92), ("Midnight Grid", 88) }, LinqRecipes.TopTracks(_c, 2, "Synthpop"));
        Assert.Equal(new[] { ("Low Tide", 92), ("Neon Rain", 92), ("Glass Garden", 88), ("Midnight Grid", 88) },
            LinqRecipes.TopTracks(_c, 4));
        Assert.Empty(LinqRecipes.TopTracks(_c, 3, "synthpop")); // wielkość liter ma znaczenie
        Assert.Equal(LinqRecipes.TopTracks(_c, 5), LinqRecipes.TopTracksQuery(_c, 5));
    }

    [Fact]
    public void PayoutPerCountry_DoubleJoinGroupSum()
    {
        Assert.Equal(new[] { ("PL", 0.027m), ("UK", 0.013m) }, LinqRecipes.PayoutPerCountry(_c));
        Assert.Equal(LinqRecipes.PayoutPerCountry(_c), LinqRecipes.PayoutPerCountryQuery(_c));
    }

    [Fact]
    public void ListeningSecondsPerArtist_LeftJoinKeepsZero() =>
        Assert.Equal(new[] { ("Echo Harbor", 902), ("Pale Comet", 799), ("Nova Lines", 730), ("Silent Orchard", 0) },
            LinqRecipes.ListeningSecondsPerArtist(_c));

    [Fact]
    public void ArtistTrackPairs_LeftJoinVariantsAgree()
    {
        var pairs = LinqRecipes.ArtistTrackPairs(_c);
        Assert.Equal(9, pairs.Count);
        Assert.Equal(("Silent Orchard", (string?)null), pairs[^1]);
        Assert.Equal(pairs, LinqRecipes.ArtistTrackPairsNet10(_c));
    }

    [Fact]
    public void CountsAndAverages()
    {
        var counts = LinqRecipes.TrackCountPerGenre(_c);
        Assert.Equal(3, counts["Synthpop"]);
        Assert.Equal(3, counts["Indie"]);
        Assert.Equal(2, counts["Jazz"]);
        Assert.Equal(counts, LinqRecipes.TrackCountPerGenreCountBy(_c));

        Assert.Equal(new[] { ("Indie", 85.0), ("Synthpop", 83.33), ("Jazz", 72.5) }, LinqRecipes.AverageRatingPerGenre(_c));
    }

    [Fact]
    public void LongestTrackPerArtist_MaxBy() =>
        Assert.Equal(new[] { ("Echo Harbor", "Cold Coffee", 356), ("Nova Lines", "Glass Garden", 233), ("Pale Comet", "Blue Hour", 412) },
            LinqRecipes.LongestTrackPerArtist(_c));

    [Fact]
    public void NotExists_Patterns()
    {
        Assert.Equal(new[] { "Glass Garden" }, LinqRecipes.TracksNeverPlayed(_c));
        Assert.Equal(new[] { "Silent Orchard" }, LinqRecipes.ArtistsWithoutPlays(_c));
    }

    [Fact]
    public void DistinctAndGroupedLists()
    {
        Assert.Equal(new[] { "kuba", "maja" }, LinqRecipes.ListenersOfArtist(_c, "Echo Harbor"));
        var genres = LinqRecipes.GenresPerCountry(_c);
        Assert.Equal(new[] { "Indie", "Jazz", "Synthpop" }, genres["PL"]);
        Assert.Equal(new[] { "Indie", "Jazz" }, genres["UK"]);
        Assert.False(genres.ContainsKey("US"));
    }

    [Fact]
    public void Paging()
    {
        var titles = _c.Tracks.Select(t => t.Title).Order(StringComparer.Ordinal);
        Assert.Equal(new[] { "Low Tide", "Midnight Grid", "Neon Rain" }, LinqRecipes.Page(titles, 2, 3));
        Assert.Equal(new[] { "Paper Boats", "Static Bloom" }, LinqRecipes.Page(titles, 3, 3));
        Assert.Empty(LinqRecipes.Page(titles, 4, 3));
    }

    [Fact]
    public void AggregatesLookupBuckets()
    {
        Assert.Equal("00:35:46", LinqRecipes.TotalDuration(_c));
        Assert.StartsWith("Midnight Grid | Neon Rain | ", LinqRecipes.TitlesJoined(_c));

        var lookup = LinqRecipes.TitlesByGenre(_c);
        Assert.Equal(new[] { "Blue Hour", "Cold Coffee" }, lookup["Jazz"]);
        Assert.Empty(lookup["Metal"]); // brak klucza -> pusta sekwencja, nie wyjątek

        Assert.Equal(new[] { ("60-69", 1), ("70-79", 2), ("80-89", 3), ("90-99", 2) }, LinqRecipes.RatingBuckets(_c));
    }

    [Fact]
    public void RankingSummaryChecks()
    {
        Assert.Equal(new[] { "1. Low Tide", "2. Neon Rain", "3. Glass Garden" }, LinqRecipes.RankedTitles(_c).Take(3));
        Assert.Equal(new[] { ("maja", 4, 999), ("ola", 3, 824), ("kuba", 3, 608) }, LinqRecipes.ListenerSummary(_c));
        Assert.Equal((true, true, true), LinqRecipes.Checks(_c));
    }
}
