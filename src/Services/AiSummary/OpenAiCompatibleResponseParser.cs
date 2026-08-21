using System.IO;
using System.Net.Http;
using System.Text;

namespace Game_Daily_Routine_Launcher;

internal static class OpenAiCompatibleResponseParser
{
    public static async Task<string> ReadStreamedContentAsync(
        HttpResponseMessage response,
        string providerDisplayName,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "未知";
        return ExtractStreamedBody(body, providerDisplayName, contentType, response.RequestMessage?.RequestUri);
    }

    public static string ExtractMessageFromJson(
        string json,
        string providerDisplayName,
        string? contentType = null,
        Uri? requestUri = null)
    {
        if (IsHtmlResponse(contentType, json))
            throw CreateHtmlResponseException(providerDisplayName, requestUri);

        var result = AiResponseJsonParser.Parse(json);
        ThrowForResponseError(result, providerDisplayName);
        if (!string.IsNullOrWhiteSpace(result.Content))
            return result.Content.Trim();

        throw CreateNoContentException(providerDisplayName, result.Diagnostic, contentType ?? "未知", json);
    }

    internal static string ExtractStreamedBody(
        string body,
        string providerDisplayName,
        string contentType,
        Uri? requestUri = null)
    {
        if (IsHtmlResponse(contentType, body))
            throw CreateHtmlResponseException(providerDisplayName, requestUri);

        var deltaBuilder = new StringBuilder();
        var snapshot = string.Empty;
        var lastDiagnostic = string.Empty;
        var sawFramedData = false;
        var sawReasoning = false;

        using var reader = new StringReader(body);
        while (reader.ReadLine() is { } line)
        {
            var data = GetEventData(line);
            if (data is not null)
            {
                sawFramedData = true;
                if (data == "[DONE]")
                    continue;
                Collect(AiResponseJsonParser.Parse(data));
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                var lineResult = AiResponseJsonParser.Parse(trimmed);
                if (lineResult.IsJson)
                    Collect(lineResult);
            }
        }

        if (deltaBuilder.Length > 0)
            return deltaBuilder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(snapshot))
            return snapshot.Trim();

        if (!sawFramedData && !string.IsNullOrWhiteSpace(body))
        {
            var fullResult = AiResponseJsonParser.Parse(body);
            ThrowForResponseError(fullResult, providerDisplayName);
            if (!string.IsNullOrWhiteSpace(fullResult.Content))
                return fullResult.Content.Trim();
            lastDiagnostic = fullResult.Diagnostic;
        }

        if (sawReasoning && !lastDiagnostic.Contains("推理字段", StringComparison.Ordinal))
            lastDiagnostic = string.IsNullOrWhiteSpace(lastDiagnostic)
                ? "流中仅检测到推理字段"
                : $"{lastDiagnostic}；流中仅检测到推理字段";

        throw CreateNoContentException(providerDisplayName, lastDiagnostic, contentType, body);

        void Collect(AiResponseParseResult result)
        {
            ThrowForResponseError(result, providerDisplayName);
            sawReasoning |= result.HasReasoning;
            if (!string.IsNullOrWhiteSpace(result.Diagnostic))
                lastDiagnostic = result.Diagnostic;
            if (string.IsNullOrEmpty(result.Content))
                return;

            if (result.ContentKind == AiResponseContentKind.Delta)
                deltaBuilder.Append(result.Content);
            else
                snapshot = result.Content;
        }
    }

    private static string? GetEventData(string line)
    {
        return line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? line["data:".Length..].Trim()
            : null;
    }

    private static void ThrowForResponseError(AiResponseParseResult result, string providerDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
            throw new AiSummaryResponseException($"{providerDisplayName}返回错误：{result.Error}");

        if (!string.IsNullOrWhiteSpace(result.Refusal))
            throw new AiSummaryResponseException($"{providerDisplayName}拒绝生成总结：{result.Refusal}");
    }

    private static AiSummaryResponseException CreateNoContentException(
        string providerDisplayName,
        string diagnostic,
        string contentType,
        string body)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(diagnostic))
            details.Add(diagnostic);
        details.Add($"Content-Type={contentType}");
        if (string.IsNullOrWhiteSpace(body))
            details.Add("响应体为空");
        else if (string.Equals(diagnostic, "响应不是有效 JSON", StringComparison.Ordinal))
            details.Add($"响应开头={BuildSafeExcerpt(body)}");

        return new AiSummaryResponseException(
            $"{providerDisplayName}返回结果中未找到正文（{string.Join("；", details)}）。");
    }

    private static AiSummaryResponseException CreateHtmlResponseException(
        string providerDisplayName,
        Uri? requestUri)
    {
        var endpoint = FormatEndpoint(requestUri);
        var endpointDetail = string.IsNullOrWhiteSpace(endpoint)
            ? string.Empty
            : $"，实际请求地址：{endpoint}";
        return new AiSummaryResponseException(
            $"{providerDisplayName} API URL 返回了 HTML 网页而不是接口数据{endpointDetail}。" +
            "请填写完整的 OpenAI 兼容接口地址，不要填写网站首页或控制台地址。");
    }

    private static bool IsHtmlResponse(string? contentType, string body)
    {
        if (contentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var trimmed = body.AsSpan().TrimStart();
        return trimmed.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatEndpoint(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
            return string.Empty;

        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{uri.Host}{port}{uri.AbsolutePath}";
    }

    private static string BuildSafeExcerpt(string body)
    {
        var normalized = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 160 ? normalized : $"{normalized[..160]}...";
    }
}
