using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Services;

namespace DbMetaTool.Firebird;

/// <summary>
/// Executes SQL statements against a Firebird database with per-file transactions.
/// </summary>
public static class FirebirdScriptExecutor
{
    /// <summary>
    /// Executes statements from a single script file within one transaction.
    /// </summary>
    public static void ExecuteFile(
        FbConnection connection,
        string fileName,
        IReadOnlyList<string> statements,
        BuildReport report)
    {
        using var tx = connection.BeginTransaction();
        var ok = true;

        foreach (var stmt in statements)
        {
            var trimmed = stmt.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            try
            {
                using var cmd = new FbCommand(trimmed, connection, tx);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();

                report.StatementsExecuted++;
            }
            catch (Exception ex)
            {
                ok = false;
                report.Errors.Add(new BuildError(fileName, Shorten(trimmed, 200), ex.Message));
                break;
            }
        }

        if (ok)
        {
            tx.Commit();
            report.FilesSucceeded++;
        }
        else
        {
            tx.Rollback();
            report.FilesFailed++;
        }
    }

    private static string Shorten(string s, int maxLen)
    {
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen) + "...";
    }
}
