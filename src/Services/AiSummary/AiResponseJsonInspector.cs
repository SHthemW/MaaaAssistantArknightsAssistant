using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

internal static class AiResponseJsonInspector
{
    public static string FindFirstString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                var nested = FindFirstString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return string.Empty;
    }

    public static bool HasReasoning(JsonElement root)
    {
        if (!string.IsNullOrWhiteSpace(FindFirstString(root, "reasoning_content")))
            return true;

        return ContainsObjectType(root, "reasoning");
    }

    public static string BuildDiagnostic(JsonElement root, bool hasReasoning)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return $"根节点类型={root.ValueKind}";

        var parts = new List<string>();
        AddProperty(parts, "type", TryGetString(root, "type"));
        AddProperty(parts, "status", TryGetString(root, "status"));

        var payload = root.TryGetProperty("response", out var response) &&
                      response.ValueKind == JsonValueKind.Object
            ? response
            : root;
        if (payload.ValueKind == JsonValueKind.Object && payload.GetRawText() != root.GetRawText())
            AddProperty(parts, "response.status", TryGetString(payload, "status"));

        if (TryGetFirstChoice(payload, out var choice))
        {
            AddProperty(parts, "finish_reason", TryGetString(choice, "finish_reason"));
            parts.Add($"choice字段={JoinPropertyNames(choice)}");
        }

        var incompleteReason = FindFirstString(payload, "reason");
        if (payload.TryGetProperty("incomplete_details", out _) && !string.IsNullOrWhiteSpace(incompleteReason))
            parts.Add($"incomplete_reason={incompleteReason}");
        if (hasReasoning)
            parts.Add("仅检测到推理字段");
        parts.Add($"根字段={JoinPropertyNames(root)}");
        return string.Join("；", parts);
    }

    public static bool TryGetFirstChoice(JsonElement root, out JsonElement choice)
    {
        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            choice = choices[0];
            return true;
        }

        choice = default;
        return false;
    }

    public static string TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool ContainsObjectType(JsonElement element, string expectedType)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (string.Equals(TryGetString(element, "type"), expectedType, StringComparison.OrdinalIgnoreCase))
                return true;

            return element.EnumerateObject().Any(property => ContainsObjectType(property.Value, expectedType));
        }

        return element.ValueKind == JsonValueKind.Array &&
               element.EnumerateArray().Any(item => ContainsObjectType(item, expectedType));
    }

    private static void AddProperty(List<string> parts, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parts.Add($"{name}={value}");
    }

    private static string JoinPropertyNames(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object
            ? string.Join(',', element.EnumerateObject().Select(property => property.Name).Take(12))
            : element.ValueKind.ToString();
    }
}
