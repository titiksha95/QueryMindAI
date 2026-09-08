using Microsoft.Data.SqlClient;
using QueryMindAI.Interfaces;
using System.Text;

namespace QueryMindAI.Services
{
    public class DatabaseSchemaService
        : IDatabaseSchemaService
    {
        private readonly string _connectionString;

        public DatabaseSchemaService(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString(
                    "TargetDatabase")
                ?? throw new InvalidOperationException(
                    "TargetDatabase connection string was not found.");
        }

        public async Task<List<string>>
            GetTableNamesAsync()
        {
            List<string> tableNames = new();

            const string sql = """
                SELECT
                    TABLE_SCHEMA,
                    TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME;
                """;

            await using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string schema = reader.GetString(0);
                string table = reader.GetString(1);

                tableNames.Add($"{schema}.{table}");
            }

            return tableNames;
        }

        public async Task<string> GetDatabaseSchemaAsync()
        {
            StringBuilder schemaBuilder = new();

            const string sql = """
        SELECT
            TABLE_SCHEMA,
            TABLE_NAME,
            COLUMN_NAME,
            DATA_TYPE,
            IS_NULLABLE,
            CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        ORDER BY
            TABLE_SCHEMA,
            TABLE_NAME,
            ORDINAL_POSITION;
        """;

            await using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            string? previousTable = null;

            while (await reader.ReadAsync())
            {
                string tableSchema =
                    reader.GetString(0);

                string tableName =
                    reader.GetString(1);

                string columnName =
                    reader.GetString(2);

                string dataType =
                    reader.GetString(3);

                string isNullable =
                    reader.GetString(4);

                object maximumLength =
                    reader.GetValue(5);

                string fullTableName =
                    $"{tableSchema}.{tableName}";

                if (previousTable != fullTableName)
                {
                    schemaBuilder.AppendLine();
                    schemaBuilder.AppendLine(
                        $"TABLE: {fullTableName}");

                    previousTable = fullTableName;
                }

                string lengthText = string.Empty;

                if (maximumLength != DBNull.Value)
                {
                    lengthText =
                        $"({maximumLength})";
                }

                schemaBuilder.AppendLine(
                    $"- {columnName}: " +
                    $"{dataType}{lengthText}, " +
                    $"Nullable: {isNullable}");
            }

            return schemaBuilder.ToString();
        }
    }
}