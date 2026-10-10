using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TableSimulator.Persistence.Postgres;

namespace TableSimulator.Persistence;

/// <summary>
/// Prenos vsetkych udajov medzi databazami simulatora s rovnakym modelom (obe na poslednej migracii) - zaloha
/// z PostgreSQL do suboru SQLite a obnova zo suboru SQLite do PostgreSQL. Cielove tabulky sa najprv vyprazdnia, riadky
/// sa prenasaju po davkach v poradi cudzich klucov s povodnymi klucmi; v PostgreSQL sa potom posunu cislovacie
/// postupnosti za najvacsi kluc. Vsetko v jednej transakcii cielovej databazy.
/// </summary>
public static class DatabaseCopier
{
    private const int Batch = 2000;

    private static readonly MethodInfo CopyMethod = typeof(DatabaseCopier).GetMethod(nameof(CopyTableAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Prenesie vsetky udaje z <paramref name="source" /> do <paramref name="target" />; vrati pocet riadkov.</summary>
    public static async Task<int> CopyAsync(AppDbContext source, AppDbContext target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        var types = Ordered(target.Model);
        var total = 0;
        target.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var transaction = await target.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        // mazanie od zavislych tabuliek
        foreach (var type in types.AsEnumerable().Reverse())
        {
            // meno tabulky je z modelu - parametrom byt nemoze
            var delete = "DELETE FROM " + Quote(type.GetTableName()!);
            await target.Database.ExecuteSqlRawAsync(delete, cancellationToken).ConfigureAwait(false);
        }

        foreach (var type in types)
        {
            var copy = (Task<int>)CopyMethod.MakeGenericMethod(type.ClrType).Invoke(null, [source, target, cancellationToken])!;
            total += await copy.ConfigureAwait(false);
        }

        if (target is PostgresAppDbContext)
            await ResetSequencesAsync(target, types, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return total;
    }

    private static async Task<int> CopyTableAsync<T>(AppDbContext source, AppDbContext target, CancellationToken cancellationToken) where T : class
    {
        var count = 0;
        var pending = 0;
        await foreach (var row in source.Set<T>().AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            target.Set<T>().Add(row);
            count++;
            if (++pending < Batch)
                continue;
            await target.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            target.ChangeTracker.Clear();
            pending = 0;
        }

        await target.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        target.ChangeTracker.Clear();
        return count;
    }

    /// <summary>Entity v poradi, v akom sa daju vkladat - najprv tie, na ktore ine odkazuju.</summary>
    private static List<IEntityType> Ordered(IModel model)
    {
        var types = model.GetEntityTypes().Where(t => t.GetTableName() is not null && !t.IsOwned()).ToList();
        var ordered = new List<IEntityType>();
        var done = new HashSet<IEntityType>();
        void Visit(IEntityType type, HashSet<IEntityType> path)
        {
            if (done.Contains(type) || !path.Add(type))
                return;
            foreach (var principal in type.GetForeignKeys().Select(f => f.PrincipalEntityType).Where(p => p != type && types.Contains(p)))
                Visit(principal, path);
            done.Add(type);
            ordered.Add(type);
        }

        foreach (var type in types.OrderBy(t => t.GetTableName(), StringComparer.Ordinal))
            Visit(type, []);
        return ordered;
    }

    /// <summary>Postupnosti identity v PostgreSQL za najvacsi preneseny kluc (inak by dalsi zapis narazil na existujuci).</summary>
    private static async Task ResetSequencesAsync(AppDbContext target, List<IEntityType> types, CancellationToken cancellationToken)
    {
        foreach (var type in types)
        {
            var table = Quote(type.GetTableName()!);
            var keys = type.FindPrimaryKey()?.Properties
                .Where(p => p.ValueGenerated == ValueGenerated.OnAdd && (p.ClrType == typeof(int) || p.ClrType == typeof(long))) ?? [];
            foreach (var key in keys)
            {
                var column = Quote(key.GetColumnName());
                // mena su z modelu - parametrom byt nemozu
                var sql = "SELECT setval(pg_get_serial_sequence('" + table.Replace("'", "''", StringComparison.Ordinal) + "', '"
                          + key.GetColumnName().Replace("'", "''", StringComparison.Ordinal) + "'), COALESCE(MAX(" + column + "), 1), MAX(" + column
                          + ") IS NOT NULL) FROM " + table;
                await target.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Meno tabulky alebo stlpca v uvodzovkach (mena su z modelu, nie od pouzivatela).</summary>
    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
