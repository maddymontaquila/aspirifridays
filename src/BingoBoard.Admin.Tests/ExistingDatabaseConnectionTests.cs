using Npgsql;

namespace BingoBoard.Admin.Tests;

public sealed class ExistingDatabaseConnectionTests
{
    [Theory]
    [InlineData("existing-user", "ordinary-password")]
    [InlineData("user;name=\"quoted\"", " p;a'ss\"word\\=!\r\n ")]
    public void Create_PreservesCredentialsAndRequiresVerifiedTls(string username, string password)
    {
        var connection = new NpgsqlConnectionStringBuilder(
            ExistingDatabaseConnection.Create(username, password));

        Assert.Equal(username, connection.Username);
        Assert.Equal(password, connection.Password);
        Assert.Equal("postgres-vevsoulyfzw2y.postgres.database.azure.com", connection.Host);
        Assert.Equal("db", connection.Database);
        Assert.Equal(SslMode.VerifyFull, connection.SslMode);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData(" ", "password")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    public void Create_RejectsMissingCredentials(string? username, string? password)
    {
        Assert.Throws<InvalidOperationException>(
            () => ExistingDatabaseConnection.Create(username, password));
    }
}
