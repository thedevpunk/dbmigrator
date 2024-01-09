// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Data;
using DevPunk.DbMigrator.Core;
using DevPunk.DbMigrator.Core.MigrationRunners;


var directoryOption = new Option<DirectoryInfo>(
                aliases: ["--directory", "-d"],
                description: "The directory to run migrations from",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()));

// var upOption = new Option<bool>(
//                 name: "up",
//                 description: "Apply migrations",
//                 getDefaultValue: () => false);

// var downOption = new Option<bool>(
//                 name: "--down",
//                 description: "Revert migrations",
//                 getDefaultValue: () => false);

var toOption = new Option<string?>(
                aliases: ["--to", "-t"],
                description: "Specific script to migrate to (up or down)",
                getDefaultValue: () => null);

var engineOption = new Option<string>(
            aliases: ["--engine", "-e"],
            description: "The database engine to use (choose from 'SqlServer', 'Postgres'). Can be set during environment variable 'DBMIGRATOR_ENGINE'",
            getDefaultValue: () => Environment.GetEnvironmentVariable("DBMIGRATOR_ENGINE") ?? "SqlServer");

var connectionStringOption = new Option<string?>(
            aliases: ["--connstring", "-c"],
            description: "The connection string to the database. Can be set during environment variable 'DBMIGRATOR_CONNSTRING'",
            getDefaultValue: () => Environment.GetEnvironmentVariable("DBMIGRATOR_CONNSTRING") ?? "");

var upCommand = new Command("up", "Apply migrations");
upCommand.AddOption(toOption);
upCommand.AddOption(directoryOption);
upCommand.AddOption(engineOption);
upCommand.AddOption(connectionStringOption);
upCommand.SetHandler(RunUpMigrations, toOption, directoryOption, engineOption, connectionStringOption);

var downCommand = new Command("down", "Revert migrations");
downCommand.AddOption(toOption);
downCommand.AddOption(directoryOption);
downCommand.AddOption(engineOption);
downCommand.AddOption(connectionStringOption);
downCommand.SetHandler(RunDownMigrations, toOption, directoryOption, engineOption, connectionStringOption);

var rootCommand = new RootCommand("Database Migration CLI Tool");
rootCommand.AddCommand(upCommand);
rootCommand.AddCommand(downCommand);

// rootCommand.SetHandler(RunMigrations, directoryOption, upOption, downOption, toOption, engineOption, connectionStringOption);

return rootCommand.InvokeAsync(args).Result;

static void RunUpMigrations(string? to, DirectoryInfo directory, string engine, string? connectionString)
{
    RunMigrations(true, to, directory, engine, connectionString);
}

static void RunDownMigrations(string? to, DirectoryInfo directory, string engine, string? connectionString)
{
    RunMigrations(false, to, directory, engine, connectionString);
}

static void RunMigrations(bool isUpMigration, string? to, DirectoryInfo directory, string engine, string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new ArgumentException("Connection string is required");
    }

    switch (engine)
    {
        case SqlServerMigrationRunner.Engine:
            var sqlServerRunner = new SqlServerMigrationRunner(connectionString, directory.FullName);
            sqlServerRunner.RunMigrations(isUpMigration);

            break;
        case PostgresMigrationRunner.Engine:
            var postgresRunner = new PostgresMigrationRunner(connectionString, directory.FullName);
            postgresRunner.RunMigrations(isUpMigration);

            break;
        default:
            Console.WriteLine($"Engine '{engine}' is not supported");
            break;
    }

    Console.WriteLine("Migrated successfully");
}

// static void RunMigrations( bool up, bool down, string? to, string engine, string? connectionString)
// {
//     Console.WriteLine($"--directory = {directory}");
//     Console.WriteLine($"--up = {up}");
//     Console.WriteLine($"--down = {down}");
//     Console.WriteLine($"--to = {to}");
//     Console.WriteLine($"--engine = {engine}");
//     Console.WriteLine($"--connection = {connectionString}");

//     Console.ReadKey();

//     return;

//     MigrationRunner runner = new MigrationRunner("Your connection string", directory.FullName);

//     if (up)
//     {
//         Console.WriteLine("Applying migrations...");
//         runner.RunMigrations(true);
//     }
//     else if (down)
//     {
//         Console.WriteLine("Reverting migrations...");
//         runner.RunMigrations(false);
//     }
//     else
//     {
//         Console.WriteLine("No operation specified. Use --up or --down.");
//     }
// }
