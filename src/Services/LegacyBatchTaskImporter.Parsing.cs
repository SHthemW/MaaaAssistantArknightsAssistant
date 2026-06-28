using System.IO;
using System.Text.RegularExpressions;

namespace Game_Daily_Routine_Launcher;

public static partial class LegacyBatchTaskImporter
{
    private static Dictionary<string, string> DiscoverBatchFiles(string appDir)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var roots = new[]
        {
            appDir,
            Path.Combine(appDir, "batch"),
            Path.Combine(appDir, "src", "batch"),
            currentDir,
            Path.Combine(currentDir, "batch"),
            Path.Combine(currentDir, "src", "batch")
        };

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var fileName in KnownBatchFileNames)
            {
                var path = Path.Combine(root, fileName);
                if (File.Exists(path) && !files.ContainsKey(fileName))
                    files[fileName] = path;
            }
        }

        return files;
    }

    private static Dictionary<string, string> ParseVariables(string text)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in SetVariableRegex().Matches(text))
            variables[match.Groups["name"].Value] = match.Groups["value"].Value.Trim();

        return variables;
    }

    private static IReadOnlyList<StartCommand> ParseStartCommands(
        string text,
        IReadOnlyDictionary<string, string> variables)
    {
        var commands = new List<StartCommand>();
        foreach (Match match in StartCommandRegex().Matches(text))
        {
            var rest = match.Groups["rest"].Value.Trim();
            var titleOrTarget = ReadBatchToken(ref rest);
            var target = string.IsNullOrEmpty(titleOrTarget) ? ReadBatchToken(ref rest) : titleOrTarget;
            target = ResolveBatchVariable(target, variables);

            if (!string.IsNullOrWhiteSpace(target))
                commands.Add(new StartCommand(target, rest.Trim()));
        }

        return commands;
    }

    private static string? ParseMonitorProcessName(string text)
    {
        var variables = ParseVariables(text);
        return variables.TryGetValue("TARGET_EXE", out var targetExe)
            ? Path.GetFileNameWithoutExtension(targetExe)
            : null;
    }

    private static string ReadBatchToken(ref string value)
    {
        value = value.TrimStart();
        if (value.Length == 0)
            return string.Empty;

        if (value[0] != '"')
            return ReadUnquotedBatchToken(ref value);

        var endQuote = value.IndexOf('"', 1);
        if (endQuote < 0)
        {
            var token = value[1..];
            value = string.Empty;
            return token;
        }

        var quoted = value[1..endQuote];
        value = value[(endQuote + 1)..];
        return quoted;
    }

    private static string ReadUnquotedBatchToken(ref string value)
    {
        var firstSpace = value.IndexOf(' ');
        if (firstSpace < 0)
        {
            var token = value;
            value = string.Empty;
            return token;
        }

        var result = value[..firstSpace];
        value = value[(firstSpace + 1)..];
        return result;
    }

    private static string ResolveBatchVariable(string value, IReadOnlyDictionary<string, string> variables)
    {
        var match = BatchVariableRegex().Match(value);
        return match.Success && variables.TryGetValue(match.Groups["name"].Value, out var resolved)
            ? resolved
            : value;
    }

    [GeneratedRegex(@"^\s*set\s+""(?<name>[^=]+)=(?<value>.*)""\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SetVariableRegex();

    [GeneratedRegex(@"^\s*start\s+(?<rest>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex StartCommandRegex();

    [GeneratedRegex(@"^[%!](?<name>[^%!]+)[%!]$")]
    private static partial Regex BatchVariableRegex();
}
