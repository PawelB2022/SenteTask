using System;
using System.IO;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

public static class FirebirdSmokeTest
{
    /// <summary>
    /// Weryfikuje, że connection string działa: otwiera połączenie i pobiera wersję silnika.
    /// </summary>
    public static void VerifyConnection(string connectionString)
    {
        using var conn = new FbConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT RDB$GET_CONTEXT('SYSTEM','ENGINE_VERSION') FROM RDB$DATABASE";
        var version = cmd.ExecuteScalar()?.ToString()?.Trim();

        Console.WriteLine($"[OK] Połączenie działa. Firebird ENGINE_VERSION: {version}");
    }

    /// <summary>
    /// Tworzy pustą bazę danych jako plik .fdb i sprawdza, czy można się do niej podłączyć.
    /// UWAGA: działa z connection stringiem wskazującym plik bazy na serwerze (np. localhost:C:\...\db.fdb).
    /// </summary>
    public static void CreateEmptyDatabase(string connectionString, bool overwrite = false)
    {
        // W providerze istnieje API do tworzenia bazy: FbConnection.CreateDatabase(connStr, overwrite: ...)
        FbConnection.CreateDatabase(connectionString, 4096, true, overwrite);

        Console.WriteLine("[OK] Baza danych została utworzona (CreateDatabase).");
        VerifyConnection(connectionString);
    }

    /// <summary>
    /// Pomocniczo: buduje connection string do Firebirda pod lokalny serwer i plik .fdb.
    /// Hasło pobieramy z ENV (żeby nie hardkodować).
    /// </summary>
    public static string BuildLocalServerConnectionString(string databaseFilePath)
    {
        var password = Environment.GetEnvironmentVariable("FIREBIRD_SYSDBA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Brak zmiennej środowiskowej FIREBIRD_SYSDBA_PASSWORD. " +
                "Ustaw hasło dla SYSDBA przed użyciem build-db."
            );
        }

        var csb = new FbConnectionStringBuilder
        {
            // Serwer lokalny:
            DataSource = "localhost",
            Port = 3050,

            // Ścieżka do pliku bazy (na maszynie serwera):
            Database = databaseFilePath,

            UserID = "SYSDBA",
            Password = password,

            // Bezpieczny, częsty default:
            Charset = "UTF8",
            Dialect = 3,
            Pooling = false
        };

        return csb.ToString();
    }

    public static string EnsureDbFilePath(string databaseDirectory, string fileName = "database.fdb")
    {
        if (string.IsNullOrWhiteSpace(databaseDirectory))
            throw new ArgumentException("databaseDirectory jest puste.");

        Directory.CreateDirectory(databaseDirectory);
        return Path.Combine(databaseDirectory, fileName);
    }
}
