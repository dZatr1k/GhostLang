namespace GhostLang.Core.Benchmark;

public static class Metrics
{
    public static int LevenshteinDistance(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    public static int LevenshteinDistance<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        comparer ??= EqualityComparer<T>.Default;

        if (a.Count == 0) return b.Count;
        if (b.Count == 0) return a.Count;

        var previous = new int[b.Count + 1];
        var current = new int[b.Count + 1];

        for (var j = 0; j <= b.Count; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Count; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Count; j++)
            {
                var cost = comparer.Equals(a[i - 1], b[j - 1]) ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Count];
    }

    public static double CharacterErrorRate(string predicted, string reference)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(reference);

        if (reference.Length == 0) return predicted.Length == 0 ? 0.0 : 1.0;

        var distance = LevenshteinDistance(predicted, reference);
        return (double)distance / reference.Length;
    }

    public static double WordErrorRate(string predicted, string reference)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(reference);

        var predTokens = Tokenize(predicted);
        var refTokens = Tokenize(reference);

        if (refTokens.Count == 0) return predTokens.Count == 0 ? 0.0 : 1.0;

        var distance = LevenshteinDistance(predTokens, refTokens, StringComparer.Ordinal);
        return (double)distance / refTokens.Count;
    }

    private static List<string> Tokenize(string s) =>
        s.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

    public static double BoundingBoxIoU(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
    {
        if (aw <= 0 || ah <= 0 || bw <= 0 || bh <= 0) return 0.0;

        var ix1 = Math.Max(ax, bx);
        var iy1 = Math.Max(ay, by);
        var ix2 = Math.Min(ax + aw, bx + bw);
        var iy2 = Math.Min(ay + ah, by + bh);

        var iw = Math.Max(0, ix2 - ix1);
        var ih = Math.Max(0, iy2 - iy1);

        var intersection = (double)(iw * ih);
        var union = (double)(aw * ah) + (bw * bh) - intersection;

        return union <= 0 ? 0.0 : intersection / union;
    }

    public static double Bleu4(string predicted, string reference)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(reference);

        var predTokens = Tokenize(predicted);
        var refTokens = Tokenize(reference);

        if (refTokens.Count == 0 || predTokens.Count == 0) return 0.0;

        var logSum = 0.0;
        var validNgrams = 0;

        for (var n = 1; n <= 4; n++)
        {
            var (matches, total) = CountNgramMatches(predTokens, refTokens, n);
            if (total == 0) continue;

            var precision = matches == 0
                ? 1.0 / (2.0 * total)
                : (double)matches / total;

            logSum += Math.Log(precision);
            validNgrams++;
        }

        if (validNgrams == 0) return 0.0;

        var geoMean = Math.Exp(logSum / validNgrams);
        var brevity = predTokens.Count >= refTokens.Count
            ? 1.0
            : Math.Exp(1.0 - (double)refTokens.Count / predTokens.Count);

        return brevity * geoMean;
    }

    private static (int matches, int total) CountNgramMatches(IReadOnlyList<string> pred, IReadOnlyList<string> reference, int n)
    {
        if (pred.Count < n) return (0, 0);

        var refCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i <= reference.Count - n; i++)
        {
            var ngram = string.Join(' ', reference.Skip(i).Take(n));
            refCounts[ngram] = refCounts.TryGetValue(ngram, out var c) ? c + 1 : 1;
        }

        var matches = 0;
        var total = 0;

        for (var i = 0; i <= pred.Count - n; i++)
        {
            var ngram = string.Join(' ', pred.Skip(i).Take(n));
            total++;
            if (refCounts.TryGetValue(ngram, out var c) && c > 0)
            {
                matches++;
                refCounts[ngram] = c - 1;
            }
        }

        return (matches, total);
    }

    public static double ChrF(string predicted, string reference, int n = 6, double beta = 2.0)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(reference);

        if (predicted.Length == 0 && reference.Length == 0) return 1.0;
        if (predicted.Length == 0 || reference.Length == 0) return 0.0;

        var totalPrecision = 0.0;
        var totalRecall = 0.0;
        var validOrders = 0;

        for (var order = 1; order <= n; order++)
        {
            var (p, r) = ChrFOrder(predicted, reference, order);
            if (double.IsNaN(p) || double.IsNaN(r)) continue;
            totalPrecision += p;
            totalRecall += r;
            validOrders++;
        }

        if (validOrders == 0) return 0.0;

        var avgP = totalPrecision / validOrders;
        var avgR = totalRecall / validOrders;

        if (avgP + avgR == 0) return 0.0;

        var beta2 = beta * beta;
        return (1 + beta2) * avgP * avgR / (beta2 * avgP + avgR);
    }

    private static (double precision, double recall) ChrFOrder(string pred, string reference, int n)
    {
        if (pred.Length < n || reference.Length < n) return (double.NaN, double.NaN);

        var predCounts = CountCharNgrams(pred, n);
        var refCounts = CountCharNgrams(reference, n);

        var intersection = 0;
        foreach (var kv in predCounts)
        {
            if (refCounts.TryGetValue(kv.Key, out var rc))
                intersection += Math.Min(kv.Value, rc);
        }

        var predTotal = predCounts.Values.Sum();
        var refTotal = refCounts.Values.Sum();

        var precision = predTotal > 0 ? (double)intersection / predTotal : 0.0;
        var recall = refTotal > 0 ? (double)intersection / refTotal : 0.0;

        return (precision, recall);
    }

    private static Dictionary<string, int> CountCharNgrams(string s, int n)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i <= s.Length - n; i++)
        {
            var ngram = s.Substring(i, n);
            counts[ngram] = counts.TryGetValue(ngram, out var c) ? c + 1 : 1;
        }
        return counts;
    }
}
