using Npgsql;

internal static class ExistingDatabaseConnection
{
    public static string Create(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Set POSTGRES_USER and POSTGRES_PASSWORD to the existing database credentials.");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = "postgres-vevsoulyfzw2y.postgres.database.azure.com",
            Database = "db",
            Username = username,
            Password = password,
            SslMode = SslMode.VerifyFull
        }.ConnectionString;
    }
}
