namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private async void RefreshWebhookRelayState()
    {
        if (!WebhookRelayEnabled)
        {
            WebhookRelayIsRunning = false;
            WebhookRelayStatusMessage = "未启用。";
            await _webhookRelayService.StopAsync();
            return;
        }

        var result = await _webhookRelayService.StartAsync(WebhookRelayPort, WebhookRelaySourceUrl, (message, rawBody) =>
        {
            AddLog(message, rawBody);
            return Task.CompletedTask;
        });
        WebhookRelayIsRunning = result.Success;
        WebhookRelayStatusMessage = result.Message;
    }

    partial void OnWebhookRelayEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        RefreshWebhookRelayState();
    }

    partial void OnWebhookRelayPortChanged(int value)
    {
        if (_isLoading || !WebhookRelayEnabled)
            return;

        RefreshWebhookRelayState();
    }

    partial void OnWebhookRelaySourceUrlChanged(string value)
    {
        if (_isLoading || !WebhookRelayEnabled)
            return;

        RefreshWebhookRelayState();
    }
}
