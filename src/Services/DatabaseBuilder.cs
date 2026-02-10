using DbMetaTool.Firebird;
using DbMetaTool.Scripting;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services;

/// <summary>
/// Orchestrates the database build process: create database and execute scripts.
/// </summary>
public sealed class DatabaseBuilder
{
    /// <summary>
    /// Creates a new database and executes all scripts from the provided directory.
    /// </summary>
    public BuildReport Build(string databaseDirectory, string scriptsDirectory, BuildOptions options)
    {
        if (string.IsNullOrWhiteSpace(databaseDirectory))
            throw new ArgumentException("Database directory is empty.", nameof(databaseDirectory));

        Directory.CreateDirectory(databaseDirectory);

        var dbPath = Path.Combine(databaseDirectory, options.DatabaseFileName);
        var report = new BuildReport(dbPath);

        try
        {
            var connStr = FirebirdConnectionFactory.BuildConnectionString(
                databasePath: dbPath,
                user: options.User,
                password: options.Password,
                host: options.Host,
                port: options.Port,
                charset: options.Charset,
                dialect: options.Dialect,
                pooling: options.Pooling);

            FirebirdDatabaseCreator.CreateEmptyDatabase(
            connStr,
            pageSize: options.PageSize,
            forcedWrites: options.ForcedWrites,
            overwrite: options.OverwriteDatabase);

            report.DatabaseCreated = true;

            var files = ScriptLoader.GetSqlFiles(scriptsDirectory);
            report.FilesTotal = files.Count;

            if (files.Count == 0)
                return report;

            using var conn = FirebirdConnectionFactory.CreateConnection(connStr);
            conn.Open();

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);

                string text;
                try
                {
                    text = ScriptLoader.ReadScriptText(filePath);
                }
                catch (Exception ex)
                {
                    report.Errors.Add(new BuildError(fileName, "<read file>", ex.Message));
                    report.FilesFailed++;
                    continue;
                }

                var statements = FirebirdSqlScriptSplitter.SplitStatements(text);
                FirebirdScriptExecutor.ExecuteFile(conn, fileName, statements, report);
            }
        }
        catch (Exception ex)
        {
            report.Errors.Add(new BuildError("<build-db>", "<create/open db>", ex.Message));
        }

        return report;
    }

    public static void PrintReport(BuildReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== BUILD-DB REPORT ===");
        Console.WriteLine($"DB Path: {report.DatabasePath}");
        Console.WriteLine($"DB Created: {report.DatabaseCreated}");
        Console.WriteLine($"Files: total={report.FilesTotal}, ok={report.FilesSucceeded}, failed={report.FilesFailed}");
        Console.WriteLine($"Statements executed: {report.StatementsExecuted}");

        if (report.Errors.Count == 0)
        {
            Console.WriteLine("Errors: none");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Errors: {report.Errors.Count}");
        foreach (var e in report.Errors)
        {
            Console.WriteLine($"- File: {e.FileName}");
            Console.WriteLine($"  Statement: {e.StatementPreview}");
            Console.WriteLine($"  Message: {e.Message}");
        }
    }
}

/// <summary>
/// Options controlling the database build process.
/// </summary>
public sealed record BuildOptions(
    string DatabaseFileName,
    bool OverwriteDatabase,
    int PageSize,
    bool ForcedWrites,
    string Host,
    int Port,
    string User,
    string Password,
    string Charset,
    int Dialect,
    bool Pooling);

