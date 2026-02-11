namespace DbMetaTool.Firebird;

/// <summary>
/// Represents a subset of database metadata required for script generation.
/// </summary>
public sealed record DbMetadata(
    List<DbDomain> Domains,
    List<DbTable> Tables,
    List<DbProcedure> Procedures);

public sealed record DbDomain(string Name, string TypeSql, bool NotNull);

public sealed record DbTable(string Name, List<DbColumn> Columns, DbPrimaryKey? PrimaryKey);

public sealed record DbColumn(string Name, string TypeOrDomain, bool NotNull);

public sealed record DbPrimaryKey(string ConstraintName, List<string> Columns);

public sealed record DbProcedure(string Name, List<DbProcParam> Inputs, List<DbProcParam> Outputs, string Body);

public sealed record DbProcParam(string Name, string TypeSql);
