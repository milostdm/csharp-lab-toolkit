namespace LabToolkit.Linq;

// =============================================================================
//  PRZYKŁADOWE DANE do przepisów LINQ — mały katalog muzyczny w pamięci.
//  Relacje:  Track.ArtistId -> Artist.Id,   Play.TrackId -> Track.Id
// =============================================================================

public sealed record Artist(int Id, string Name, string Country);

public sealed record Track(int Id, string Title, int ArtistId, string Genre, int DurationSeconds, int Rating);

/// <summary>Jedno odtworzenie utworu przez słuchacza; Payout = wypłata za to odtworzenie.</summary>
public sealed record Play(int TrackId, string Listener, int SecondsListened, decimal Payout);

public sealed record MusicCatalog(
    IReadOnlyList<Artist> Artists,
    IReadOnlyList<Track> Tracks,
    IReadOnlyList<Play> Plays)
{
    public static MusicCatalog Sample() => new(
        Artists:
        [
            new(1, "Nova Lines", "PL"),
            new(2, "Echo Harbor", "UK"),
            new(3, "Pale Comet", "PL"),
            new(4, "Silent Orchard", "US"),   // brak utworów -> znika przy inner join
        ],
        Tracks:
        [
            new(1, "Midnight Grid", 1, "Synthpop", 214, 88),
            new(2, "Neon Rain",     1, "Synthpop", 198, 92),
            new(3, "Paper Boats",   2, "Indie",    245, 75),
            new(4, "Low Tide",      2, "Indie",    301, 92),
            new(5, "Blue Hour",     3, "Jazz",     412, 81),
            new(6, "Static Bloom",  3, "Synthpop", 187, 70),
            new(7, "Cold Coffee",   2, "Jazz",     356, 64),
            new(8, "Glass Garden",  1, "Indie",    233, 88),   // nikt nie słuchał
        ],
        Plays:
        [
            new(1, "ola",  214, 0.004m),
            new(2, "ola",  198, 0.004m),
            new(2, "kuba", 120, 0.003m),
            new(4, "kuba", 301, 0.004m),
            new(5, "ola",  412, 0.005m),
            new(5, "maja", 200, 0.003m),
            new(3, "maja", 245, 0.004m),
            new(6, "kuba", 187, 0.004m),
            new(7, "maja", 356, 0.005m),
            new(2, "maja", 198, 0.004m),
        ]);
}
