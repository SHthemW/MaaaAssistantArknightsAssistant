using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game_Daily_Routine_Launcher;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _configPath;

    public ConfigService()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(appDir, "appsettings.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
            return CreateDefault();

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefault();
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    private AppConfig CreateDefault()
    {
        var config = new AppConfig
        {
            Tasks =
            [
                new GameTaskConfig
                {
                    Id = "maa",
                    Name = "明日方舟 (MAA)",
                    ToolPath = @"D:\Services\MAA-v5.16.5-win-x64\MAA.exe",
                    GameProcessName = "",
                    DelayBeforeStartMs = 0
                },
                new GameTaskConfig
                {
                    Id = "march7th",
                    Name = "崩坏：星穹铁道 (March7th)",
                    ToolPath = @"D:\Services\March7thAssistant_full\March7th Assistant.exe",
                    GameProcessName = "",
                    DelayBeforeStartMs = 30000
                },
                new GameTaskConfig
                {
                    Id = "zzz",
                    Name = "绝区零 (OneDragon)",
                    ToolPath = @"D:\Services\ZenlessZoneZero-OneDragon-v2.1.1-Full-Environment\program\OneDragon-Launcher.exe",
                    ToolArgs = "-o -c",
                    GameProcessName = "ZenlessZoneZero",
                    DelayBeforeStartMs = 0
                },
                new GameTaskConfig
                {
                    Id = "genshin",
                    Name = "原神 (BetterGI)",
                    ToolPath = "bettergi://startOneDragon",
                    LaunchMode = LaunchMode.Uri,
                    GameProcessName = "YuanShen",
                    DelayBeforeStartMs = 0
                },
                new GameTaskConfig
                {
                    Id = "maaend",
                    Name = "MaaEnd",
                    ToolPath = @"D:\Services\MaaEnd-win-x86_64-v2.4.0\MaaEnd.exe",
                    GameProcessName = "",
                    DelayBeforeStartMs = 0
                },
                new GameTaskConfig
                {
                    Id = "ww",
                    Name = "鸣潮 (ok-ww)",
                    ToolPath = @"D:\Services\ok-ww\ok-ww.exe",
                    ToolArgs = "-t 1 -e",
                    GameProcessName = "Wuthering Waves",
                    DelayBeforeStartMs = 0
                }
            ]
        };

        Save(config);
        return config;
    }
}
