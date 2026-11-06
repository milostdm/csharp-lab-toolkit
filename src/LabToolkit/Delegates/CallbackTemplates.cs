using System.Text;

namespace LabToolkit.Delegates;

// =============================================================================
//  SZABLONY ALGORYTMÓW Z CALLBACKAMI
// =============================================================================
//  Metoda zna KOLEJNOŚĆ kroków, a wywołujący dostarcza, CO robi każdy krok.
//  Typowe zastosowanie: generowanie tekstu/raportu, przetwarzanie z "hakami".
//
//  Jak adaptować:
//   * zmień typy generyczne na konkretne typy z zadania,
//   * usuń/dodaj parametry-delegaty zgodnie z sygnaturą z polecenia,
//   * pilnuj KOLEJNOŚCI wywołań (testy często ją sprawdzają, zapisując log zdarzeń),
//   * pilnuj, KIEDY callback NIE jest wołany (np. po odrzuceniu elementu przez filtr).
// =============================================================================
public static class CallbackTemplates
{
    // -------------------------------------------------------------------------
    //  A. Jeden poziom: filtr -> formatowanie z indeksem -> sklejenie.
    //     Indeks liczony WŚRÓD PRZYJĘTYCH elementów (0, 1, 2...).
    // -------------------------------------------------------------------------
    public static string Render<T>(
        IEnumerable<T> items,
        Func<T, bool> include,
        Func<int, T, string> render,
        string separator = "")
    {
        var sb = new StringBuilder();
        int index = 0;
        foreach (var item in items)
        {
            if (!include(item))
                continue;
            if (index > 0)
                sb.Append(separator);       // separator tylko MIĘDZY elementami
            sb.Append(render(index, item));
            index++;
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    //  B. Dwa poziomy: sekcje i ich linie.
    //     Dla każdej sekcji:
    //       include(sekcja) == false -> pomiń sekcję (żaden inny callback dla niej)
    //       onSectionStart(sekcja)   -> raz, PRZED liniami (także gdy linii brak)
    //       renderLine(i, linia)     -> dla każdej linii, i od 0; wyniki doklejane
    //       finishSection(sekcja, tekst) -> przetwarza tekst CAŁEJ sekcji,
    //                                       wynik doklejany do rezultatu
    //     linesOf pozwala, by linie zależały od sekcji. Gdy lista linii jest
    //     wspólna dla wszystkich sekcji, przekaż: _ => wspolnaLista
    // -------------------------------------------------------------------------
    public static string RenderSections<TSection, TLine>(
        IEnumerable<TSection> sections,
        Func<TSection, IReadOnlyList<TLine>> linesOf,
        Func<TSection, bool> include,
        Action<TSection> onSectionStart,
        Func<int, TLine, string> renderLine,
        Func<TSection, string, string> finishSection)
    {
        var result = new StringBuilder();
        foreach (var section in sections)
        {
            if (!include(section))
                continue;

            onSectionStart(section);

            var lines = linesOf(section);
            var sectionText = new StringBuilder();     // nowy bufor dla KAŻDEJ sekcji
            for (int i = 0; i < lines.Count; i++)
                sectionText.Append(renderLine(i, lines[i]));

            result.Append(finishSection(section, sectionText.ToString()));
        }
        return result.ToString();
    }

    // -------------------------------------------------------------------------
    //  C. Opcjonalne haki: null = krok pomijany. Przydatne, gdy nie każdy
    //     wywołujący potrzebuje każdego kroku.
    // -------------------------------------------------------------------------
    public static List<TOut> Transform<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, TOut> map,
        Func<TIn, bool>? where = null,
        Action<TIn>? before = null,
        Action<TIn, TOut>? after = null)
    {
        var output = new List<TOut>();
        foreach (var item in items)
        {
            if (where is not null && !where(item))
                continue;
            before?.Invoke(item);
            var mapped = map(item);
            after?.Invoke(item, mapped);
            output.Add(mapped);
        }
        return output;
    }

    // -------------------------------------------------------------------------
    //  D. Akumulacja z podglądem kroków (ręczna wersja LINQ Aggregate).
    // -------------------------------------------------------------------------
    public static TAcc Fold<T, TAcc>(
        IEnumerable<T> items,
        TAcc seed,
        Func<TAcc, T, TAcc> step,
        Action<int, TAcc>? onStep = null)
    {
        var acc = seed;
        int i = 0;
        foreach (var item in items)
        {
            acc = step(acc, item);
            onStep?.Invoke(i++, acc);
        }
        return acc;
    }

    // -------------------------------------------------------------------------
    //  E. Ponawianie z callbackiem błędu (Func<T> = "przepis na wynik").
    // -------------------------------------------------------------------------
    public static T Retry<T>(Func<T> attempt, int maxAttempts, Action<int, Exception>? onFailure = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        for (int tryNo = 1; ; tryNo++)
        {
            try
            {
                return attempt();
            }
            catch (Exception ex) when (tryNo < maxAttempts) // filtr wyjątku: ostatnia porażka leci dalej
            {
                onFailure?.Invoke(tryNo, ex);
            }
        }
    }
}
