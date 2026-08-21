using System.Text;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

internal enum AiResponseContentKind
{
    None,
    Delta,
    Snapshot
}

internal readonly record struct AiResponseParseResult(
    bool IsJson,
    string Content,
    AiResponseContentKind ContentKind,
    string Error,
    string Refusal,
    bool HasReasoning,
    string Diagnostic);

internal static class AiResponseJsonParser
{
    public static AiResponseParseResult Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var error = ExtractError(root);
            var refusal = AiResponseJsonInspector.FindFirstString(root, "refusal");
            var eventType = AiResponseJsonInspector.TryGetString(root, "type");
            if (string.IsNullOrWhiteSpace(refusal) &&
                eventType.Contains("refusal", StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("delta", out var refusalDelta))
                refusal = ExtractText(refusalDelta);
            var (content, contentKind) = ExtractContent(root);
            var hasReasoning = AiResponseJsonInspector.HasReasoning(root);
            var diagnostic = AiResponseJsonInspector.BuildDiagnostic(root, hasReasoning);
            return new(true, content, contentKind, error, refusal, hasReasoning, diagnostic);
        }
        catch (JsonException)
        {
            return new(false, string.Empty, AiResponseContentKind.None, string.Empty,
                string.Empty, false, "响应不是有效 JSON");
        }
    }

    private static (string Content, AiResponseContentKind Kind) ExtractContent(JsonElement root)
    {
        if (AiResponseJsonInspector.TryGetFirstChoice(root, out var choice))
        {
            if (choice.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var deltaContent))
                return (ExtractText(deltaContent), AiResponseContentKind.Delta);

            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var messageContent))
                return (ExtractText(messageContent), AiResponseContentKind.Snapshot);

            if (choice.TryGetProperty("text", out var choiceText))
                return (ExtractText(choiceText), AiResponseContentKind.Snapshot);
        }

        var eventType = AiResponseJsonInspector.TryGetString(root, "type");
        if (eventType.EndsWith(".delta", StringComparison.OrdinalIgnoreCase) &&
            root.TryGetProperty("delta", out var rootDelta))
            return (ExtractText(rootDelta), AiResponseContentKind.Delta);

        if (eventType.EndsWith(".done", StringComparison.OrdinalIgnoreCase) &&
            root.TryGetProperty("text", out var doneText))
            return (ExtractText(doneText), AiResponseContentKind.Snapshot);

        if (root.TryGetProperty("response", out var response))
        {
            var nested = ExtractContent(response);
            if (!string.IsNullOrWhiteSpace(nested.Content))
                return nested;
        }

        if (root.TryGetProperty("output", out var output))
            return (ExtractText(output), AiResponseContentKind.Snapshot);

        if (root.TryGetProperty("output_text", out var outputText))
            return (ExtractText(outputText), AiResponseContentKind.Snapshot);

        if (root.TryGetProperty("content", out var content))
            return (ExtractText(content), AiResponseContentKind.Snapshot);

        if (root.TryGetProperty("data", out var data) && data.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            return (ExtractText(data), AiResponseContentKind.Snapshot);

        return (string.Empty, AiResponseContentKind.None);
    }

    private static string ExtractText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? string.Empty;

        if (element.ValueKind == JsonValueKind.Array)
        {
            var builder = new StringBuilder();
            foreach (var item in element.EnumerateArray())
                builder.Append(ExtractText(item));
            return builder.ToString();
        }

        if (element.ValueKind != JsonValueKind.Object)
            return string.Empty;

        if (element.TryGetProperty("text", out var text))
            return ExtractText(text);

        if (element.TryGetProperty("output_text", out var outputText))
            return ExtractText(outputText);

        if (element.TryGetProperty("content", out var content))
            return ExtractText(content);

        if (element.TryGetProperty("message", out var message))
            return ExtractText(message);

        return string.Empty;
    }

    private static string ExtractError(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error))
        {
            if (root.TryGetProperty("response", out var response) &&
                response.ValueKind == JsonValueKind.Object)
                return ExtractError(response);

            if (!string.Equals(
                    AiResponseJsonInspector.TryGetString(root, "type"),
                    "error",
                    StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return FormatError(
                AiResponseJsonInspector.TryGetString(root, "code"),
                AiResponseJsonInspector.TryGetString(root, "message"));
        }

        if (error.ValueKind == JsonValueKind.String)
            return error.GetString() ?? string.Empty;

        if (error.ValueKind != JsonValueKind.Object)
            return error.ToString();

        return FormatError(
            AiResponseJsonInspector.TryGetString(error, "code"),
            AiResponseJsonInspector.TryGetString(error, "message"));
    }

    private static string FormatError(string code, string message)
    {
        return string.IsNullOrWhiteSpace(code)
            ? message
            : $"{code}: {message}".TrimEnd(' ', ':');
    }
}
