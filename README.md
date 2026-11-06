# C# Lab Toolkit

Prywatny zbiór przepisów C# (.NET 10): delegaty i lambdy, metody rozszerzające, iteratory,
generic math i LINQ. Każdy przepis jest krótki, skomentowany po polsku i pokryty testami,
więc da się go skopiować i dopasować do własnych typów.

## Uruchamianie

```bash
dotnet test LabToolkit.slnx
```

```bash
dotnet run --project samples/Playground
```

Wymagane .NET SDK 10. Jedyne zależności to pakiety xUnit w projekcie testów.

## Struktura

```
src/LabToolkit/
├── Delegates/DelegateBasics.cs        Func/Action/Predicate, domknięcia, kompozycja, multicast, eventy
├── Delegates/CallbackTemplates.cs     szablony algorytmów z callbackami (filtr → hak → render → post)
├── Extensions/DictionaryExtensions.cs GetOrAdd, multimapa, odwracanie słownika, częstości, scalanie
├── Extensions/EnumerableExtensions.cs posortowana kopia, indeksy, Pairwise, TopBy, MinMax
├── Extensions/StringExtensions.cs     zamiany sekwencyjne vs jednoczesne, cyfry rzymskie, helpery
├── Statistics/GenericStatistics.cs    INumber<T>: suma, średnia, kwantyle (4 konwencje), rozstęp ćwiartkowy
├── Iterators/IteratorRecipes.cs       yield return, własne Where/Select, Batch, Cycle, licznik enumeracji
├── Collections/RingBuffer.cs          własna kolekcja generyczna z IEnumerable<T> i indeksatorem
└── Linq/                              17 wzorców zapytań na przykładowym katalogu muzycznym
samples/Playground/                    konsola pokazująca wyniki przepisów
tests/LabToolkit.Tests/                testy xUnit (84), które są też przykładami użycia
```

## Potrzebuję… → gdzie to jest

| Potrzebuję | Przepis |
|---|---|
| metody, która przyjmuje kilka funkcji i woła je w ustalonej kolejności | `CallbackTemplates.RenderSections`, `Render`, `Transform` |
| licznika lub stanu współdzielonego przez lambdy | `DelegateBasics.MakeCounter`, `MakeSharedCounter` |
| złożenia funkcji, memoizacji, licznika wywołań | `Then`, `Pipeline`, `Memoize`, `WithCallCounter` |
| zdarzenia (`event`) | `ProgressReporter` |
| słownika „wartość → lista kluczy” | `DictionaryExtensions.ToReverseLookup` (pętla) / `ToReverseLookupLinq` |
| słownika list budowanego w pętli | `AddToBucket`, `GetOrAdd` |
| zliczenia wystąpień | `Frequencies`, wbudowane `CountBy` |
| sortowania bez ruszania oryginału i jednego przejścia po źródle | `ToSortedArray` |
| funkcji działającej dla `int`, `double` i `decimal` naraz | `GenericStatistics` (`where T : INumber<T>`) |
| kwartyla / mediany / rozstępu ćwiartkowego | `Quartiles`, `Quantile`, `QuantileLinear`, `QuartilesByHalves`, `InterquartileRange` |
| zamiany wielu fragmentów tekstu | `ReplaceSequential` (kaskadowo) / `ReplaceSimultaneous` (naraz) |
| własnego operatora LINQ | `IteratorRecipes.MyWhere`, `MySelect`, `Batch` |
| filtr → sortowanie → top N → projekcja | `LinqRecipes.TopTracks` |
| join → group → sum → sort | `LinqRecipes.PayoutPerCountry` (także w składni zapytań) |
| left join | `ListeningSecondsPerArtist` (GroupJoin), `ArtistTrackPairs` (DefaultIfEmpty), `ArtistTrackPairsNet10` (`LeftJoin`) |
| elementu z maksimum w grupie | `LongestTrackPerArtist` (`MaxBy`) |
| „nie istnieje” | `TracksNeverPlayed` (HashSet), `ArtistsWithoutPlays` (`Except`) |

## Jak dopasować przepis

1. Skopiuj metodę i zmień typy generyczne na konkretne typy (albo zostaw generyczne).
2. Dopasuj sygnaturę **dokładnie** do wymagań: nazwy, kolejność parametrów, typ zwracany (`IDictionary` czy `Dictionary`, `List` czy `IEnumerable`).
3. Sprawdź warunki brzegowe: pusta sekwencja, brak dopasowań, remisy przy sortowaniu, wielkość liter w porównaniach.
4. Sprawdź, **kiedy** callbacki mają *nie* być wołane i w jakiej kolejności są wołane.
5. Odpal testy. Gdy coś nie przechodzi, wklej fragment do `Playground` i wypisz wartości pośrednie.

