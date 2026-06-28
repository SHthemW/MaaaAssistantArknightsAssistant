using System.IO;

namespace Game_Daily_Routine_Launcher;

public static partial class LegacyBatchTaskImporter
{
    private static readonly string[] KnownBatchFileNames =
    [
        "launch.bat",
        "run_auto_zzz.bat",
        "run_bettergi.bat",
        "run_maaend.bat",
        "run_okww.bat",
        "monitor_zzz.bat",
        "monitor_genshin.bat",
        "monitor_ww.bat"
    ];

    public static bool TryApply(AppConfig config, string appDir)
    {
        var files = DiscoverBatchFiles(appDir);
        if (files.Count == 0)
            return false;

        var changed = false;
        changed |= ImportLaunchTasks(config, files);
        changed |= ImportScriptTask(config, files, "zzz", "绝区零 (OneDragon)", "run_auto_zzz.bat", "monitor_zzz.bat");
        changed |= ImportScriptTask(config, files, "genshin", "原神 (BetterGI)", "run_bettergi.bat", "monitor_genshin.bat");
        changed |= ImportScriptTask(config, files, "maaend", "MaaEnd", "run_maaend.bat", null);
        changed |= ImportScriptTask(config, files, "ww", "鸣潮 (ok-ww)", "run_okww.bat", "monitor_ww.bat");
        return changed;
    }

    private static bool ImportLaunchTasks(AppConfig config, IReadOnlyDictionary<string, string> files)
    {
        if (!files.TryGetValue("launch.bat", out var path))
            return false;

        var text = File.ReadAllText(path);
        var variables = ParseVariables(text);
        var commands = ParseStartCommands(text, variables);
        var changed = false;

        changed |= ImportPathByVariable(config, "maa", "明日方舟 (MAA)", variables, "E");
        changed |= ImportPathByVariable(config, "march7th", "崩坏：星穹铁道 (March7th)", variables, "D", 30000);
        changed |= ImportPathByKeyword(config, "maa", "明日方舟 (MAA)", commands, "maa");
        changed |= ImportPathByKeyword(config, "march7th", "崩坏：星穹铁道 (March7th)", commands, "march7th", 30000);
        return changed;
    }

    private static bool ImportPathByVariable(
        AppConfig config,
        string id,
        string name,
        IReadOnlyDictionary<string, string> variables,
        string variableName,
        int? delayBeforeStartMs = null)
    {
        if (!variables.TryGetValue(variableName, out var toolPath))
            return false;

        return ApplyTaskValues(config, id, name, toolPath, string.Empty, null, delayBeforeStartMs);
    }

    private static bool ImportPathByKeyword(
        AppConfig config,
        string id,
        string name,
        IReadOnlyList<StartCommand> commands,
        string keyword,
        int? delayBeforeStartMs = null)
    {
        var command = commands.FirstOrDefault(x => x.Target.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (command is null)
            return false;

        return ApplyTaskValues(config, id, name, command.Target, command.Arguments, null, delayBeforeStartMs);
    }

    private static bool ImportScriptTask(
        AppConfig config,
        IReadOnlyDictionary<string, string> files,
        string id,
        string name,
        string runFileName,
        string? monitorFileName)
    {
        if (!files.TryGetValue(runFileName, out var runPath))
            return false;

        var runText = File.ReadAllText(runPath);
        var command = ParseStartCommands(runText, ParseVariables(runText))
            .FirstOrDefault(x => !x.Target.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
        if (command is null)
            return false;

        string? processName = null;
        if (monitorFileName is not null && files.TryGetValue(monitorFileName, out var monitorPath))
            processName = ParseMonitorProcessName(File.ReadAllText(monitorPath));

        return ApplyTaskValues(config, id, name, command.Target, command.Arguments, processName, null);
    }

    private static bool ApplyTaskValues(
        AppConfig config,
        string id,
        string name,
        string toolPath,
        string toolArgs,
        string? processName,
        int? delayBeforeStartMs)
    {
        if (string.IsNullOrWhiteSpace(toolPath))
            return false;

        var task = FindOrAddTask(config, id, name);
        var changed = false;

        if (string.IsNullOrWhiteSpace(task.ToolPath))
        {
            task.ToolPath = toolPath;
            task.LaunchMode = IsUri(toolPath) ? LaunchMode.Uri : LaunchMode.File;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(task.ToolArgs) && !string.IsNullOrWhiteSpace(toolArgs))
        {
            task.ToolArgs = toolArgs;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(task.GameProcessName) && !string.IsNullOrWhiteSpace(processName))
        {
            task.GameProcessName = processName;
            changed = true;
        }

        if (delayBeforeStartMs is > 0 && task.DelayBeforeStartMs == 0)
        {
            task.DelayBeforeStartMs = delayBeforeStartMs.Value;
            changed = true;
        }

        return changed;
    }

    private static GameTaskConfig FindOrAddTask(AppConfig config, string id, string name)
    {
        var task = config.Tasks.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (task is not null)
            return task;

        task = new GameTaskConfig
        {
            Id = id,
            Name = name,
            Enabled = true,
            TimeoutMinutes = 60
        };
        config.Tasks.Add(task);
        return task;
    }

    private static bool IsUri(string value) =>
        !Path.IsPathRooted(value) &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        !string.IsNullOrWhiteSpace(uri.Scheme);

    private sealed record StartCommand(string Target, string Arguments);
}
