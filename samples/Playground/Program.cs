// =============================================================================
//  PLAYGROUND — szybki podgląd działania przepisów:  dotnet run --project samples/Playground
//  Wygodne miejsce do sprawdzenia fragmentu kodu, zanim przeniesiesz go gdzie indziej.
// =============================================================================
using LabToolkit.Delegates;
using LabToolkit.Extensions;
using LabToolkit.Iterators;
using LabToolkit.Linq;
using LabToolkit.Statistics;

Section("Delegaty i domknięcia");
var next = DelegateBasics.MakeCounter();
Console.WriteLine($"licznik: {next()}, {next()}, {next()}");
Console.WriteLine($"for: [{DelegateBasics.CaptureForVariable().JoinWith(", ")}]   kopia: [{DelegateBasics.CaptureCopyInFor().JoinWith(", ")}]");
Func<int, int> twice = x => x * 2;
Console.WriteLine($"kompozycja (x*2 potem +1)(5) = {twice.Then(x => x + 1)(5)}");

Section("Szablon z callbackami (sekcje i linie)");
int sectionNo = 0;
string doc = CallbackTemplates.RenderSections(
    sections: new[] { "Wstęp", "Szkic", "Koniec" },
    linesOf: _ => new[] { "alfa", "beta" },
    include: s => s != "Szkic",
    onSectionStart: _ => sectionNo++,
    renderLine: (i, line) => $"  {i + 1}) {line}\n",
    finishSection: (s, text) => $"{sectionNo.ToRoman()}. {s}\n{text}");
Console.Write(doc);

Section("Rozszerzenia");
var owners = new Dictionary<string, string> { ["Ada"] = "kot", ["Olek"] = "pies", ["Iga"] = "kot" };
foreach (var (pet, people) in owners.ToReverseLookup())
    Console.WriteLine($"{pet}: {people.JoinWith(", ")}");
Console.WriteLine("12".ReplaceSequential(("1", "2"), ("2", "3")) + " vs " +
                  "12".ReplaceSimultaneous(new Dictionary<string, string> { ["1"] = "2", ["2"] = "3" }));

Section("Iteratory");
Console.WriteLine($"Fibonacci: {IteratorRecipes.Fibonacci().Take(12).JoinWith(" ")}");
Console.WriteLine($"Batch(3): {Enumerable.Range(1, 8).Batch(3).Select(b => $"[{b.JoinWith(",")}]").JoinWith(" ")}");

Section("Kwartyle: porównanie konwencji (dane 3, 6, 9, ..., 3n)");
Console.WriteLine(" n | CountBelow | NearestRank | Linear      | Halves");
for (int n = 4; n <= 8; n++)
{
    var ints = Enumerable.Range(1, n).Select(k => 3 * k).ToArray();
    var doubles = ints.Select(x => (double)x).ToArray();
    var (cb1, _, cb3) = ints.Quartiles(QuantileMethod.CountBelow);
    var (nr1, _, nr3) = ints.Quartiles(QuantileMethod.NearestRank);
    var (h1, h3) = doubles.QuartilesByHalves();
    Console.WriteLine($" {n} | {cb1,3} {cb3,3}    | {nr1,3} {nr3,3}     | {doubles.QuantileLinear(0.25),4} {doubles.QuantileLinear(0.75),4}   | {h1} {h3}");
}
int[] skewed = [1, 2, 3, 100];
Console.WriteLine($"Rozstęp ćwiartkowy [1,2,3,100]: CountBelow={skewed.InterquartileRange(QuantileMethod.CountBelow)}, " +
                  $"NearestRank={skewed.InterquartileRange(QuantileMethod.NearestRank)}");

Section("LINQ na katalogu muzycznym");
var catalog = MusicCatalog.Sample();
Print("Top 3 utwory", LinqRecipes.TopTracks(catalog, 3));
Print("Wypłaty wg kraju", LinqRecipes.PayoutPerCountry(catalog));
Print("Sekundy słuchania wg artysty (left join)", LinqRecipes.ListeningSecondsPerArtist(catalog));
Print("Najdłuższy utwór artysty", LinqRecipes.LongestTrackPerArtist(catalog));
Print("Przedziały ocen", LinqRecipes.RatingBuckets(catalog));
Console.WriteLine($"Łączny czas: {LinqRecipes.TotalDuration(catalog)}");

static void Section(string title) => Console.WriteLine($"\n=== {title} ===");

static void Print<T>(string title, IEnumerable<T> rows)
{
    Console.WriteLine($"-- {title}");
    foreach (var row in rows)
        Console.WriteLine($"   {row}");
}
