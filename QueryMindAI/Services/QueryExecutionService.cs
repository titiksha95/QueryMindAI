using System.Diagnostics;
using Microsoft.Data.SqlClient;
using QueryMindAI.Interfaces;
using QueryMindAI.Models;

namespace QueryMindAI.Services
{
    public class QueryExecutionService
        : IQueryExecutionService
    {
        private readonly string _connectionString;

        public QueryExecutionService(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString(
                    "TargetDatabase")
                ?? throw new InvalidOperationException(
                    "TargetDatabase was not found.");
        }

        public async Task<QueryExecutionResult>
            ExecuteAsync(string sql)
        {
            QueryExecutionResult result = new();

            await using SqlConnection connection =
                new(_connectionString);

            await connection.OpenAsync();

            await using SqlCommand command =
                new(sql, connection);

            command.CommandTimeout = 30;

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            for (int index = 0;
                 index < reader.FieldCount;
                 index++)
            {
                string columnName =
                    reader.GetName(index);

                // Handle duplicate column names from JOIN queries.
                if (result.Columns.Contains(
                    columnName,
                    StringComparer.OrdinalIgnoreCase))
                {
                    columnName =
                        $"{columnName}_{index + 1}";
                }

                result.Columns.Add(columnName);
            }

            // Application-level maximum of 100 rows.
            while (await reader.ReadAsync() &&
                   result.Rows.Count < 100)
            {
                Dictionary<string, object?> row =
                    new();

                for (int index = 0;
                     index < reader.FieldCount;
                     index++)
                {
                    object? value =
                        await reader.IsDBNullAsync(index)
                            ? null
                            : reader.GetValue(index);

                    row[result.Columns[index]] = value;
                }

                result.Rows.Add(row);
            }

            stopwatch.Stop();

            result.ExecutionTimeMs =
                stopwatch.ElapsedMilliseconds;

            return result;
        }
    }
}