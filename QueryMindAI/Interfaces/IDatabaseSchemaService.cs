namespace QueryMindAI.Interfaces
{
    public interface IDatabaseSchemaService
    {
        Task<List<string>> GetTableNamesAsync();

        Task<string> GetDatabaseSchemaAsync();

        Task<string> GetSelectedTablesSchemaAsync(
            List<string> selectedTables);
    }
}