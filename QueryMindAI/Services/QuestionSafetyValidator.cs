using System.Text.RegularExpressions;
using QueryMindAI.Interfaces;

namespace QueryMindAI.Services
{
    public class QuestionSafetyValidator
        : IQuestionSafetyValidator
    {
        private static readonly string[]
            BlockedCommands =
            {
                "delete",
                "drop",
                "truncate",
                "update",
                "insert",
                "alter",
                "merge",
                "execute",
                "exec",
                "create"
            };

        public bool IsSafe(
            string question,
            out string message)
        {
            foreach (string command
                     in BlockedCommands)
            {
                string pattern =
                    $@"\b{Regex.Escape(command)}\b";

                if (Regex.IsMatch(
                    question,
                    pattern,
                    RegexOptions.IgnoreCase))
                {
                    message =
                        $"The request contains the " +
                        $"blocked operation '{command}'. " +
                        "QueryMind only supports " +
                        "read-only questions.";

                    return false;
                }
            }

            message = string.Empty;

            return true;
        }
    }
}