## Ściąga

### Delegaty

| Typ | Kształt |
|---|---|
| `Action`, `Action<T>` | `() => void`, `(T) => void` |
| `Func<TResult>`, `Func<T, TResult>`, `Func<T1, T2, TResult>` | ostatni parametr generyczny to **wynik** |
| `Predicate<T>` | `(T) => bool` (inny typ niż `Func<T,bool>`, wymaga go `List.FindAll`) |
| `Comparison<T>` | `(T, T) => int` |

- Lambda przechwytuje **zmienną**, nie wartość. Zmienna pętli `for` jest jedna na całą pętlę, w `foreach` jest nowa w każdej iteracji.
- Zmienna lokalna metody to nowy stan przy każdym wywołaniu. Pole klasy przeżywa między wywołaniami.
- `string` jest niemutowalny: `s.Replace(...)` zwraca nowy napis.

### Metody rozszerzające
`static class` + `static` metoda + `this` przy pierwszym parametrze. Przestrzeń nazw klasy musi być w `using`.

### Generic math

| Ograniczenie | Daje |
|---|---|
| `INumberBase<T>` | `+ - * /`, `T.Zero`, `T.One`, `T.CreateChecked(x)` |
| `INumber<T>` | to co wyżej, plus porównania, `IComparable<T>`, `T.Min`, `T.Max` |
| `IFloatingPoint<T>` | ułamki (`double`, `float`, `decimal`) |

Dzielenie dla typów całkowitych obcina. `3 / 4 == 0`, więc pisz `3 * n / 4`, a nie `3 / 4 * n`.

### Kwartyle: konwencje (dane 3, 6, 9, …, 3n)

| n | CountBelow Q1, Q3 | NearestRank Q1, Q3 | Linear Q1, Q3 | Halves Q1, Q3 |
|---|---|---|---|---|
| 4 | 6, 12 | 3, 9 | 5.25, 9.75 | 4.5, 10.5 |
| 5 | 6, 12 | 6, 12 | 6, 12 | 4.5, 13.5 |
| 6 | 6, 15 | 6, 15 | 6.75, 14.25 | 6, 15 |
| 7 | 6, 18 | 6, 18 | 7.5, 16.5 | 6, 18 |
| 8 | 9, 21 | 6, 18 | 8.25, 18.75 | 7.5, 19.5 |

- **CountBelow**: `sorted[n/4]`, `sorted[3n/4]`, czyli element, przed którym stoi 1/4 (3/4) elementów.
- **NearestRank**: `sorted[ceil(n/4) − 1]`.
- **Linear**: interpolacja jak w Excelu (QUARTILE.INC) i numpy.
- **Halves**: mediany dolnej i górnej połowy („metoda szkolna”).

Dla nierównomiernych danych konwencje dają bardzo różne wyniki: `[1,2,3,100]` ma rozstęp ćwiartkowy 98 (CountBelow) albo 2 (NearestRank).
Zanim wybierzesz konwencję, sprawdź na przykładzie z wymagań, która daje oczekiwany wynik.

### LINQ

- **Odroczone** (budują zapytanie): `Where`, `Select`, `OrderBy`, `Join`, `GroupBy`, `Skip`, `Take`, `Distinct`…
- **Natychmiastowe**: `ToList`, `ToArray`, `ToDictionary`, `ToLookup`, `Count`, `Sum`, `Average`, `First`, `Any`, `All`.
- Sortowanie jest **stabilne**. Drugie kryterium podajesz przez `ThenBy`, nie przez drugie `OrderBy`.
- Kolejność w łańcuchu: `Where` → `OrderBy` → `Take` → `Select` (po `Select` nie ma już pól do sortowania).
- `outer.Join(inner, outerKey, innerKey, (o, i) => wynik)` to inner join: elementy bez pary znikają.
- `GroupBy(klucz, element)` z drugim argumentem decyduje, co trafia do grupy.
- `ToLookup` przy brakującym kluczu zwraca pustą sekwencję, a słownik rzuca wyjątek.
- Pieniądze trzymaj w `decimal`. `int * decimal` daje `decimal`.
- `Average` zwraca `double` nawet dla `int`. `Aggregate` bez seed rzuca wyjątek na pustej sekwencji.
