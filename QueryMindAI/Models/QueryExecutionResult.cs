namespace QueryMindAI.Models
{
    public class QueryExecutionResult
    {
        public List<string> Columns { get; set; } =
            new();

        public List<Dictionary<string, object?>>
            Rows
        { get; set; } = new();

        public long ExecutionTimeMs { get; set; }
    }
}