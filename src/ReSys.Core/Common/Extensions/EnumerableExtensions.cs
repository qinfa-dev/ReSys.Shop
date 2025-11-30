namespace ReSys.Core.Common.Extensions;

public static class EnumerableExtensions
{
    public static string JoinToSentence(this IEnumerable<string> items)
    {
        var list = items.ToList();

        if (!list.Any())
            return string.Empty;

        if (list.Count == 1)
            return list[index: 0];

        if (list.Count == 2)
            return $"{list[index: 0]} and {list[index: 1]}";

        return $"{string.Join(separator: ", ", values: list.Take(count: list.Count - 1))}, and {list[^1]}";
    }
}