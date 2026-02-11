using DbMetaTool.Firebird;
using DbMetaTool.Scripting;

namespace DbMetaTool.Services;

/// <summary>
/// Coordinates metadata export: read metadata from the database and write it to files.
/// </summary>
public sealed class ScriptExporter
{
    /// <summary>
    /// Exports domains, tables (with columns) and stored procedures into output files.
/// </summary>
    public void Export(string connectionString, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory is empty.", nameof(outputDirectory));

        Directory.CreateDirectory(outputDirectory);

        using var conn = FirebirdConnectionFactory.CreateConnection(connectionString);
        conn.Open();

        var reader = new FirebirdMetadataReader(conn);
        var metadata = reader.ReadAll();

        var writer = new SqlMetadataWriter();
        writer.WriteAll(metadata, outputDirectory);
    }
}
