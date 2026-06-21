using System.Windows;

namespace Game_Daily_Routine_Launcher;

public partial class WebhookRelayHelpWindow : Window
{
    public WebhookRelayHelpWindow()
    {
        InitializeComponent();
        DataContext = new WebhookRelayHelpViewModel();
    }
}

public sealed class WebhookRelayHelpViewModel
{
    public string ForwardUrl { get; } = "http://127.0.0.1:5058/";
    public string ForwardBody { get; } = "{\"url\":\"https://example.com/webhook\",\"body\":{\"time\":\"__TIME__\",\"content\":\"__CONTENT__\"}}";
}
