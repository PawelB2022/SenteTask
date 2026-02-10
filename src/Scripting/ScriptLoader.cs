namespace DbMetaTool.Scripting;

/// <summary>
/// Loads script files from a directory and provides a deterministic ordering.
/// </summary>
public static class ScriptLoader
{
    /// <summary>
    /// Returns paths to .sql files in a directory, ordered by file name.
    /// </summary>
    public static IReadOnlyList<string> GetSqlFiles(string scriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(scriptsDirectory))
            throw new ArgumentException("Scripts directory is empty.", nameof(scriptsDirectory));

        if (!Directory.Exists(scriptsDirectory))
            throw new DirectoryNotFoundException($"Scripts directory not found: {scriptsDirectory}");

        return Directory.EnumerateFiles(scriptsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads the full contents of a script file.
    /// </summary>
    public static string ReadScriptText(string filePath)
    {
        // Using UTF-8 is a common default for SQL scripts.
        return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
    }
}
