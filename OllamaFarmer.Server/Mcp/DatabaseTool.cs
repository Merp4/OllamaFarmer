using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Mcp;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace OllamaFarmer.Server.MCP
{



    [McpServerToolType()]
    public class DatabaseTool
    {

        [McpServerTool, Description("Executes an SQL query against the database, and returns the results in CSV format")]
        public static async Task<string> ExecuteQuery(
            IMcpServer thisServer,
            [Description("The raw SQL query to execute")] string query
            )
        {
            using var ctx = new ToolContext(nameof(DatabaseTool), thisServer, thisServer.Services!);
   
            if (string.IsNullOrWhiteSpace(query))
            {
                ctx.Logger.LogError("Query is null or empty.");
                return "Query is null or empty.";
            }

            ctx.Logger.LogInformation("DatabaseTool executing query: {Query}", query);
            using var conn = ctx.AppDbContext.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            await conn.OpenAsync();
            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                ctx.Logger.LogInformation("Query executed successfully, processing results...");
                if (!reader.HasRows)
                {
                    ctx.Logger.LogInformation("Query returned no results.");
                    return "Query returned no results.";
                }
                ctx.Logger.LogInformation("Query returned {RowCount} rows.", reader.FieldCount);
                var result = await ReadResultsAsCsv(reader);
                await reader.CloseAsync();
                ctx.Logger.LogInformation("Query results processed successfully.");
                return result;
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex, "Error executing query: {Query}", query);
                return $"Error executing query: {ex.Message}";
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // method to fetch DB structure information
        [McpServerTool, Description("Queries the structure of the database, including tables, columns, and their types. Returns results in CSV format.")]
        public static async Task<string> GetBasicDatabaseStructure(
            IMcpServer thisServer
            )
        {
            var queryTables = @"
SELECT TABLE_SCHEMA, TABLE_NAME, 
        TABLE_TYPE, TABLE_ROWS, 
        CREATE_TIME, UPDATE_TIME
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = DATABASE();
            ";

            var queryTableColumns = @"
SELECT TABLE_SCHEMA, TABLE_NAME, 
        COLUMN_NAME,
        COLUMN_TYPE, IS_NULLABLE,
        COLUMN_DEFAULT,
        EXTRA
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE();
            ";

            var queryTableConstraints = @"
SELECT TABLE_SCHEMA, TABLE_NAME,
        CONSTRAINT_NAME,
        CONSTRAINT_TYPE,
        COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_SCHEMA = DATABASE();
            ";


            var query = @$"
SELECT * 
FROM
    ({queryTables}) Tables
    JOIN ({queryTableColumns}) Columns
        ON Tables.TABLE_SCHEMA = Columns.TABLE_SCHEMA
        AND Tables.TABLE_NAME = Columns.TABLE_NAME  
    JOIN ({queryTableConstraints}) Constraints
        ON Tables.TABLE_SCHEMA = Constraints.TABLE_SCHEMA
        AND Tables.TABLE_NAME = Constraints.TABLE_NAME
        AND Columns.COLUMN_NAME = Constraints.COLUMN_NAME
            ";


            using var ctx = new ToolContext(nameof(DatabaseTool), thisServer, thisServer.Services!);
            using var conn = ctx.AppDbContext.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = queryTables;

            ctx.Logger.LogInformation("Fetching database structure...");

            await conn.OpenAsync();
            try 
            {
                ctx.Logger.LogInformation("Executing query to fetch database structure.");
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    ctx.Logger.LogInformation("No tables found in the database.");
                    return "No tables found in the database.";
                }

                ctx.Logger.LogInformation("Database structure query executed successfully, processing results...");

                var result = await ReadResultsAsCsv(reader);
                await reader.CloseAsync();

                ctx.Logger.LogInformation("Database structure fetched successfully.");
                var resultContext = "INFORMATION_SCHEMA.TABLES, INFORMATION_SCHEMA.COLUMNS, and INFORMATION_SCHEMA.CONSTRAINTS have been queried in the database. The results of this query follows in CSV format:\n\n";
                return $"{resultContext}{result}";
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex, "Error fetching database structure.");
                return $"Error fetching database structure: {ex.Message}";
            }
            finally
            {
                await conn.CloseAsync();
            }


        }



        private static async Task<string> ReadResultsAsCsv(System.Data.Common.DbDataReader reader)
        {
            var sb = new StringBuilder();
            var columnNames = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();
            sb.AppendLine(string.Join(",", columnNames));

            while (await reader.ReadAsync())
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                    values.Add(value);
                }
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }
    }
}



/*
You are a calculation formula assistant, this is your sole concern. You will be provided with a set of fields, and a user prompt.
You will use the provided fields as the variables in the formula as appropriate. You will only use simple mathematic operations.
Prompts will be provided to you in the format <fields> ... comma separated fields ... </fields> <prompt> ... user prompt ... </prompt>.
Your responses will only be in the format <calc> ... the proposed formula/calculation ... </calc> <msg> ... a short explanation ... </msg>.
*/
