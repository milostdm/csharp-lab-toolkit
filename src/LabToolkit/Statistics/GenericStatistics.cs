using System.Numerics;
using LabToolkit.Extensions;

namespace LabToolkit.Statistics;

// =============================================================================
//  GENERIC MATH (.NET 7+) + STATYSTYKA OPISOWA
// =============================================================================
//  Bez ograniczenia na T nie da się napisać a + b ani a - b dla generycznego T.
//  Interfejsy z System.Numerics:
//    INumberBase<T>   -> + - * /, T.Zero, T.One, T.CreateChecked(x)
//    INumber<T>       -> powyższe + porównania (<, >), IComparable<T>, T.Min/T.Max
//                        (int, long, double, decimal, float, short, byte...)
//    IFloatingPoint<T>-> INumber + ułamki (double, float, decimal, Half)
//    IBinaryInteger<T>-> liczby całkowite (int, long...), operacje bitowe, %
//
//  T.CreateChecked(5)   -> stała 5 w typie T (rzuca przy przepełnieniu)
//  double.CreateChecked(t) -> konwersja T na double
//  UWAGA: dla typów całkowitych "/" obcina (7 / 2 == 3).
// =============================================================================

/// <summary>Konwencje kwartyli/kwantyli bez interpolacji (wynik jest zawsze elementem danych).</summary>
public enum QuantileMethod
{
    /// <summary>
    /// sorted[floor(p·n)]: element, przed którym w posortowanej tablicy stoi floor(p·n) elementów.
    /// Q1 = sorted[n/4], Q3 = sorted[3n/4] (dzielenie całkowite, indeksy od 0).
    /// </summary>
    CountBelow,

    /// <summary>
    /// "Nearest rank": sorted[ceil(p·n) − 1]: najmniejszy element, dla którego co najmniej p·n
    /// elementów jest ≤ niemu. Q1 = sorted[ceil(n/4) − 1].
    /// </summary>
    NearestRank,
}

public static class GenericStatistics
{
    public static T SumAll<T>(this IEnumerable<T> source) where T : INumberBase<T>
    {
        T total = T.Zero;
        foreach (var x in source)
            total += x;
        return total;
    }

    /// <summary>Średnia w typie T (dla int obcina!). Pusta sekwencja -> InvalidOperationException.</summary>
    public static T Mean<T>(this IEnumerable<T> source) where T : INumber<T>
    {
        T total = T.Zero;
        int count = 0;
        foreach (var x in source)  // jedno przejście: suma i licznik naraz
        {
            total += x;
            count++;
        }
        if (count == 0)
            throw new InvalidOperationException("Sekwencja jest pusta.");
        return total / T.CreateChecked(count);
    }

    /// <summary>Średnia jako double (bez obcinania dla typów całkowitych).</summary>
    public static double MeanAsDouble<T>(this IEnumerable<T> source) where T : INumber<T>
        => source.Select(x => double.CreateChecked(x)).Average(); // pusta -> InvalidOperationException

    /// <summary>Rozstęp: max − min.</summary>
    public static T Spread<T>(this IEnumerable<T> source) where T : INumber<T>
    {
        var (min, max) = source.MinMax();
        return max - min;
    }

    // -------------------------------------------------------------------------
    //  KWANTYLE BEZ INTERPOLACJI
    // -------------------------------------------------------------------------

    /// <summary>Indeks (od 0) kwantyla p w posortowanej tablicy długości n.</summary>
    public static int QuantileIndex(int n, double p, QuantileMethod method)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        if (p is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(p), "p musi być w [0, 1].");

        // Zaokrąglenie chroni przed błędem binarnym, np. 0.1 * 30 = 3.0000000000000004.
        double position = Math.Round(p * n, 9);
        int index = method switch
        {
            QuantileMethod.CountBelow => (int)Math.Floor(position),
            QuantileMethod.NearestRank => (int)Math.Ceiling(position) - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
        return Math.Clamp(index, 0, n - 1);
    }

