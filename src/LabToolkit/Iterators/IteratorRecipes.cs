namespace LabToolkit.Iterators;

// =============================================================================
//  ITERATORY (yield return) i WYKONANIE ODROCZONE
// =============================================================================
//  Metoda z "yield return" nie wykonuje się przy wywołaniu. Zwraca obiekt,
//  który policzy kolejne elementy dopiero przy iteracji (foreach / ToList...).
//  Każda kolejna iteracja uruchamia kod OD NOWA.
//
//  PUŁAPKA: walidacja argumentów w metodzie-iteratorze też jest odroczona!
//  Wyjątek poleci dopiero przy pierwszym MoveNext(), a nie przy wywołaniu.
//  Rozwiązanie: publiczna metoda waliduje od razu i zwraca wynik lokalnej
//  funkcji-iteratora (wzorzec użyty niżej w Range/MyWhere/MySelect).
// =============================================================================
public static class IteratorRecipes
{
    /// <summary>Liczby od start (włącznie) do end (wyłącznie) co step.</summary>
    public static IEnumerable<int> Range(int start, int endExclusive, int step = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, 0); // walidacja OD RAZU
        return Iterate();

        IEnumerable<int> Iterate() // lokalna funkcja-iterator: tu dopiero yield
        {
            for (int i = start; i < endExclusive; i += step)
                yield return i;
        }
    }

    /// <summary>Nieskończony ciąg — bezpieczny tylko z Take/TakeWhile/First.</summary>
    public static IEnumerable<long> Fibonacci()
    {
        (long a, long b) = (0, 1);
        while (true)
        {
            yield return a;
            (a, b) = (b, a + b);
        }
    }

    // -------------------------------------------------------------------------
    //  Własne odpowiedniki operatorów LINQ — tak (w uproszczeniu) działa System.Linq.
    // -------------------------------------------------------------------------
    public static IEnumerable<T> MyWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return Iterate();

        IEnumerable<T> Iterate()
        {
            foreach (var item in source)
                if (predicate(item))
                    yield return item;
        }
    }

    /// <summary>Select z indeksem (jak przeciążenie Select((x, i) => ...)).</summary>
    public static IEnumerable<TResult> MySelect<T, TResult>(this IEnumerable<T> source, Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return Iterate();

        IEnumerable<TResult> Iterate()
        {
            int index = 0;
            foreach (var item in source)
                yield return selector(item, index++);
        }
    }

    /// <summary>Pierwsze elementy, dopóki warunek jest spełniony (jak TakeWhile).</summary>
    public static IEnumerable<T> MyTakeWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            if (!predicate(item))
                yield break; // kończy iterację
            yield return item;
        }
    }

    /// <summary>Paczki po size elementów (ostatnia może być krótsza). Wbudowane: Chunk(size).</summary>
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        return Iterate();

        IEnumerable<List<T>> Iterate()
        {
            var batch = new List<T>(size);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == size)
                {
                    yield return batch;
                    batch = new List<T>(size); // NOWA lista — nie czyścimy już zwróconej!
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }
    }

    /// <summary>Powtarza sekwencję w nieskończoność (materializuje ją raz).</summary>
    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
    {
        var items = source.ToList();
        if (items.Count == 0)
            yield break;
        while (true)
            foreach (var item in items)
                yield return item;
    }
}

/// <summary>
/// Opakowanie licznika enumeracji — do sprawdzania, ile razy kod przechodzi po źródle.
/// (Przydaje się, gdy wymaganie brzmi "przejdź po sekwencji tylko raz".)
/// </summary>
public sealed class EnumerationCounter<T>(IEnumerable<T> inner) : IEnumerable<T>
{
    public int Enumerations { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerations++;
        return inner.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
