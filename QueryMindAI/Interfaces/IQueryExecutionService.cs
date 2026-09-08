using QueryMindAI.Models;

namespace QueryMindAI.Interfaces
{
    public interface IQueryExecutionService
    {
        Task<QueryExecutionResult> ExecuteAsync(
            string sql);
    }
}