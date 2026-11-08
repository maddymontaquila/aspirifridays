#!/usr/bin/env dotnet
#:package Npgsql.EntityFrameworkCore.PostgreSQL
#:include ./ExistingDatabaseConnection.cs

using System.Diagnostics;

string connection;
try
{
    connection = ExistingDatabaseConnection.Create(
        Environment.GetEnvironmentVariable("POSTGRES_USER"),
        Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"));
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine($"::error::{exception.Message}");
    return 1;
}

var start = new ProcessStartInfo("aspire");
foreach (var argument in new[]
{
    "deploy", "--apphost", "./src/apphost.cs", "--environment", "Production", "--non-interactive"
})
{
    start.ArgumentList.Add(argument);
}
// Pass credentials only through the child's environment, never CLI arguments or logs.
start.Environment.Remove("POSTGRES_USER");
start.Environment.Remove("POSTGRES_PASSWORD");
start.Environment["ConnectionStrings__db"] = connection;
using var process = Process.Start(start)
    ?? throw new InvalidOperationException("Unable to start Aspire deployment.");
await process.WaitForExitAsync();
return process.ExitCode;
