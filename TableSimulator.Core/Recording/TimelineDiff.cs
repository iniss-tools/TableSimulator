namespace TableSimulator.Core.Recording;

/// <summary>Druh kroku porovnania.</summary>
public enum DiffKind
{
    /// <summary>Rovnaky text v oboch zaznamoch.</summary>
    Same,

    /// <summary>Na tom istom mieste postupnosti iny text.</summary>
    Changed,

    /// <summary>Text len v prvom zazname.</summary>
    Removed,

    /// <summary>Text len v druhom zazname.</summary>
    Added
}

/// <summary>Krok porovnania dvoch priebehov tabule.</summary>
/// <param name="Kind">druh</param>
/// <param name="A">stav v prvom zazname; null pri pridanom</param>
/// <param name="B">stav v druhom zazname; null pri odobranom</param>
public sealed record DiffStep(DiffKind Kind, BoardState? A, BoardState? B);

/// <summary>
/// Porovnanie postupnosti stavov tabule dvoch zaznamov ako <c>diff</c> (Myers): na case nezalezi, len na poradi, takze
/// zaznamy mozu zacat inokedy. Susedne odobrane a pridane stavy sa sparuju ako zmenene.
/// </summary>
public static class TimelineDiff
{
    /// <summary>Najviac rozdielov, pre ktore sa hlada najkratsi rozdiel; pri viacerych sa zvysok paruje po poradi.</summary>
    public const int MaxDifferences = 2000;

    private enum Op
    {
        Equal,
        Delete,
        Insert
    }

    public static IReadOnlyList<DiffStep> Compare(IReadOnlyList<BoardState> a, IReadOnlyList<BoardState> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        var x = a.Select(s => Id(ids, s.Key)).ToArray();
        var y = b.Select(s => Id(ids, s.Key)).ToArray();

        // spolocny zaciatok a koniec netreba hladat
        var prefix = 0;
        while (prefix < x.Length && prefix < y.Length && x[prefix] == y[prefix])
            prefix++;
        var suffix = 0;
        while (suffix < x.Length - prefix && suffix < y.Length - prefix && x[x.Length - 1 - suffix] == y[y.Length - 1 - suffix])
            suffix++;

        var steps = new List<DiffStep>();
        for (var i = 0; i < prefix; i++)
            steps.Add(new DiffStep(DiffKind.Same, a[i], b[i]));

        var middleA = x[prefix..(x.Length - suffix)];
        var middleB = y[prefix..(y.Length - suffix)];
        var ops = Myers(middleA, middleB)
                  ?? [.. Enumerable.Range(0, middleA.Length).Select(i => (Op.Delete, i, -1)), .. Enumerable.Range(0, middleB.Length).Select(j => (Op.Insert, -1, j))];
        var removed = new List<BoardState>();
        var added = new List<BoardState>();
        foreach (var (op, i, j) in ops)
        {
            switch (op)
            {
                case Op.Delete:
                    removed.Add(a[prefix + i]);
                    break;
                case Op.Insert:
                    added.Add(b[prefix + j]);
                    break;
                default:
                    Flush(steps, removed, added);
                    steps.Add(new DiffStep(DiffKind.Same, a[prefix + i], b[prefix + j]));
                    break;
            }
        }

        Flush(steps, removed, added);
        for (var i = 0; i < suffix; i++)
            steps.Add(new DiffStep(DiffKind.Same, a[x.Length - suffix + i], b[y.Length - suffix + i]));
        return steps;
    }

    private static int Id(Dictionary<string, int> ids, string key)
    {
        if (!ids.TryGetValue(key, out var id))
            ids[key] = id = ids.Count;
        return id;
    }

    /// <summary>Odobrane a pridane stavy medzi dvoma rovnakymi: najprv dvojice zmenenych, potom zvysok.</summary>
    private static void Flush(List<DiffStep> steps, List<BoardState> removed, List<BoardState> added)
    {
        var pairs = Math.Min(removed.Count, added.Count);
        for (var i = 0; i < pairs; i++)
            steps.Add(new DiffStep(DiffKind.Changed, removed[i], added[i]));
        steps.AddRange(removed.Skip(pairs).Select(s => new DiffStep(DiffKind.Removed, s, null)));
        steps.AddRange(added.Skip(pairs).Select(s => new DiffStep(DiffKind.Added, null, s)));
        removed.Clear();
        added.Clear();
    }

    /// <summary>Najkratsia postupnost uprav (Myers); null, ak je rozdielov viac nez <see cref="MaxDifferences" />.</summary>
    private static List<(Op Op, int A, int B)>? Myers(int[] a, int[] b)
    {
        int n = a.Length, m = b.Length, max = n + m;
        var offset = max + 1;
        var v = new int[2 * max + 3];
        // pre kazdy krok stav v pred nim (k od -d-1 po d+1) na spatne prejdenie
        var trace = new List<int[]>();
        for (var d = 0; d <= max; d++)
        {
            if (d > MaxDifferences)
                return null;
            var snapshot = new int[2 * d + 3];
            Array.Copy(v, offset - d - 1, snapshot, 0, snapshot.Length);
            trace.Add(snapshot);
            for (var k = -d; k <= d; k += 2)
            {
                var x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? v[offset + k + 1] : v[offset + k - 1] + 1;
                var y = x - k;
                while (x < n && y < m && a[x] == b[y])
                {
                    x++;
                    y++;
                }

                v[offset + k] = x;
                if (x >= n && y >= m)
                    return Backtrack(trace, n, m);
            }
        }

        return Backtrack(trace, n, m);
    }

    private static List<(Op Op, int A, int B)> Backtrack(List<int[]> trace, int n, int m)
    {
        var ops = new List<(Op Op, int A, int B)>();
        int x = n, y = m;
        for (var d = trace.Count - 1; d >= 0; d--)
        {
            var v = trace[d];
            var k = x - y;
            var prevK = k == -d || (k != d && v[k - 1 + d + 1] < v[k + 1 + d + 1]) ? k + 1 : k - 1;
            var prevX = v[prevK + d + 1];
            var prevY = prevX - prevK;
            while (x > prevX && y > prevY)
            {
                ops.Add((Op.Equal, x - 1, y - 1));
                x--;
                y--;
            }

            if (d > 0)
                ops.Add(x == prevX ? (Op.Insert, -1, y - 1) : (Op.Delete, x - 1, -1));
            x = prevX;
            y = prevY;
        }

        ops.Reverse();
        return ops;
    }
}
