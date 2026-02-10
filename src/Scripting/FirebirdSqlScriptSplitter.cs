using System.Text;

namespace DbMetaTool.Scripting;

/// <summary>
/// Splits Firebird SQL scripts into executable statements, supporting SET TERM.
/// </summary>
public static class FirebirdSqlScriptSplitter
{
    /// <summary>
    /// Splits script text into statements based on the active terminator.
    /// </summary>
    public static IReadOnlyList<string> SplitStatements(string scriptText)
    {
        var result = new List<string>();
        var buffer = new StringBuilder();

        // Default statement terminator used by many tools.
        var terminator = ";";

        using var reader = new StringReader(scriptText);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            // SET TERM changes the statement terminator used by script runners.
            if (trimmed.StartsWith("SET TERM", StringComparison.OrdinalIgnoreCase))
            {
                FlushByTerminator(result, buffer, terminator);

                terminator = ParseNewTerminatorOrKeep(trimmed, terminator);

                // SET TERM is a script-runner directive, not SQL executed by the engine.
                continue;
            }

            buffer.AppendLine(line);

            // Attempt to flush statements whenever the terminator appears.
            FlushByTerminator(result, buffer, terminator);
        }

        if (buffer.Length > 0)
            result.Add(buffer.ToString());

        return result;
    }

    private static string ParseNewTerminatorOrKeep(string setTermLine, string current)
    {
        // Typical form: SET TERM ^ ;
        var parts = setTermLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 4 ? parts[2] : current;
    }

    private static void FlushByTerminator(List<string> output, StringBuilder buffer, string terminator)
    {
        while (true)
        {
            var text = buffer.ToString();
            var idx = text.IndexOf(terminator, StringComparison.Ordinal);
            if (idx < 0) break;

            var stmt = text.Substring(0, idx);
            output.Add(stmt);

            var rest = text.Substring(idx + terminator.Length);
            buffer.Clear();
            buffer.Append(rest);
        }
    }
}
