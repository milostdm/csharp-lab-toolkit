namespace LabToolkit.Extensions;

// =============================================================================
//  METODY ROZSZERZAJĄCE DLA SŁOWNIKÓW
// =============================================================================
//  Metoda rozszerzająca = static metoda w static klasie, pierwszy parametr z "this".
//  Wywołanie dict.GetOrAdd(...) kompilator zamienia na DictionaryExtensions.GetOrAdd(dict, ...).
//
//  Ograniczenie "where TKey : notnull" — klucz słownika nie może być null.
//  Jeśli wartości stają się kluczami (odwracanie), one też potrzebują notnull.
// =============================================================================
public static class DictionaryExtensions
{
    /// <summary>Zwraca istniejącą wartość albo tworzy ją fabryką i dodaje. Jedno wyszukiwanie przy trafieniu.</summary>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = factory(key);
            dict.Add(key, value);
        }
        return value;
    }

    /// <summary>"Multimapa": dopisuje element do listy pod kluczem, tworząc listę przy pierwszym użyciu.</summary>
    public static void AddToBucket<TKey, TItem>(this IDictionary<TKey, List<TItem>> buckets, TKey key, TItem item)
        where TKey : notnull
        => buckets.GetOrAdd(key, _ => []).Add(item);

    /// <summary>
    /// Odwrócenie relacji klucz -> wartość na wartość -> lista kluczy.
    /// Kolejność kluczy na listach = kolejność, w jakiej źródło je zwraca przy foreach.
    /// Działa dla KAŻDEGO słownika (Dictionary, SortedDictionary, IDictionary, IReadOnlyDictionary),
    /// bo każdy z nich jest IEnumerable&lt;KeyValuePair&lt;K,V&gt;&gt;.
    /// Gdy potrzebna jest węższa sygnatura (np. "this IDictionary&lt;K,V&gt;"), wystarczy zmienić typ parametru.
    /// </summary>
    public static Dictionary<TValue, List<TKey>> ToReverseLookup<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        where TKey : notnull
        where TValue : notnull
    {
        var result = new Dictionary<TValue, List<TKey>>();
        foreach (var (key, value) in pairs)  // dekonstrukcja KeyValuePair
            result.AddToBucket(value, key);
        return result;
    }

    /// <summary>To samo co ToReverseLookup, w jednej linii LINQ (GroupBy zachowuje kolejność pierwszego wystąpienia).</summary>
    public static Dictionary<TValue, List<TKey>> ToReverseLookupLinq<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        where TValue : notnull
        => pairs.GroupBy(kv => kv.Value)
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

    /// <summary>Ile razy wystąpił każdy element. (.NET 9+: wbudowane CountBy(x => x).)</summary>
    public static Dictionary<T, int> Frequencies<T>(this IEnumerable<T> source) where T : notnull
    {
        var counts = new Dictionary<T, int>();
        foreach (var item in source)
            counts[item] = counts.GetValueOrDefault(item) + 1; // brak klucza -> default(int) = 0
        return counts;
    }

    /// <summary>Grupuje elementy do słownika list: klucz -> lista wybranych wartości.</summary>
    public static Dictionary<TKey, List<TElement>> GroupToDictionary<T, TKey, TElement>(
        this IEnumerable<T> source, Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
        where TKey : notnull
        => source.GroupBy(keySelector, elementSelector)
                 .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>Nowy słownik tylko z parami spełniającymi warunek (źródło bez zmian).</summary>
    public static Dictionary<TKey, TValue> WhereEntries<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs, Func<TKey, TValue, bool> predicate)
        where TKey : notnull
        => pairs.Where(kv => predicate(kv.Key, kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

    /// <summary>Nowy słownik z przekształconymi wartościami, te same klucze.</summary>
    public static Dictionary<TKey, TResult> MapValues<TKey, TValue, TResult>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs, Func<TValue, TResult> map)
        where TKey : notnull
        => pairs.ToDictionary(kv => kv.Key, kv => map(kv.Value));

    /// <summary>Scala "other" do "target"; przy konflikcie kluczy o wartości decyduje resolve(stara, nowa).</summary>
    public static void MergeFrom<TKey, TValue>(this IDictionary<TKey, TValue> target,
        IEnumerable<KeyValuePair<TKey, TValue>> other, Func<TValue, TValue, TValue> resolve)
        where TKey : notnull
    {
        foreach (var (key, value) in other)
            target[key] = target.TryGetValue(key, out var existing) ? resolve(existing, value) : value;
    }
}
