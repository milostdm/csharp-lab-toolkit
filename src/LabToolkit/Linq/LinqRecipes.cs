namespace LabToolkit.Linq;

// =============================================================================
//  PRZEPISY LINQ — każdy to osobny, typowy wzorzec zapytania.
// =============================================================================
//  Odpowiedniki SQL:
//    Where = WHERE         Select = SELECT         OrderBy/ThenBy = ORDER BY
//    Take/Skip = TOP/LIMIT/OFFSET                  Join = INNER JOIN
//    GroupJoin + DefaultIfEmpty = LEFT JOIN        GroupBy = GROUP BY
//    Sum/Count/Average/Min/Max = agregaty          Distinct = DISTINCT
//    Any/All = EXISTS / NOT EXISTS
//
//  Wykonanie ODROCZONE: Where/Select/OrderBy/Join/GroupBy tylko budują zapytanie.
//  NATYCHMIASTOWE: ToList/ToArray/ToDictionary/ToLookup/Count/Sum/First/Any...
//  Zwracając List<...>, kończ ToList() — wynik nie zmieni się po zmianie danych.
//
//  Sortowanie LINQ jest STABILNE: przy równych kluczach zostaje kolejność wejścia.
//  Kolejne kryterium sortowania: ThenBy/ThenByDescending (NIE drugie OrderBy —
//  drugie OrderBy sortuje od nowa i "gubi" pierwsze kryterium).
// =============================================================================
public static class LinqRecipes
{
    // -------------------------------------------------------------------------
    //  1. FILTR -> SORTOWANIE -> TOP N -> PROJEKCJA
    //     Kolejność ma znaczenie: Take PO sortowaniu, Select NA KOŃCU
    //     (po Select nie ma już pól, po których sortowaliśmy).
    // -------------------------------------------------------------------------
    public static List<(string Title, int Rating)> TopTracks(MusicCatalog c, int n, string? genre = null) =>
        c.Tracks
            .Where(t => genre is null || t.Genre == genre)   // == na stringach rozróżnia wielkość liter
            .OrderByDescending(t => t.Rating)
            .ThenBy(t => t.Title)                            // rozstrzyganie remisów
            .Take(n)
            .Select(t => (t.Title, t.Rating))
            .ToList();

    // To samo w składni zapytań:
    public static List<(string Title, int Rating)> TopTracksQuery(MusicCatalog c, int n, string? genre = null) =>
        (from t in c.Tracks
         where genre == null || t.Genre == genre
         orderby t.Rating descending, t.Title
         select (t.Title, t.Rating))
        .Take(n)
        .ToList();

