using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

/// <summary>
/// Reads Firebird database metadata required for script generation.
/// </summary>
public sealed class FirebirdMetadataReader
{
    private readonly FbConnection _connection;

    public FirebirdMetadataReader(FbConnection connection)
    {
        _connection = connection;
    }

    public DbMetadata ReadAll()
        => new(
            Domains: ReadDomains(),
            Tables: ReadTables(),
            Procedures: ReadProcedures()
        );

    // --------------------
    // Domains
    // --------------------
    private List<DbDomain> ReadDomains()
    {
        const string sql = @"
SELECT
  TRIM(f.RDB$FIELD_NAME) AS NAME,
  COALESCE(f.RDB$NULL_FLAG, 0) AS NULL_FLAG,
  f.RDB$FIELD_TYPE,
  COALESCE(f.RDB$FIELD_SUB_TYPE, 0) AS FIELD_SUB_TYPE,
  f.RDB$FIELD_LENGTH,
  f.RDB$FIELD_PRECISION,
  f.RDB$FIELD_SCALE,
  f.RDB$CHARACTER_LENGTH
FROM RDB$FIELDS f
WHERE COALESCE(f.RDB$SYSTEM_FLAG, 0) = 0
  AND f.RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
ORDER BY TRIM(f.RDB$FIELD_NAME)";

        using var cmd = new FbCommand(sql, _connection);
        using var r = cmd.ExecuteReader();

        var domains = new List<DbDomain>();
        while (r.Read())
        {
            var name = r.GetString(0);
            var notNull = r.GetInt16(1) == 1;

            var fieldType = r.GetInt16(2);
            var subType = r.GetInt16(3);
            var len = r.GetInt16(4);
            var precision = r.IsDBNull(5) ? (short?)null : r.GetInt16(5);
            var scale = r.IsDBNull(6) ? (short?)null : r.GetInt16(6);
            var charLen = r.IsDBNull(7) ? (short?)null : r.GetInt16(7);

            var typeSql = FirebirdTypeMapper.FieldTypeToSql(fieldType, subType, len, precision, scale, charLen);
            domains.Add(new DbDomain(name, typeSql, notNull));
        }

        return domains;
    }

    // --------------------
    // Tables + columns + PK
    // --------------------
    private List<DbTable> ReadTables()
    {
        var tables = GetUserTableNames();
        var result = new List<DbTable>();

        foreach (var table in tables)
        {
            var columns = ReadTableColumns(table);
            var pk = ReadPrimaryKey(table);
            result.Add(new DbTable(table, columns, pk));
        }

        return result;
    }

    private List<string> GetUserTableNames()
    {
        const string sql = @"
SELECT TRIM(r.RDB$RELATION_NAME) AS NAME
FROM RDB$RELATIONS r
WHERE COALESCE(r.RDB$SYSTEM_FLAG, 0) = 0
  AND r.RDB$VIEW_BLR IS NULL
ORDER BY TRIM(r.RDB$RELATION_NAME)";

        using var cmd = new FbCommand(sql, _connection);
        using var r = cmd.ExecuteReader();

        var result = new List<string>();
        while (r.Read())
            result.Add(r.GetString(0));

        return result;
    }

    private List<DbColumn> ReadTableColumns(string table)
    {
        const string sql = @"
SELECT
  TRIM(rf.RDB$FIELD_NAME) AS COL_NAME,
  TRIM(rf.RDB$FIELD_SOURCE) AS FIELD_SOURCE,
  COALESCE(rf.RDB$NULL_FLAG, 0) AS NULL_FLAG,

  f.RDB$FIELD_TYPE,
  COALESCE(f.RDB$FIELD_SUB_TYPE, 0) AS FIELD_SUB_TYPE,
  f.RDB$FIELD_LENGTH,
  f.RDB$FIELD_PRECISION,
  f.RDB$FIELD_SCALE,
  f.RDB$CHARACTER_LENGTH
FROM RDB$RELATION_FIELDS rf
JOIN RDB$FIELDS f ON f.RDB$FIELD_NAME = rf.RDB$FIELD_SOURCE
WHERE rf.RDB$RELATION_NAME = @REL
ORDER BY rf.RDB$FIELD_POSITION";

        using var cmd = new FbCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@REL", table);

        using var r = cmd.ExecuteReader();
        var cols = new List<DbColumn>();

        while (r.Read())
        {
            var colName = r.GetString(0);
            var fieldSource = r.GetString(1);
            var notNull = r.GetInt16(2) == 1;

            var fieldType = r.GetInt16(3);
            var subType = r.GetInt16(4);
            var len = r.GetInt16(5);
            var precision = r.IsDBNull(6) ? (short?)null : r.GetInt16(6);
            var scale = r.IsDBNull(7) ? (short?)null : r.GetInt16(7);
            var charLen = r.IsDBNull(8) ? (short?)null : r.GetInt16(8);

            var typeSql = fieldSource.StartsWith("RDB$", StringComparison.OrdinalIgnoreCase)
                ? FirebirdTypeMapper.FieldTypeToSql(fieldType, subType, len, precision, scale, charLen)
                : fieldSource;

            cols.Add(new DbColumn(colName, typeSql, notNull));
        }

        return cols;
    }

