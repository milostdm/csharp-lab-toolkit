using System.Text;

namespace LabToolkit.Extensions;

// =============================================================================
//  OPERACJE NA NAPISACH
// =============================================================================
//  string jest NIEMUTOWALNY: Replace/ToUpper/Trim... NIE zmieniają napisu,
//  tylko ZWRACAJĄ nowy. Zawsze używaj wyniku: s = s.Replace(...).
//  Doklejanie w pętli: StringBuilder (string += w pętli kopiuje cały tekst).
// =============================================================================
public static class StringExtensions
{
    /// <summary>
    /// Zamiany wykonywane PO KOLEI na całym tekście (jak łańcuch .Replace().Replace()).
    /// Uwaga na kaskadę: wynik wcześniejszej zamiany może zostać zmieniony przez późniejszą.
    /// "12" z [("1","2"), ("2","3")] -> "22" -> "33".
    /// </summary>
    public static string ReplaceSequential(this string text, params (string From, string To)[] rules)
    {
        foreach (var (from, to) in rules)
            text = text.Replace(from, to);
        return text;
    }

    /// <summary>
    /// Wszystkie zamiany "naraz", w jednym przejściu — bez kaskady.
    /// Przy kilku pasujących regułach wygrywa najdłuższy wzorzec.
    /// "12" z {1->2, 2->3} -> "23".
    /// </summary>
    public static string ReplaceSimultaneous(this string text, IReadOnlyDictionary<string, string> rules)
    {
        var patterns = rules.Keys.Where(k => k.Length > 0).OrderByDescending(k => k.Length).ToArray();
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            string? match = patterns.FirstOrDefault(p => text.AsSpan(i).StartsWith(p, StringComparison.Ordinal));
            if (match is null)
            {
                sb.Append(text[i]);
                i++;
            }
            else
            {
                sb.Append(rules[match]);
                i += match.Length;
            }
        }
        return sb.ToString();
    }

    /// <summary>Zamiana znak po znaku funkcją (np. cyfry na słowa). Znak bez zmiany: zwróć c.ToString().</summary>
    public static string MapChars(this string text, Func<char, string> map)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(map(c));
        return sb.ToString();
    }

    /// <summary>Liczba 1..3999 zapisana cyframi rzymskimi.</summary>
    public static string ToRoman(this int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, 3999);

        (int Value, string Symbol)[] table =
        [
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        ];
        var sb = new StringBuilder();
        foreach (var (value, symbol) in table)
        {
            while (number >= value)
            {
                sb.Append(symbol);
                number -= value;
            }
        }
        return sb.ToString();
    }

    public static string Repeat(this string text, int count) => string.Concat(Enumerable.Repeat(text, count));

    public static string JoinWith<T>(this IEnumerable<T> items, string separator) => string.Join(separator, items);

    public static string Surround(this string text, string left, string right) => left + text + right;

    public static string Truncate(this string text, int maxLength, string ellipsis = "…")
        => text.Length <= maxLength ? text : text[..maxLength] + ellipsis;
}