    public static T Quantile<T>(this IEnumerable<T> source, double p, QuantileMethod method = QuantileMethod.CountBelow)
        where T : INumber<T>
    {
        T[] sorted = source.ToSortedArray();
        if (sorted.Length == 0)
            throw new InvalidOperationException("Sekwencja jest pusta.");
        return sorted[QuantileIndex(sorted.Length, p, method)];
    }

    /// <summary>
    /// Q1, mediana (Q2) i Q3 na liczbach całkowitych (bez double), więc indeksy są dokładne.
    /// minimumCount: mniej elementów -> InvalidOperationException.
    /// </summary>
    public static (T Q1, T Q2, T Q3) Quartiles<T>(this IEnumerable<T> source,
        QuantileMethod method = QuantileMethod.CountBelow, int minimumCount = 1)
        where T : INumber<T>
    {
        T[] s = source.ToSortedArray();      // kopia + sortowanie + jedno przejście po źródle
        int n = s.Length;
        if (n < Math.Max(1, minimumCount))
            throw new InvalidOperationException($"Potrzeba co najmniej {Math.Max(1, minimumCount)} elementów, jest {n}.");

        return method switch
        {
            // floor(k·n/4): dzielenie całkowite to podłoga dla liczb nieujemnych
            QuantileMethod.CountBelow => (s[n / 4], s[2 * n / 4], s[3 * n / 4]),
            // ceil(k·n/4) − 1 = (k·n + 3) / 4 − 1
            QuantileMethod.NearestRank => (s[(n + 3) / 4 - 1], s[(2 * n + 3) / 4 - 1], s[(3 * n + 3) / 4 - 1]),
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
    }

    /// <summary>Rozstęp ćwiartkowy Q3 − Q1 wybraną konwencją.</summary>
    public static T InterquartileRange<T>(this IEnumerable<T> source,
        QuantileMethod method = QuantileMethod.CountBelow, int minimumCount = 1)
        where T : INumber<T>
    {
        var (lower, _, upper) = source.Quartiles(method, minimumCount);
        return upper - lower;
    }

    // -------------------------------------------------------------------------
    //  KONWENCJE Z UŁAMKAMI (wymagają typu zmiennoprzecinkowego)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Interpolacja liniowa (Excel QUARTILE.INC / numpy domyślnie / R type 7):
    /// h = (n−1)·p, wynik = x[⌊h⌋] + (h − ⌊h⌋)·(x[⌊h⌋+1] − x[⌊h⌋]).
    /// </summary>
    public static T QuantileLinear<T>(this IEnumerable<T> source, double p) where T : IFloatingPoint<T>
    {
        if (p is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(p));
        T[] s = source.ToSortedArray();
        if (s.Length == 0)
            throw new InvalidOperationException("Sekwencja jest pusta.");

        double h = (s.Length - 1) * p;
        int lo = (int)Math.Floor(h);
        if (lo >= s.Length - 1)
            return s[^1];
        T fraction = T.CreateChecked(h - lo);
        return s[lo] + fraction * (s[lo + 1] - s[lo]);
    }

    /// <summary>Mediana; przy parzystej liczbie elementów średnia dwóch środkowych.</summary>
    public static T Median<T>(this IEnumerable<T> source) where T : IFloatingPoint<T>
    {
        T[] s = source.ToSortedArray();
        if (s.Length == 0)
            throw new InvalidOperationException("Sekwencja jest pusta.");
        return MedianOfSorted(s);
    }

    /// <summary>
    /// "Metoda szkolna" (mediany połówek): Q1 = mediana dolnej połowy, Q3 = mediana górnej.
    /// Przy nieparzystym n środkowy element NIE wchodzi do żadnej połowy.
    /// </summary>
    public static (T Q1, T Q3) QuartilesByHalves<T>(this IEnumerable<T> source) where T : IFloatingPoint<T>
    {
        T[] s = source.ToSortedArray();
        if (s.Length < 2)
            throw new InvalidOperationException("Potrzeba co najmniej 2 elementów.");
        int half = s.Length / 2;
        return (MedianOfSorted(s[..half]), MedianOfSorted(s[^half..]));
    }

    private static T MedianOfSorted<T>(T[] sorted) where T : IFloatingPoint<T>
    {
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / T.CreateChecked(2);
    }
}