    private DbPrimaryKey? ReadPrimaryKey(string table)
    {
        const string sql = @"
SELECT
  TRIM(rc.RDB$CONSTRAINT_NAME) AS CONSTRAINT_NAME,
  TRIM(isg.RDB$FIELD_NAME) AS FIELD_NAME
FROM RDB$RELATION_CONSTRAINTS rc
JOIN RDB$INDICES i ON i.RDB$INDEX_NAME = rc.RDB$INDEX_NAME
JOIN RDB$INDEX_SEGMENTS isg ON isg.RDB$INDEX_NAME = i.RDB$INDEX_NAME
WHERE rc.RDB$RELATION_NAME = @REL
  AND rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY isg.RDB$FIELD_POSITION";

        using var cmd = new FbCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@REL", table);

        using var r = cmd.ExecuteReader();

        string? constraint = null;
        var cols = new List<string>();

        while (r.Read())
        {
            constraint ??= r.GetString(0);
            cols.Add(r.GetString(1));
        }

        return constraint is null ? null : new DbPrimaryKey(constraint, cols);
    }

    // --------------------
    // Procedures
    // --------------------
    private List<DbProcedure> ReadProcedures()
    {
        const string sql = @"
SELECT TRIM(p.RDB$PROCEDURE_NAME) AS NAME,
       p.RDB$PROCEDURE_SOURCE AS SRC
FROM RDB$PROCEDURES p
WHERE COALESCE(p.RDB$SYSTEM_FLAG, 0) = 0
ORDER BY TRIM(p.RDB$PROCEDURE_NAME)";

        using var cmd = new FbCommand(sql, _connection);
        using var r = cmd.ExecuteReader();

        var procs = new List<DbProcedure>();
        while (r.Read())
        {
            var name = r.GetString(0);
            var src = (r.IsDBNull(1) ? "" : r.GetString(1)).TrimEnd();

            var (ins, outs) = ReadProcedureParameters(name);
            procs.Add(new DbProcedure(name, ins, outs, string.IsNullOrWhiteSpace(src) ? "BEGIN\nEND" : src));
        }

        return procs;
    }

    private (List<DbProcParam> Inputs, List<DbProcParam> Outputs) ReadProcedureParameters(string procName)
    {
        const string sql = @"
SELECT
  TRIM(pp.RDB$PARAMETER_NAME) AS P_NAME,
  pp.RDB$PARAMETER_TYPE AS P_TYPE, -- 0=in, 1=out
  f.RDB$FIELD_TYPE,
  COALESCE(f.RDB$FIELD_SUB_TYPE, 0) AS FIELD_SUB_TYPE,
  f.RDB$FIELD_LENGTH,
  f.RDB$FIELD_PRECISION,
  f.RDB$FIELD_SCALE,
  f.RDB$CHARACTER_LENGTH
FROM RDB$PROCEDURE_PARAMETERS pp
JOIN RDB$FIELDS f ON f.RDB$FIELD_NAME = pp.RDB$FIELD_SOURCE
WHERE pp.RDB$PROCEDURE_NAME = @P
ORDER BY pp.RDB$PARAMETER_TYPE, pp.RDB$PARAMETER_NUMBER";

        using var cmd = new FbCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@P", procName);

        using var r = cmd.ExecuteReader();

        var ins = new List<DbProcParam>();
        var outs = new List<DbProcParam>();

        while (r.Read())
        {
            var name = r.GetString(0);
            var pType = r.GetInt16(1);

            var typeSql = FirebirdTypeMapper.FieldTypeToSql(
                fieldType: r.GetInt16(2),
                fieldSubType: r.GetInt16(3),
                fieldLength: r.GetInt16(4),
                precision: r.IsDBNull(5) ? (short?)null : r.GetInt16(5),
                scale: r.IsDBNull(6) ? (short?)null : r.GetInt16(6),
                charLength: r.IsDBNull(7) ? (short?)null : r.GetInt16(7)
            );

            if (pType == 0) ins.Add(new DbProcParam(name, typeSql));
            else outs.Add(new DbProcParam(name, typeSql));
        }

        return (ins, outs);
    }
}
