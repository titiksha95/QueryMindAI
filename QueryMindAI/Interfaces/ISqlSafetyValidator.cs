using QueryMindAI.Models;

namespace QueryMindAI.Interfaces
{
    public interface ISqlSafetyValidator
    {
        SqlValidationResult Validate(string sql);
    }
}