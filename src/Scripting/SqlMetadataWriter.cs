using System.Text;

namespace DbMetaTool.Scripting;

/// <summary>
/// Writes metadata to SQL files.
/// </summary>
public sealed class SqlMetadataWriter
{
    public void WriteAll(DbMetaTool.Firebird.DbMetadata metadata, string outputDirectory)
    {
        WriteDomains(metadata, outputDirectory);
        WriteTables(metadata, outputDirectory);
        WriteProcedures(metadata, outputDirectory);
    }

    private static void WriteDomains(DbMetaTool.Firebird.DbMetadata metadata, string outputDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Export: DOMAINS");
        sb.AppendLine();

        foreach (var d in metadata.Domains)
        {
            sb.Append("CREATE DOMAIN ").Append(d.Name).Append(' ').Append(d.TypeSql);
            if (d.NotNull) sb.Append(" NOT NULL");
            sb.AppendLine(";");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(outputDirectory, "01_domains.sql"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteTables(DbMetaTool.Firebird.DbMetadata metadata, string outputDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Export: TABLES");
        sb.AppendLine();

        foreach (var t in metadata.Tables)
        {
            sb.AppendLine($"CREATE TABLE {t.Name} (");

            for (int i = 0; i < t.Columns.Count; i++)
            {
                var c = t.Columns[i];
                sb.Append("    ").Append(c.Name).Append(' ').Append(c.TypeOrDomain);
                if (c.NotNull) sb.Append(" NOT NULL");

                sb.AppendLine(i == t.Columns.Count - 1 && t.PrimaryKey is null ? "" : ",");
            }

            if (t.PrimaryKey is not null)
            {
                sb.Append("    CONSTRAINT ").Append(t.PrimaryKey.ConstraintName)
                  .Append(" PRIMARY KEY (")
                  .Append(string.Join(", ", t.PrimaryKey.Columns))
                  .AppendLine(")");
            }

            sb.AppendLine(");");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(outputDirectory, "02_tables.sql"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteProcedures(DbMetaTool.Firebird.DbMetadata metadata, string outputDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Export: PROCEDURES");
        sb.AppendLine("SET TERM ^ ;");
        sb.AppendLine();

        foreach (var p in metadata.Procedures)
        {
            sb.Append("CREATE PROCEDURE ").Append(p.Name);

            if (p.Inputs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("(");
                for (int i = 0; i < p.Inputs.Count; i++)
                {
                    var prm = p.Inputs[i];
                    sb.Append("    ").Append(prm.Name).Append(' ').Append(prm.TypeSql);
                    sb.AppendLine(i == p.Inputs.Count - 1 ? "" : ",");
                }
                sb.Append(")");
            }

            if (p.Outputs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("RETURNS (");
                for (int i = 0; i < p.Outputs.Count; i++)
                {
                    var prm = p.Outputs[i];
                    sb.Append("    ").Append(prm.Name).Append(' ').Append(prm.TypeSql);
                    sb.AppendLine(i == p.Outputs.Count - 1 ? "" : ",");
                }
                sb.AppendLine(")");
            }

            sb.AppendLine("AS");
            sb.AppendLine(p.Body.TrimEnd());
            sb.AppendLine("^");
            sb.AppendLine();
        }

        sb.AppendLine("SET TERM ; ^");
        sb.AppendLine();

        File.WriteAllText(Path.Combine(outputDirectory, "03_procedures.sql"), sb.ToString(), Encoding.UTF8);
    }
}
