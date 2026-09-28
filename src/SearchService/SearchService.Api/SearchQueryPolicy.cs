public static class SearchQueryPolicy
{
    public static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query is required.", nameof(query));
        var value = query.Trim();
        if (value.Length > 4096) throw new ArgumentException("Query exceeds the maximum length.", nameof(query));
        return value;
    }
    public static int NormalizeTopK(int topK)
    {
        if (topK is < 0) throw new ArgumentOutOfRangeException(nameof(topK));
        return Math.Clamp(topK == 0 ? 10 : topK, 1, 50);
    }
}