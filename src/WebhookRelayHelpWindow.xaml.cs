using System.Windows;

namespace Game_Daily_Routine_Launcher;

public partial class WebhookRelayHelpWindow : Window
{
    public WebhookRelayHelpWindow()
    {
        InitializeComponent();
        DataContext ??= new WebhookRelayHelpViewModel();
    }

    private async void OnCopyForwardUrlClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WebhookRelayHelpViewModel vm)
            return;

        var copied = await ClipboardService.TrySetTextAsync(vm.ForwardUrlExample, retryCount: 4, delayMs: 25);
        if (!copied)
            MessageBox.Show("复制失败，请稍后重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class WebhookRelayHelpViewModel
{
    public WebhookRelayHelpViewModel(int port = 5058)
    {
        ForwardUrlExample = $"http://127.0.0.1:{port}/cgi-bin/webhook/send?key=你的key";
    }

    public string ForwardUrlExample { get; }
}
