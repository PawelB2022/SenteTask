using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

/// <summary>
/// Creates Firebird connection strings and connections.
/// </summary>
public static class FirebirdConnectionFactory
{
    /// <summary>
    /// Builds a connection string for a database file hosted by a Firebird server.
    /// </summary>
    public static string BuildConnectionString(
        string databasePath,
        string user,
        string password,
        string host = "localhost",
        int port = 3050,
        string charset = "UTF8",
        int dialect = 3,
        bool pooling = false)
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource = host,
            Port = port,
            Database = databasePath,
            UserID = user,
            Password = password,
            Charset = charset,
            Dialect = dialect,
            Pooling = pooling
        };

        return csb.ToString();
    }

    /// <summary>
    /// Creates a new connection instance.
    /// </summary>
    public static FbConnection CreateConnection(string connectionString) => new(connectionString);
}
