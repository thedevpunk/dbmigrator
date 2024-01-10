using Npgsql;

namespace DevPunk.DbMigrator.Core.MigrationRunners;

public class PostgresMigrationRunner : IMigrationRunner
{
    public const string Engine = "Postgres";
    private readonly string _connectionString;
    private readonly string _scriptDirectory;

    public PostgresMigrationRunner(string connectionString, string scriptDirectory)
    {
        _connectionString = connectionString;
        _scriptDirectory = scriptDirectory;
    }

    public void RunMigrations(bool isUpMigration)
    {
        Console.WriteLine($"Directory: {_scriptDirectory}");

        var executedScripts = GetExecutedScripts();

        var sqlFiles = Directory.GetFiles(_scriptDirectory, "*.sql");
        var pgsqlFiles = Directory.GetFiles(_scriptDirectory, "*.pgsql");
        var scriptFiles = sqlFiles.Concat(pgsqlFiles).ToArray();

        if (isUpMigration)
        {
            Array.Sort(scriptFiles); // Sort by name to ensure scripts are executed in order
        }
        else
        {
            Array.Reverse(scriptFiles); // Reverse to ensure scripts are executed in descending order
        }

        Console.WriteLine($"Scripts to execute: {string.Join(", ", scriptFiles)}");

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            if (executedScripts.Count == 0)
            {
                CreateMigrationTableIfNotExists(connection);
            }

            foreach (var scriptFile in scriptFiles)
            {
                var scriptName = Path.GetFileName(scriptFile);

                if (isUpMigration ? !executedScripts.Contains(scriptName) : executedScripts.Contains(scriptName))
                {
                    var scriptContent = File.ReadAllText(scriptFile);

                    var scriptParts = scriptContent.Split(new string[] { "-- Down" }, StringSplitOptions.None);
                    var upScript = scriptParts[0].Replace("-- Up", "");
                    var downScript = scriptParts[1].Replace("-- Down", "");

                    var scriptToExecute = isUpMigration ? upScript : downScript;

                    using (var transaction = connection.BeginTransaction())
                    {
                        Console.WriteLine($"Executing script: {scriptName}");
                        Console.WriteLine($"Script: {scriptToExecute}");

                        using (var command = new NpgsqlCommand(scriptToExecute, connection, transaction))
                        {
                            try
                            {
                                command.ExecuteNonQuery();

                                if (isUpMigration)
                                {
                                    RecordScriptAsExecuted(scriptName, connection, transaction);
                                }
                                else
                                {
                                    RemoveScriptAsExecuted(scriptName, connection, transaction);
                                }

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception("Error executing script " + scriptName, ex);
                            }
                        }
                    }
                }
            }
        }
    }

    private void CreateMigrationTableIfNotExists(NpgsqlConnection connection)
    {
        var sql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT FROM pg_catalog.pg_tables
                    WHERE schemaname = 'public'
                    AND tablename = 'database_migrations') THEN
                    CREATE TABLE public.database_migrations(
                        Id SERIAL PRIMARY KEY,
                        ScriptName VARCHAR(255),
                        Applied TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                END IF;
            END $$;
        ";

        using (var command = new NpgsqlCommand(sql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    private bool MigrationsTableExists(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
            AND tablename = 'database_migrations'
        ";

        using (var command = new NpgsqlCommand(sql, connection))
        {
            using (var reader = command.ExecuteReader())
            {
                return reader.HasRows;
            }
        }
    }

    private HashSet<string> GetExecutedScripts()
    {
        var executedScripts = new HashSet<string>();


        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            if (!MigrationsTableExists(connection))
            {
                return executedScripts;
            }

            using (var command = new NpgsqlCommand("SELECT ScriptName FROM database_migrations", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        executedScripts.Add(reader.GetString(0));
                    }
                }
            }
        }

        return executedScripts;
    }

    private void RecordScriptAsExecuted(string scriptName, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using (var command = new NpgsqlCommand("INSERT INTO database_migrations (ScriptName) VALUES (@scriptName)", connection, transaction))
        {
            command.Parameters.AddWithValue("@scriptName", scriptName);
            command.ExecuteNonQuery();
        }
    }

    private void RemoveScriptAsExecuted(string scriptName, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using (var command = new NpgsqlCommand("DELETE FROM database_migrations WHERE ScriptName = @scriptName", connection, transaction))
        {
            command.Parameters.AddWithValue("@scriptName", scriptName);
            command.ExecuteNonQuery();
        }
    }
}
