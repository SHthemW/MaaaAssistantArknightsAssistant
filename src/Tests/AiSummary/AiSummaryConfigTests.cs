using Game_Daily_Routine_Launcher;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher.Tests;

public sealed class AiSummaryConfigTests
{
    [Fact]
    public void RetryDelayDefaultsToThirtySeconds()
    {
        var common = new AiSummaryCommonConfig();

        Assert.Equal(30, common.RequestRetryDelaySeconds);
    }

    [Fact]
    public void LegacyCommonProxyIsCopiedToEachProvider()
    {
        var config = new AiSummaryConfig
        {
            LegacyProxy = new AiSummaryProxyConfig
            {
                Enabled = true,
                Url = "http://127.0.0.1:7890",
                Username = "proxy-user",
                Password = "proxy-password"
            }
        };

        Assert.True(config.MigrateLegacyCommonConfig());
        Assert.Null(config.LegacyProxy);
        AssertProxy(config.ZhipuAi.Proxy);
        AssertProxy(config.ChatGpt.Proxy);
        AssertProxy(config.DeepSeek.Proxy);
    }

    [Fact]
    public void LegacyCommonProxyDoesNotOverwriteProviderProxy()
    {
        var configuredProxy = new AiSummaryProxyConfig
        {
            Enabled = true,
            Url = "http://127.0.0.1:8080"
        };
        var config = new AiSummaryConfig
        {
            LegacyProxy = new AiSummaryProxyConfig
            {
                Enabled = true,
                Url = "http://127.0.0.1:7890"
            }
        };
        config.ChatGpt.Proxy = configuredProxy;

        Assert.True(config.MigrateLegacyCommonConfig());
        Assert.Same(configuredProxy, config.ChatGpt.Proxy);
        Assert.Equal("http://127.0.0.1:7890", config.ZhipuAi.Proxy.Url);
        Assert.Equal("http://127.0.0.1:7890", config.DeepSeek.Proxy.Url);
    }

    [Fact]
    public void ProviderProxySettingsAreIndependentByDefault()
    {
        var config = new AiSummaryConfig();
        config.ZhipuAi.Proxy.Enabled = true;

        Assert.True(config.ZhipuAi.Proxy.Enabled);
        Assert.False(config.ChatGpt.Proxy.Enabled);
        Assert.False(config.DeepSeek.Proxy.Enabled);
    }

    [Fact]
    public void LegacyProxyJsonIsMigratedToProviderConfig()
    {
        const string json = "{\"proxy\":{\"enabled\":true,\"url\":\"http://127.0.0.1:7890\"}}";
        var config = JsonSerializer.Deserialize<AiSummaryConfig>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(config);
        Assert.True(config!.MigrateLegacyCommonConfig());
        Assert.True(config.ChatGpt.Proxy.Enabled);
        Assert.Equal("http://127.0.0.1:7890", config.ChatGpt.Proxy.Url);
        Assert.Null(config.LegacyProxy);
    }

    private static void AssertProxy(AiSummaryProxyConfig proxy)
    {
        Assert.True(proxy.Enabled);
        Assert.Equal("http://127.0.0.1:7890", proxy.Url);
        Assert.Equal("proxy-user", proxy.Username);
        Assert.Equal("proxy-password", proxy.Password);
    }
}
