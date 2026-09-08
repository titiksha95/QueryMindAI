namespace QueryMindAI.Interfaces
{
    public interface IQuestionSafetyValidator
    {
        bool IsSafe(
            string question,
            out string message);
    }
}