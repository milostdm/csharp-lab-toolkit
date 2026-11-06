using LabToolkit.Collections;
using LabToolkit.Iterators;

namespace LabToolkit.Tests;

public class IteratorAndCollectionTests
{
    [Fact]
    public void Range_StepsAndValidatesEagerly()
    {
        Assert.Equal(new[] { 0, 3, 6, 9 }, IteratorRecipes.Range(0, 10, 3));
        // Wyjątek już przy WYWOŁANIU, bez enumerowania (walidacja poza iteratorem):
        Assert.Throws<ArgumentOutOfRangeException>(() => IteratorRecipes.Range(0, 5, 0));
    }

    [Fact]
    public void Fibonacci_InfiniteButLazy() =>
        Assert.Equal(new long[] { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34 }, IteratorRecipes.Fibonacci().Take(10));

    [Fact]
    public void MyWhereMySelect_MatchLinq()
    {
        var data = new[] { 5, 8, 1, 9, 4 };
        Assert.Equal(data.Where(x => x > 4), data.MyWhere(x => x > 4));
        Assert.Equal(data.Select((x, i) => x * i), data.MySelect((x, i) => x * i));
        Assert.Equal(new[] { 5 }, data.MyTakeWhile(x => x < 6));
        Assert.Throws<ArgumentNullException>(() => IteratorRecipes.MyWhere<int>(null!, _ => true));
    }

    [Fact]
    public void DeferredExecution_QuerySeesLaterChanges()
    {
        var source = new List<int> { 1, 2, 3 };
        var query = source.MyWhere(x => x > 1);   // nic się jeszcze nie wykonało
        source.Add(4);
        Assert.Equal(new[] { 2, 3, 4 }, query);    // wykonanie dopiero tutaj
    }

    [Fact]
    public void EnumerationCounter_EachTerminalOperationEnumeratesAgain()
    {
        var counted = new EnumerationCounter<int>(new[] { 1, 2, 3 });
        var query = counted.Where(x => x > 1);
        Assert.Equal(0, counted.Enumerations);
        _ = query.Count();
        _ = query.ToList();
        Assert.Equal(2, counted.Enumerations);
    }

    [Fact]
    public void BatchAndCycle()
    {
        var batches = Enumerable.Range(1, 7).Batch(3).ToList();
        Assert.Equal(new[] { 3, 3, 1 }, batches.Select(b => b.Count));
        Assert.Equal(new[] { 7 }, batches[2]);
        Assert.Equal(new[] { 1, 2, 1, 2, 1 }, new[] { 1, 2 }.Cycle().Take(5));
        Assert.Empty(Array.Empty<int>().Cycle());
    }

    [Fact]
    public void RingBuffer_OverwritesOldest()
    {
        var buffer = new RingBuffer<string>(3);
        buffer.Add("a");
        buffer.Add("b");
        Assert.Equal(2, buffer.Count);
        Assert.False(buffer.IsFull);

        buffer.Add("c");
        buffer.Add("d");
        Assert.True(buffer.IsFull);
        Assert.Equal(new[] { "b", "c", "d" }, buffer);
        Assert.Equal("b", buffer[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }
}
