using DbMetaTool.Firebird;
using DbMetaTool.Scripting;

namespace DbMetaTool.Services;

/// <summary>
/// Orchestrates the database update process: connect and execute scripts.
/// </summary>
public sealed class DatabaseUpdater
{
    /// <summary>
    /// Executes all scripts from the provided directory against an existing database.
    /// </summary>
    public UpdateReport Update(string connectionString, string scriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));

        var report = new UpdateReport();

        try
        {
            var files = ScriptLoader.GetSqlFiles(scriptsDirectory);
            report.FilesTotal = files.Count;

            if (files.Count == 0)
                return report;

            using var conn = FirebirdConnectionFactory.CreateConnection(connectionString);

            try
            {
                conn.Open();
                report.ConnectionOpened = true;
            }
            catch (Exception ex)
            {
                report.ConnectionOpened = false;
                report.Errors.Add(new BuildError("<update-db>", "<open connection>", ex.Message));
                return report;
            }


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

                // Executes statements in a transaction per file to ensure atomicity.
                DbMetaTool.Firebird.FirebirdScriptExecutor.ExecuteFile(conn, fileName, statements, report);
            }
        }
        catch (Exception ex)
        {
            report.Errors.Add(new BuildError("<update-db>", "<connect/execute>", ex.Message));
        }

        return report;
    }

    public static void PrintReport(UpdateReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== UPDATE-DB REPORT ===");
        Console.WriteLine($"Status: {(report.Succeeded ? "SUCCESS" : "FAILED")}");
        Console.WriteLine($"Connection opened: {report.ConnectionOpened}");
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