    // -------------------------------------------------------------------------
    //  2. JOIN (podwójny) -> GROUP BY -> SUM -> ORDER BY
    //     Suma wypłat dla kraju artysty. Pieniądze: decimal, nie double.
    //     Inner join: kraje bez odtworzeń nie pojawią się w wyniku.
    //     Join(inner, outerKey, innerKey, (outer, inner) => wynik)
    // -------------------------------------------------------------------------
    public static List<(string Country, decimal Total)> PayoutPerCountry(MusicCatalog c) =>
        c.Plays
            .Join(c.Tracks, p => p.TrackId, t => t.Id, (p, t) => new { p.Payout, t.ArtistId })
            .Join(c.Artists, x => x.ArtistId, a => a.Id, (x, a) => new { a.Country, x.Payout })
            .GroupBy(x => x.Country)
            .Select(g => (Country: g.Key, Total: g.Sum(x => x.Payout)))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Country)
            .ToList();

    // Podwójny join jest czytelniejszy w składni zapytań ("equals", nie "=="):
    public static List<(string Country, decimal Total)> PayoutPerCountryQuery(MusicCatalog c) =>
        (from p in c.Plays
         join t in c.Tracks on p.TrackId equals t.Id
         join a in c.Artists on t.ArtistId equals a.Id
         group p.Payout by a.Country into g
         let total = g.Sum()
         orderby total descending, g.Key
         select (Country: g.Key, Total: total))
        .ToList();

    // -------------------------------------------------------------------------
    //  3. LEFT JOIN z agregatem — WSZYSCY artyści, także ci z zerem.
    //     a) słownik pomocniczy TrackId -> sekundy (ToDictionary),
    //     b) GroupJoin: każdy artysta + kolekcja JEGO utworów (pusta, gdy brak),
    //     c) GetValueOrDefault: utwór bez odtworzeń -> 0.
    // -------------------------------------------------------------------------
    public static List<(string Artist, int Seconds)> ListeningSecondsPerArtist(MusicCatalog c)
    {
        Dictionary<int, int> secondsByTrack = c.Plays
            .GroupBy(p => p.TrackId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.SecondsListened));

        return c.Artists
            .GroupJoin(c.Tracks, a => a.Id, t => t.ArtistId,
                (a, tracks) => (Artist: a.Name, Seconds: tracks.Sum(t => secondsByTrack.GetValueOrDefault(t.Id))))
            .OrderByDescending(r => r.Seconds)
            .ThenBy(r => r.Artist)
            .ToList();
    }

    // -------------------------------------------------------------------------
    //  4. KLASYCZNY LEFT JOIN "wiersz po wierszu": join ... into + DefaultIfEmpty.
    //     Artysta bez utworów daje jeden wiersz z null po prawej stronie.
    // -------------------------------------------------------------------------
    public static List<(string Artist, string? Track)> ArtistTrackPairs(MusicCatalog c) =>
        (from a in c.Artists
         join t in c.Tracks on a.Id equals t.ArtistId into artistTracks
         from t in artistTracks.DefaultIfEmpty()      // brak dopasowania -> null
         select (a.Name, t?.Title))
        .ToList();

    // .NET 10+: wbudowany operator LeftJoin (prawa strona jako typ nullable).
    public static List<(string Artist, string? Track)> ArtistTrackPairsNet10(MusicCatalog c) =>
        c.Artists
            .LeftJoin(c.Tracks, a => a.Id, t => t.ArtistId, (a, t) => (a.Name, t?.Title))
            .ToList();

    // -------------------------------------------------------------------------
    //  5. ZLICZANIE W GRUPACH -> słownik
    // -------------------------------------------------------------------------
    public static Dictionary<string, int> TrackCountPerGenre(MusicCatalog c) =>
        c.Tracks
            .GroupBy(t => t.Genre)
            .ToDictionary(g => g.Key, g => g.Count());

    // .NET 9+: CountBy zwraca pary klucz -> liczba bez budowania grup.
    public static Dictionary<string, int> TrackCountPerGenreCountBy(MusicCatalog c) =>
        c.Tracks.CountBy(t => t.Genre).ToDictionary();

    // -------------------------------------------------------------------------
    //  6. ŚREDNIA W GRUPACH (Average zwraca double nawet dla int)
    // -------------------------------------------------------------------------
    public static List<(string Genre, double AverageRating)> AverageRatingPerGenre(MusicCatalog c) =>
        c.Tracks
            .GroupBy(t => t.Genre)
            .Select(g => (Genre: g.Key, AverageRating: Math.Round(g.Average(t => t.Rating), 2)))
            .OrderByDescending(r => r.AverageRating)
            .ToList();

    // -------------------------------------------------------------------------
    //  7. "ARGMAX" W GRUPIE: cały element z największą wartością (MaxBy, .NET 6+).
    //     Max(t => t.DurationSeconds) dałby tylko liczbę, MaxBy daje cały utwór.
    // -------------------------------------------------------------------------
    public static List<(string Artist, string Title, int DurationSeconds)> LongestTrackPerArtist(MusicCatalog c) =>
        c.Tracks
            .GroupBy(t => t.ArtistId)
            .Select(g => g.MaxBy(t => t.DurationSeconds)!)  // grupa nigdy nie jest pusta
            .Join(c.Artists, t => t.ArtistId, a => a.Id, (t, a) => (Artist: a.Name, t.Title, t.DurationSeconds))
            .OrderBy(r => r.Artist)
            .ToList();

    // -------------------------------------------------------------------------
    //  8. "NIE ISTNIEJE": HashSet + Contains (szybkie) albo Except na kluczach.
    // -------------------------------------------------------------------------
    public static List<string> TracksNeverPlayed(MusicCatalog c)
    {
        var playedIds = c.Plays.Select(p => p.TrackId).ToHashSet();
        return c.Tracks.Where(t => !playedIds.Contains(t.Id)).Select(t => t.Title).ToList();
    }

    public static List<string> ArtistsWithoutPlays(MusicCatalog c)
    {
        var playedArtistIds = c.Plays
            .Join(c.Tracks, p => p.TrackId, t => t.Id, (p, t) => t.ArtistId);
        var silentIds = c.Artists.Select(a => a.Id).Except(playedArtistIds).ToHashSet(); // Except usuwa też duplikaty
        return c.Artists.Where(a => silentIds.Contains(a.Id)).Select(a => a.Name).ToList();
    }

    // -------------------------------------------------------------------------
    //  9. DISTINCT + sortowanie napisów
    // -------------------------------------------------------------------------
    public static List<string> ListenersOfArtist(MusicCatalog c, string artistName) =>
        (from a in c.Artists
         where a.Name == artistName
         join t in c.Tracks on a.Id equals t.ArtistId
         join p in c.Plays on t.Id equals p.TrackId
         select p.Listener)
        .Distinct()
        .Order(StringComparer.Ordinal)   // Order() (.NET 7+) = OrderBy(x => x)
        .ToList();

    // -------------------------------------------------------------------------
    //  10. GRUPA -> LISTA unikalnych wartości (słownik list)
    // -------------------------------------------------------------------------
    public static Dictionary<string, List<string>> GenresPerCountry(MusicCatalog c) =>
        c.Tracks
            .Join(c.Artists, t => t.ArtistId, a => a.Id, (t, a) => (a.Country, t.Genre))
            .GroupBy(x => x.Country, x => x.Genre)       // drugi argument: CO trafia do grupy
            .ToDictionary(g => g.Key, g => g.Distinct().Order(StringComparer.Ordinal).ToList());

    // -------------------------------------------------------------------------
    //  11. STRONICOWANIE (strony od 1)
    // -------------------------------------------------------------------------
    public static List<T> Page<T>(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
    }

    // -------------------------------------------------------------------------
    //  12. SUMA + formatowanie czasu; Aggregate jako ogólny "fold"
    // -------------------------------------------------------------------------
    public static string TotalDuration(MusicCatalog c) =>
        TimeSpan.FromSeconds(c.Tracks.Sum(t => t.DurationSeconds)).ToString(@"hh\:mm\:ss");

    public static string TitlesJoined(MusicCatalog c) =>
        c.Tracks.Select(t => t.Title)
                .Aggregate((acc, title) => acc + " | " + title); // bez seed: pusta sekwencja -> wyjątek

    // -------------------------------------------------------------------------
    //  13. LOOKUP: jak słownik list, ale brak klucza -> pusta sekwencja (bez wyjątku).
    // -------------------------------------------------------------------------
    public static ILookup<string, string> TitlesByGenre(MusicCatalog c) =>
        c.Tracks.ToLookup(t => t.Genre, t => t.Title);

    // -------------------------------------------------------------------------
    //  14. GRUPOWANIE PO WYLICZONYM KLUCZU (przedziały)
    // -------------------------------------------------------------------------
    public static List<(string Bucket, int Count)> RatingBuckets(MusicCatalog c) =>
        c.Tracks
            .GroupBy(t => t.Rating / 10 * 10)            // dzielenie całkowite: 88 -> 80
            .OrderBy(g => g.Key)
            .Select(g => (Bucket: $"{g.Key}-{g.Key + 9}", Count: g.Count()))
            .ToList();

    // -------------------------------------------------------------------------
    //  15. NUMERACJA: Select z indeksem (indeks od 0!)
    // -------------------------------------------------------------------------
    public static List<string> RankedTitles(MusicCatalog c) =>
        c.Tracks
            .OrderByDescending(t => t.Rating)
            .ThenBy(t => t.Title)
            .Select((t, i) => $"{i + 1}. {t.Title}")
            .ToList();

    // -------------------------------------------------------------------------
    //  16. KILKA AGREGATÓW NA GRUPĘ
    // -------------------------------------------------------------------------
    public static List<(string Listener, int Plays, int Seconds)> ListenerSummary(MusicCatalog c) =>
        c.Plays
            .GroupBy(p => p.Listener)
            .Select(g => (Listener: g.Key, Plays: g.Count(), Seconds: g.Sum(p => p.SecondsListened)))
            .OrderByDescending(r => r.Seconds)
            .ToList();

    // -------------------------------------------------------------------------
    //  17. KWANTYFIKATORY: All / Any / Contains (zwracają bool, wykonują się od razu)
    //      All na pustej sekwencji = true, Any na pustej = false.
    // -------------------------------------------------------------------------
    public static (bool AllRatedAtLeast60, bool AnyLongerThan400, bool HasBlueHour) Checks(MusicCatalog c) =>
        (c.Tracks.All(t => t.Rating >= 60),
         c.Tracks.Any(t => t.DurationSeconds > 400),
         c.Tracks.Select(t => t.Title).Contains("Blue Hour"));
}
