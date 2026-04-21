using System.Runtime.CompilerServices;

namespace Tokenization.Tests.Shared.Utils.Cache;

public static class TestCacheKey
{
    private static readonly string RunId = Guid.NewGuid().ToString("N");
    private static int _counter = 0;

    /// <summary>
    /// Produces a unique, human-readable keyName per call:
    /// e.g. "kek_GetAllClients_Works_caseA_7f1c..._3"
    /// </summary>
    public static string New(
        string? caseId = null,
        [CallerMemberName] string? testName = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        var name = testName ?? "test";
        var tag = string.IsNullOrWhiteSpace(caseId) ? "" : $"_{Sanitize(caseId)}";
        return $"kek_{name}{tag}_{RunId}_{idx}";
    }

    private static string Sanitize(string s) => new(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}
