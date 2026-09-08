namespace QueryMindAI.Models
{
    public class SqlValidationResult
    {
        public bool IsSafe { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}