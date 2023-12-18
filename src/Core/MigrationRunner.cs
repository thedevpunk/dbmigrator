using System.Data.SqlClient;

namespace DevPunk.DbMigrator.Core;

public class MigrationRunner
{
    private readonly string _connectionString;
    private readonly string _scriptDirectory;

    public MigrationRunner(string connectionString, string scriptDirectory)
    {
        _connectionString = connectionString;
        _scriptDirectory = scriptDirectory;
    }

    public void RunMigrations(bool isUpMigration)
    {
        Console.WriteLine($"Directory: {_scriptDirectory}");

        var executedScripts = GetExecutedScripts();

        var scriptFiles = Directory.GetFiles(_scriptDirectory, "*.sql");

        if (isUpMigration)
        {
            Array.Sort(scriptFiles); // Sort by name to ensure scripts are executed in order
        }
        else
        {
            Array.Reverse(scriptFiles); // Reverse to ensure scripts are executed in descending order
        }

        Console.WriteLine($"Scripts to execute: {string.Join(", ", scriptFiles)}");

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            if (executedScripts.Count == 0)
            {
                CreateMigrationTableIfNotExists(connection);
            }

            foreach (var scriptFile in scriptFiles)
            {
                var scriptName = Path.GetFileName(scriptFile);
                if (!executedScripts.Contains(scriptName))
                {
                    var scriptContent = File.ReadAllText(scriptFile);

                    var scriptParts = scriptContent.Split(new string[] { "-- Down" }, StringSplitOptions.None);
                    var upScript = scriptParts[0].Replace("-- Up", "");
                    var downScript = scriptParts[1].Replace("-- Down", "");

                    var scriptToExecute = isUpMigration ? upScript : downScript;

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            Console.WriteLine($"Executing script: {scriptName}");
                            Console.WriteLine($"Script: {scriptToExecute}");

                            using (var command = new SqlCommand(scriptContent, connection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            RecordScriptAsExecuted(scriptName, connection, transaction);

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

    private void CreateMigrationTableIfNotExists(SqlConnection connection)
    {
        var sql = @"
            IF NOT EXISTS (
                SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME='DatabaseMigrations')
            CREATE TABLE [dbo].[DatabaseMigrations](
                Id INT IDENTITY PRIMARY KEY,
                ScriptName NVARCHAR(255),
                Applied DATETIME DEFAULT GETDATE()
            )
        ";

        using (var command = new SqlCommand(sql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    private bool DatabaseMigrationsTableExists(SqlConnection connection)
    {
        var sql = @"
            SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME='DatabaseMigrations'
        ";

        using (var command = new SqlCommand(sql, connection))
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


        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            if (!DatabaseMigrationsTableExists(connection))
            {
                return executedScripts;
            }

            using (var command = new SqlCommand("SELECT ScriptName FROM DatabaseMigrations", connection))
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

    private void RecordScriptAsExecuted(string scriptName, SqlConnection connection, SqlTransaction transaction)
    {
        using (var command = new SqlCommand("INSERT INTO DatabaseMigrations (ScriptName) VALUES (@scriptName)", connection, transaction))
        {
            command.Parameters.AddWithValue("@scriptName", scriptName);
            command.ExecuteNonQuery();
        }
    }
}
