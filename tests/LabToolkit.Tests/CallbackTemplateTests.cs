using LabToolkit.Delegates;

namespace LabToolkit.Tests;

public class CallbackTemplateTests
{
    [Fact]
    public void Render_FiltersAndNumbersIncludedItems()
    {
        string text = CallbackTemplates.Render(Enumerable.Range(1, 6), x => x % 2 == 0, (i, x) => $"{i}:{x}", ",");
        Assert.Equal("0:2,1:4,2:6", text);
    }

    [Fact]
    public void RenderSections_CallbackOrderAndSkipping()
    {
        var log = new List<string>();
        string result = CallbackTemplates.RenderSections(
            new[] { "intro", "hidden", "outro" },
            s => s == "intro" ? new[] { "a", "b" } : new[] { "z" },
            s => s != "hidden",
            s => log.Add("start:" + s),
            (i, line) => { log.Add($"line:{i}:{line}"); return $"{i}{line}"; },
            (s, text) => { log.Add("end:" + s); return $"[{s}:{text}]"; });

        Assert.Equal("[intro:0a1b][outro:0z]", result);
        Assert.Equal(new[] { "start:intro", "line:0:a", "line:1:b", "end:intro",
                             "start:outro", "line:0:z", "end:outro" }, log);
    }

    [Fact]
    public void RenderSections_NoLines_StillCallsStartAndFinish()
    {
        int starts = 0;
        string result = CallbackTemplates.RenderSections(
            new[] { 1 }, _ => Array.Empty<string>(), _ => true, _ => starts++,
            (_, _) => throw new InvalidOperationException("brak linii"),
            (_, text) => $"({text})");
        Assert.Equal("()", result);
        Assert.Equal(1, starts);
    }

    [Fact]
    public void Transform_OptionalHooks()
    {
        var seen = new List<int>();
        var result = CallbackTemplates.Transform(new[] { 1, 2, 3, 4 }, x => x * x, where: x => x > 1, before: seen.Add);
        Assert.Equal(new[] { 4, 9, 16 }, result);
        Assert.Equal(new[] { 2, 3, 4 }, seen);
    }

    [Fact]
    public void Fold_ReportsEveryStep()
    {
        var steps = new List<int>();
        int sum = CallbackTemplates.Fold(new[] { 1, 2, 3 }, 0, (acc, x) => acc + x, (_, acc) => steps.Add(acc));
        Assert.Equal(6, sum);
        Assert.Equal(new[] { 1, 3, 6 }, steps);
    }

    [Fact]
    public void Retry_SucceedsAfterFailures_OrRethrowsLast()
    {
        int attempts = 0;
        var failures = new List<int>();
        int value = CallbackTemplates.Retry(
            () => ++attempts < 3 ? throw new InvalidOperationException() : 42,
            maxAttempts: 5,
            onFailure: (n, _) => failures.Add(n));

        Assert.Equal(42, value);
        Assert.Equal(new[] { 1, 2 }, failures);
        Assert.Throws<InvalidOperationException>(() =>
            CallbackTemplates.Retry<int>(() => throw new InvalidOperationException(), 2));
    }
}
