using System.Net.Http.Json;
using System.Text.Json;
using QueryMindAI.Interfaces;

namespace QueryMindAI.Services
{
    public class GeminiSqlGeneratorService
        : IAiSqlGeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiSqlGeneratorService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateSqlAsync(
            string question,
            string databaseSchema)
        {
            string apiKey =
                _configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException(
                    "Gemini API key was not found.");

            string model =
                _configuration["Gemini:Model"]
                ?? "gemini-3.6-flash";

            string baseUrl =
                _configuration["Gemini:BaseUrl"]
                ?? "https://generativelanguage.googleapis.com/v1beta/models";

            string prompt = $"""
                You are an expert Microsoft SQL Server assistant.

                Convert the user's question into one safe,
                read-only SQL Server SELECT query.

                Rules:
                1. Return only the SQL query.
                2. Do not include Markdown code blocks.
                3. Do not include explanations.
                4. Only generate a SELECT statement.
                5. Never generate INSERT, UPDATE, DELETE,
                   DROP, ALTER, TRUNCATE, MERGE or EXEC.
                6. Use only tables and columns from the
                   provided database schema.
                7. Use SQL Server syntax.
                8. Add TOP 100 unless the user requests
                   a specific number of records.
                9. Include the schema name in table names.

                DATABASE SCHEMA:
                {databaseSchema}

                USER QUESTION:
                {question}
                """;

            object requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",

                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },

                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 1000
                }
            };

            string requestUrl =
                $"{baseUrl}/{model}:generateContent";

           

            HttpResponseMessage? response = null;
            string responseJson = string.Empty;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using HttpRequestMessage retryRequest =
                    new(HttpMethod.Post, requestUrl);

                retryRequest.Headers.Add(
                    "x-goog-api-key",
                    apiKey);

                retryRequest.Content =
                    JsonContent.Create(requestBody);

                response =
                    await _httpClient.SendAsync(retryRequest);

                responseJson =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                if ((int)response.StatusCode == 503 &&
                    attempt < 3)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(attempt * 2));

                    continue;
                }

                throw new InvalidOperationException(
                    "Gemini request failed: "
                    + responseJson);
            }

            if (response == null ||
                !response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Gemini is temporarily unavailable. " +
                    "Please try again shortly.");
            }

            using JsonDocument document =
                JsonDocument.Parse(responseJson);

            string? generatedText =
                document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            if (string.IsNullOrWhiteSpace(
                generatedText))
            {
                throw new InvalidOperationException(
                    "Gemini returned an empty response.");
            }

            return CleanSql(generatedText);
        }

        private static string CleanSql(string sql)
        {
            return sql
                .Replace("```sql", "",
                    StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();
        }
    }
}