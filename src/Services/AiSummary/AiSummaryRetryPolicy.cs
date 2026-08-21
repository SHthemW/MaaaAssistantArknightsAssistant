using System.IO;
using System.Net;
using System.Net.Http;

namespace Game_Daily_Routine_Launcher;

internal static class AiSummaryRetryPolicy
{
    public static bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            AiSummaryResponseException => false,
            HttpRequestException httpException => IsTransient(httpException.StatusCode),
            OperationCanceledException => true,
            IOException => true,
            _ => false
        };
    }

    private static bool IsTransient(HttpStatusCode? statusCode)
    {
        if (!statusCode.HasValue)
            return true;

        var numericStatus = (int)statusCode.Value;
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict ||
               numericStatus == 429 ||
               numericStatus >= 500;
    }
}
