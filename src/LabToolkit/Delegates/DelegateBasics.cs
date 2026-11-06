namespace LabToolkit.Delegates;

// =============================================================================
//  DELEGATY — podstawy, domknięcia, kompozycja, multicast
// =============================================================================
//  Delegat = typ, którego wartością jest "referencja do funkcji".
//
//  Wbudowane typy (prawie zawsze wystarczą — własny typ delegatu rzadko jest potrzebny):
//    Action, Action<T1..T16>        -> zwraca void
//    Func<TResult>, Func<T1..T16, TResult>  -> OSTATNI parametr generyczny = typ wyniku
//    Predicate<T>                   -> to samo co Func<T,bool>, ale INNY typ
//                                      (List<T>.FindAll/RemoveAll go wymagają)
//    Comparison<T>                  -> (T, T) => int   (List<T>.Sort(Comparison<T>))
//    EventHandler / EventHandler<T> -> (object? sender, T args) => void
// =============================================================================

/// <summary>Własny typ delegatu. Zgodny "kształtem" z Func&lt;T,TResult&gt;, ale to inny typ.</summary>
public delegate TResult Transformer<in T, out TResult>(T input);

public static class DelegateBasics
{
    public static int Square(int x) => x * x;

    // -------------------------------------------------------------------------
    //  Cztery sposoby utworzenia tego samego delegatu
    // -------------------------------------------------------------------------
    public static IReadOnlyList<Func<int, int>> FourWaysToCreate()
    {
        Func<int, int> methodGroup = Square;                      // nazwa metody (bez nawiasów!)
        Func<int, int> expressionLambda = x => x * x;             // lambda wyrażeniowa
        Func<int, int> statementLambda = x => { int y = x * x; return y; }; // lambda blokowa
        Func<int, int> anonymousMethod = delegate (int x) { return x * x; }; // stara składnia (C# 2)
        return [methodGroup, expressionLambda, statementLambda, anonymousMethod];
    }

    // -------------------------------------------------------------------------
    //  Konwersje między typami delegatów o tym samym kształcie
    //  Func<T,bool> NIE jest automatycznie Predicate<T> — trzeba "opakować".
    // -------------------------------------------------------------------------
    public static Predicate<T> ToPredicate<T>(this Func<T, bool> func) => x => func(x);
    public static Func<T, bool> ToFunc<T>(this Predicate<T> predicate) => x => predicate(x);
    public static Func<T, TResult> ToFunc<T, TResult>(this Transformer<T, TResult> t) => t.Invoke;

    // -------------------------------------------------------------------------
    //  DOMKNIĘCIA (closures): lambda przechwytuje ZMIENNĄ, nie jej wartość.
    //  Zmienna żyje tak długo, jak długo żyje lambda — nawet po wyjściu z metody.
    // -------------------------------------------------------------------------

    /// <summary>Każde wywołanie MakeCounter tworzy NOWĄ, niezależną zmienną "current".</summary>
    public static Func<int> MakeCounter(int start = 0, int step = 1)
    {
        int current = start;
        return () => current += step; // przypisanie jest wyrażeniem -> zwraca nową wartość
    }

    /// <summary>Kilka lambd współdzielących JEDNĄ przechwyconą zmienną.</summary>
    public static (Action Increment, Func<int> Read, Action Reset) MakeSharedCounter()
    {
        int value = 0;
        return (() => value++, () => value, () => value = 0);
    }

    // Pułapka: zmienna pętli FOR jest jedna na całą pętlę -> wszystkie lambdy widzą
    // jej końcową wartość. Wynik: [3, 3, 3].
    public static List<int> CaptureForVariable()
    {
        var funcs = new List<Func<int>>();
        for (int i = 0; i < 3; i++)
            funcs.Add(() => i);
        return funcs.Select(f => f()).ToList();
    }

    // Naprawa: lokalna kopia w ciele pętli = nowa zmienna w każdej iteracji. Wynik: [0, 1, 2].
    public static List<int> CaptureCopyInFor()
    {
        var funcs = new List<Func<int>>();
        for (int i = 0; i < 3; i++)
        {
            int copy = i;
            funcs.Add(() => copy);
        }
        return funcs.Select(f => f()).ToList();
    }

