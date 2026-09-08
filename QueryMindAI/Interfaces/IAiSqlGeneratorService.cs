namespace QueryMindAI.Interfaces
{
    public interface IAiSqlGeneratorService
    {
        Task<string> GenerateSqlAsync(
            string question,
            string databaseSchema);
    }
}