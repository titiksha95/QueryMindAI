using System.ComponentModel.DataAnnotations;

namespace QueryMindAI.ViewModels
{
    public class QueryAssistantViewModel
    {
        [Display(Name = "Ask your database")]
        [Required(ErrorMessage = "Please enter a question.")]
        public string Question { get; set; } =
            string.Empty;

        public string GeneratedSql { get; set; } =
            string.Empty;

        public List<string> AvailableTables { get; set; } =
            new();

        public List<string> ResultColumns { get; set; } =
            new();

        public List<Dictionary<string, object?>>
            ResultRows
        { get; set; } = new();

        public string DatabaseSchema { get; set; } =
        string.Empty;

        public string? ErrorMessage { get; set; }

        public bool? IsSqlSafe { get; set; }

        public string SqlValidationMessage { get; set; } =
            string.Empty;

        public long ExecutionTimeMs { get; set; }

        public List<string> SelectedTables { get; set; } =
            new();

    }
}