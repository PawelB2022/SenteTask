using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

/// <summary>
/// Creates an empty Firebird database using the ADO.NET provider.
/// </summary>
public static class FirebirdDatabaseCreator
{
    /// <summary>
    /// Creates a database file defined by the provided connection string.
    /// </summary>
    public static void CreateEmptyDatabase(string connectionString, int pageSize, bool forcedWrites, bool overwrite)
{
    FbConnection.CreateDatabase(
        connectionString,
        pageSize,
        forcedWrites,
        overwrite);
}
}
