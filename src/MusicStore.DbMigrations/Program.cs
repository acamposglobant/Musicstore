using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Connection string injected by Aspire (resource name: musicstore-db-database).
var connectionString =
    builder.Configuration.GetConnectionString("musicstore-db-database")
    ?? throw new InvalidOperationException(
        "Connection string 'musicstore-db-database' was not provided. " +
        "Run via the Aspire AppHost (WithReference(database)).");

builder.AddServiceDefaults();

var upgradeEngine = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .JournalToPostgresqlTable("public", "schemaversions")
    .LogToConsole()
    .Build();

var result = upgradeEngine.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine("Database migration FAILED:");
    Console.Error.WriteLine(result.Error);
    return 1;
}

var executedScripts = result.Scripts.ToList();

if (executedScripts.Count == 0)
{
    Console.WriteLine("Database already up to date — no scripts executed.");
}
else
{
    foreach (var script in executedScripts)
    {
        Console.WriteLine($"Applied: {script.Name}");
    }
    Console.WriteLine($"Database migration succeeded ({executedScripts.Count} script(s) executed).");
}

return 0;
