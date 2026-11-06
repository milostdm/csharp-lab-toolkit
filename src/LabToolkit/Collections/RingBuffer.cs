using System.Collections;

namespace LabToolkit.Collections;

// =============================================================================
//  WŁASNA KOLEKCJA GENERYCZNA — bufor cykliczny o stałej pojemności.
//  Pokazuje: klasę generyczną, implementację IEnumerable<T> przez yield,
//  indeksator, IReadOnlyCollection<T>, wyjątki przy złym indeksie.
//  Po zapełnieniu nowy element nadpisuje NAJSTARSZY.
// =============================================================================
public sealed class RingBuffer<T> : IReadOnlyCollection<T>
{
    private readonly T[] _items;
    private int _start; // indeks najstarszego elementu
    private int _count;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _items = new T[capacity];
    }

    public int Capacity => _items.Length;
    public int Count => _count;
    public bool IsFull => _count == _items.Length;

    public void Add(T item)
    {
        int end = (_start + _count) % Capacity; // pierwsze wolne miejsce "za" ostatnim
        _items[end] = item;
        if (IsFull)
            _start = (_start + 1) % Capacity;   // nadpisaliśmy najstarszy -> przesuwamy początek
        else
            _count++;
    }

    /// <summary>Indeks 0 = najstarszy element.</summary>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count) // jedno porównanie łapie też ujemne
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[(_start + index) % Capacity];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return this[i];
    }

    // Niegeneryczna wersja wymagana przez IEnumerable — deleguje do generycznej.
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
