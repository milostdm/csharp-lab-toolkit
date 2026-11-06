using LabToolkit.Delegates;

namespace LabToolkit.Tests;

public class DelegateTests
{
    [Fact]
    public void FourWaysToCreate_AllBehaveTheSame() =>
        Assert.All(DelegateBasics.FourWaysToCreate(), f => Assert.Equal(49, f(7)));

    [Fact]
    public void MakeCounter_EachCounterHasItsOwnVariable()
    {
        var a = DelegateBasics.MakeCounter();
        var b = DelegateBasics.MakeCounter(start: 10, step: 5);
        Assert.Equal(1, a());
        Assert.Equal(2, a());
        Assert.Equal(15, b());
        Assert.Equal(3, a());
    }

    [Fact]
    public void SharedCounter_LambdasSeeTheSameVariable()
    {
        var (increment, read, reset) = DelegateBasics.MakeSharedCounter();
        increment();
        increment();
        Assert.Equal(2, read());
        reset();
        Assert.Equal(0, read());
    }

    [Fact]
    public void LoopCapture_ForSharesVariable_ForeachAndCopyDoNot()
    {
        Assert.Equal(new[] { 3, 3, 3 }, DelegateBasics.CaptureForVariable());
        Assert.Equal(new[] { 0, 1, 2 }, DelegateBasics.CaptureCopyInFor());
        Assert.Equal(new[] { 0, 1, 2 }, DelegateBasics.CaptureForeachVariable());
    }

    [Fact]
    public void Composition_ThenPipelinePartial()
    {
        Func<int, int> addOne = x => x + 1;
        Func<int, string> show = x => $"<{x}>";
        Assert.Equal("<6>", addOne.Then(show)(5));

        Assert.Equal(13, DelegateBasics.Pipeline<int>(x => x + 1, x => x * 2, x => x + 1)(5));

        Func<int, int, int> power = (b, e) => (int)Math.Pow(b, e);
        Assert.Equal(8, power.Partial(2)(3));
    }

    [Fact]
    public void Memoize_CallsUnderlyingFunctionOncePerArgument()
    {
        int calls = 0;
        Func<int, int> slow = x => { calls++; return x * 10; };
        var fast = slow.Memoize();
        Assert.Equal(10, fast(1));
        Assert.Equal(10, fast(1));
        Assert.Equal(20, fast(2));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void WithCallCounter_CountsEveryCall()
    {
        Func<string, int> length = s => s.Length;
        var (wrapped, count) = length.WithCallCounter();
        wrapped("a");
        wrapped("bb");
        Assert.Equal(2, count());
    }

    [Fact]
    public void Multicast_RunsAllHandlers_FuncReturnsLast()
    {
        Assert.Equal(new[] { "A:x", "B:x", "A:y", "B:y" }, DelegateBasics.MulticastDemo());
        Assert.Equal(2, DelegateBasics.MulticastFuncReturnsLast());
    }

    [Fact]
    public void FuncToPredicate_WorksWithListFindAll()
    {
        Func<int, bool> even = x => x % 2 == 0;
        var numbers = new List<int> { 1, 2, 3, 4 };
        Assert.Equal(new[] { 2, 4 }, numbers.FindAll(even.ToPredicate()));
    }

    [Fact]
    public void Events_SubscribersAreNotified()
    {
        var reporter = new ProgressReporter();
        var seen = new List<int>();
        bool done = false;
        reporter.ProgressChanged += (_, percent) => seen.Add(percent);
        reporter.Completed += () => done = true;

        reporter.Run(4);

        Assert.Equal(new[] { 25, 50, 75, 100 }, seen);
        Assert.True(done);
    }
}
