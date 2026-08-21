namespace Game_Daily_Routine_Launcher;

internal sealed class AiSummaryResponseException : InvalidOperationException
{
    public AiSummaryResponseException(string message)
        : base(message)
    {
    }
}