    // FOREACH (od C# 5) tworzy nową zmienną w każdej iteracji. Wynik: [0, 1, 2].
    public static List<int> CaptureForeachVariable()
    {
        var funcs = new List<Func<int>>();
        foreach (int i in new[] { 0, 1, 2 })
            funcs.Add(() => i);
        return funcs.Select(f => f()).ToList();
    }

    // -------------------------------------------------------------------------
    //  KOMPOZYCJA i funkcje wyższego rzędu (biorą/zwracają funkcje)
    // -------------------------------------------------------------------------

    /// <summary>f.Then(g) == x => g(f(x)).</summary>
    public static Func<TIn, TOut> Then<TIn, TMid, TOut>(this Func<TIn, TMid> first, Func<TMid, TOut> second)
        => x => second(first(x));

    /// <summary>Składa dowolnie wiele przekształceń T -> T w jedno (od lewej do prawej).</summary>
    public static Func<T, T> Pipeline<T>(params Func<T, T>[] steps)
        => x => steps.Aggregate(x, (acc, step) => step(acc));

    /// <summary>Częściowa aplikacja: "zamraża" pierwszy argument.</summary>
    public static Func<T2, TResult> Partial<T1, T2, TResult>(this Func<T1, T2, TResult> f, T1 first)
        => second => f(first, second);

    /// <summary>Zapamiętuje wyniki (cache w domknięciu). Dla funkcji bez efektów ubocznych.</summary>
    public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> f) where T : notnull
    {
        var cache = new Dictionary<T, TResult>();
        return x =>
        {
            if (!cache.TryGetValue(x, out var result))
                cache[x] = result = f(x);
            return result;
        };
    }

    /// <summary>Opakowuje funkcję licznikiem wywołań — wzorzec "dekorator" z użyciem domknięcia.</summary>
    public static (Func<T, TResult> Wrapped, Func<int> CallCount) WithCallCounter<T, TResult>(this Func<T, TResult> f)
    {
        int calls = 0;
        return (x => { calls++; return f(x); }, () => calls);
    }

    // -------------------------------------------------------------------------
    //  MULTICAST: delegat może trzymać listę funkcji (+= dodaje, -= usuwa).
    //  Wywołanie odpala wszystkie po kolei. Dla Func zwracany jest wynik OSTATNIEJ.
    // -------------------------------------------------------------------------
    public static List<string> MulticastDemo()
    {
        var log = new List<string>();
        // Typ nullable, bo po usunięciu wszystkich funkcji (-=) delegat staje się null.
        Action<string>? handlers = s => log.Add("A:" + s);
        handlers += s => log.Add("B:" + s);
        handlers("x");                            // A:x, B:x

        Action<string> extra = s => log.Add("C:" + s);
        handlers += extra;
        handlers -= extra;                        // usuwać można tylko przez TĘ SAMĄ referencję
        handlers?.Invoke("y");                    // A:y, B:y
        return log;
    }

    public static int MulticastFuncReturnsLast()
    {
        Func<int> f = () => 1;
        f += () => 2;
        return f(); // 2 — wynik pierwszej funkcji przepada
    }

    // Bezpieczne wywołanie delegatu, który może być null:
    public static void InvokeIfSet(Action? maybeAction) => maybeAction?.Invoke();
}

// =============================================================================
//  ZDARZENIA (events): delegat, który z ZEWNĄTRZ klasy można tylko += i -=.
//  Wywołać (Invoke) może wyłącznie klasa, która go deklaruje.
// =============================================================================
public sealed class ProgressReporter
{
    public event EventHandler<int>? ProgressChanged; // (sender, procent)
    public event Action? Completed;

    public void Run(int steps)
    {
        for (int i = 1; i <= steps; i++)
            ProgressChanged?.Invoke(this, i * 100 / steps); // ?. — brak subskrybentów = null
        Completed?.Invoke();
    }
}
