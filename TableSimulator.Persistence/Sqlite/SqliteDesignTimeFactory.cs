using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TableSimulator.Persistence.Sqlite;

/// <summary>Kontext pre <c>dotnet ef migrations</c> (subor databazy sa pri navrhu nepouziva).</summary>
public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqliteAppDbContext>().UseSqlite("Data Source=design.db").Options);
}
