using System.Numerics;

namespace LabToolkit.Extensions;

// =============================================================================
//  METODY ROZSZERZAJĄCE DLA SEKWENCJI (IEnumerable<T>)
// =============================================================================
//  Zasady, o które często pytają testy:
//   * NIE modyfikuj sekwencji wejściowej (sortuj KOPIĘ),
//   * przechodź po źródle JEDEN raz (IEnumerable bywa leniwe i jednorazowe),
//     więc najpierw ToArray()/ToList(), potem Count/indeksy/sortowanie,
//   * pusta sekwencja: albo wyjątek InvalidOperationException (jak LINQ Min/Max),
//     albo wartość domyślna — zależnie od wymagań.
// =============================================================================
public static class EnumerableExtensions
{
    /// <summary>Zawsze NOWA, posortowana tablica. Źródło nietknięte, enumerowane raz.</summary>
    public static T[] ToSortedArray<T>(this IEnumerable<T> source) where T : IComparable<T>
    {
        T[] copy = source.ToArray(); // ToArray tworzy nową tablicę nawet gdy source jest tablicą
        Array.Sort(copy);            // sortowanie w miejscu — ale na kopii
        return copy;
        // Alternatywa (.NET 7+): source.Order().ToArray()
    }

    /// <summary>Gdy źródło już jest listą/tablicą — zwraca je; inaczej materializuje. NIE sortuj wyniku w miejscu!</summary>
    public static IReadOnlyList<T> Materialize<T>(this IEnumerable<T> source)
        => source as IReadOnlyList<T> ?? source.ToList();

    /// <summary>Pary (indeks, element). (.NET 9+: wbudowane source.Index().)</summary>
    public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source)
        => source.Select((item, index) => (Index: index, Item: item));

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<int, T> action)
    {
        int index = 0;
        foreach (var item in source)
            action(index++, item);
    }

    /// <summary>Sąsiednie pary: [a,b,c] -> (a,b), (b,c).</summary>
    public static IEnumerable<(T Previous, T Current)> Pairwise<T>(this IEnumerable<T> source)
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            yield break;
        T previous = e.Current;
        while (e.MoveNext())
        {
            yield return (previous, e.Current);
            previous = e.Current;
        }
    }

    public static bool IsSorted<T>(this IEnumerable<T> source) where T : IComparable<T>
        => source.Pairwise().All(p => p.Previous.CompareTo(p.Current) <= 0);

    /// <summary>n "najlepszych" wg klucza. Sortowanie LINQ jest stabilne: remisy zachowują kolejność wejścia.</summary>
    public static List<T> TopBy<T, TKey>(this IEnumerable<T> source, int n, Func<T, TKey> keySelector, bool descending = true)
        => (descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector))
            .Take(n)
            .ToList();

    /// <summary>Co n-ty element, zaczynając od pierwszego.</summary>
    public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> source, int n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        return source.Where((_, i) => i % n == 0);
    }

    /// <summary>Min i max w JEDNYM przejściu. Pusta sekwencja -> InvalidOperationException.</summary>
    public static (T Min, T Max) MinMax<T>(this IEnumerable<T> source) where T : INumber<T>
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            throw new InvalidOperationException("Sekwencja jest pusta.");
        T min = e.Current, max = e.Current;
        while (e.MoveNext())
        {
            min = T.Min(min, e.Current);
            max = T.Max(max, e.Current);
        }
        return (min, max);
    }

    /// <summary>Spłaszcza sekwencję sekwencji (to samo co SelectMany(x => x)).</summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(x => x);
}
