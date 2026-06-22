using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

public static class AiSummaryPromptLogFormatter
{
    private static readonly JsonSerializerOptions ReadableJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string Format(LogEntryRecord entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}");

        var body = FormatRawBody(entry.RawBody);
        if (!string.IsNullOrWhiteSpace(body))
        {
            builder.AppendLine("原始内容：");
            builder.AppendLine(body);
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatRawBody(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return string.Empty;

        var text = rawBody.Trim();
        if (TryFormatJson(text, out var formattedJson))
            return formattedJson;

        if (TryFormatEmbeddedJson(text, out var formattedEmbeddedJson))
            return formattedEmbeddedJson;

        return text;
    }

    private static bool TryFormatEmbeddedJson(string text, out string formatted)
    {
        formatted = string.Empty;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        var jsonText = text[start..(end + 1)];
        if (!TryFormatJson(jsonText, out var formattedJson))
            return false;

        var builder = new StringBuilder();
        var prefix = text[..start].Trim();
        var suffix = text[(end + 1)..].Trim();

        if (!string.IsNullOrWhiteSpace(prefix))
            builder.AppendLine(prefix);

        builder.AppendLine(formattedJson);

        if (!string.IsNullOrWhiteSpace(suffix))
            builder.AppendLine(suffix);

        formatted = builder.ToString().TrimEnd();
        return true;
    }

    private static bool TryFormatJson(string text, out string formatted)
    {
        formatted = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(text);
            formatted = FormatJsonDocument(document.RootElement);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatJsonDocument(JsonElement root)
    {
        if (TryFormatWebhookMessage(root, out var webhookMessage))
            return webhookMessage;

        var readableObject = ToReadableObject(root, string.Empty);
        return JsonSerializer.Serialize(readableObject, ReadableJsonOptions);
    }

    private static bool TryFormatWebhookMessage(JsonElement root, out string formatted)
    {
        formatted = string.Empty;

        if (root.ValueKind != JsonValueKind.Object ||
            !TryGetStringProperty(root, "msgtype", out var messageType))
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"消息类型：{messageType}");

        if (messageType == "text" &&
            root.TryGetProperty("text", out var textElement) &&
            TryGetStringProperty(textElement, "content", out var content))
        {
            builder.AppendLine("文本内容：");
            builder.AppendLine(content);
        }
        else if (messageType == "image" &&
                 root.TryGetProperty("image", out var imageElement))
        {
            builder.AppendLine("图片内容：已省略 base64 数据。");
            if (TryGetStringProperty(imageElement, "md5", out var md5))
                builder.AppendLine($"图片 md5：{md5}");

            if (TryGetStringProperty(imageElement, "base64", out var base64))
                builder.AppendLine($"base64 长度：{base64.Length} 字符");
        }
        else
        {
            var readableObject = ToReadableObject(root, string.Empty);
            builder.AppendLine(JsonSerializer.Serialize(readableObject, ReadableJsonOptions));
        }

        formatted = builder.ToString().TrimEnd();
        return true;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static object? ToReadableObject(JsonElement element, string propertyName)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ToReadableDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(x => ToReadableObject(x, propertyName)).ToList(),
            JsonValueKind.String => ToReadableString(element, propertyName),
            JsonValueKind.Number => ToReadableNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static Dictionary<string, object?> ToReadableDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
            result[property.Name] = ToReadableObject(property.Value, property.Name);

        return result;
    }

    private static object ToReadableString(JsonElement element, string propertyName)
    {
        var value = element.GetString() ?? string.Empty;
        if (propertyName.Equals("base64", StringComparison.OrdinalIgnoreCase))
            return $"<已省略 base64，长度 {value.Length} 字符>";

        return value;
    }

    private static object ToReadableNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var intValue))
            return intValue;

        if (element.TryGetDouble(out var doubleValue))
            return doubleValue;

        return element.ToString();
    }
}
