using Microsoft.SqlServer.TransactSql.ScriptDom;
using QueryMindAI.Interfaces;
using QueryMindAI.Models;

namespace QueryMindAI.Services
{
    public class SqlSafetyValidator
        : ISqlSafetyValidator
    {
        public SqlValidationResult Validate(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return Invalid(
                    "The generated SQL is empty.");
            }

            TSql160Parser parser =
                new(initialQuotedIdentifiers: true);

            using StringReader reader =
                new(sql);

            TSqlFragment fragment =
                parser.Parse(
                    reader,
                    out IList<ParseError> errors);

            if (errors.Count > 0)
            {
                string errorMessage =
                    string.Join(
                        ", ",
                        errors.Select(error =>
                            error.Message));

                return Invalid(
                    "Invalid SQL syntax: "
                    + errorMessage);
            }

            if (fragment is not TSqlScript script)
            {
                return Invalid(
                    "The SQL could not be analyzed.");
            }

            if (script.Batches.Count != 1)
            {
                return Invalid(
                    "Only one SQL batch is allowed.");
            }

            IList<TSqlStatement> statements =
                script.Batches[0].Statements;

            if (statements.Count != 1)
            {
                return Invalid(
                    "Only one SQL statement is allowed.");
            }

            if (statements[0]
                is not SelectStatement selectStatement)
            {
                return Invalid(
                    "Only SELECT queries are allowed.");
            }

            if (selectStatement.Into != null)
            {
                return Invalid(
                    "SELECT INTO is not allowed.");
            }

            return new SqlValidationResult
            {
                IsSafe = true,
                Message =
                    "The generated SQL passed safety validation."
            };
        }

        private static SqlValidationResult Invalid(
            string message)
        {
            return new SqlValidationResult
            {
                IsSafe = false,
                Message = message
            };
        }
    }
}