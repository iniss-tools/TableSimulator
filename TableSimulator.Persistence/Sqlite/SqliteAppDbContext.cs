using Microsoft.EntityFrameworkCore;

namespace TableSimulator.Persistence.Sqlite;

/// <summary>Databaza simulatora v SQLite (subor v priecinku nastaveni) - migracie v <c>Migrations/Sqlite</c>.</summary>
public sealed class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options);
