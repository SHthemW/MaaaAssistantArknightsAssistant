using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
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

        await CopyToClipboardAsync(vm.ForwardUrl);
    }

    private async void OnCopyForwardBodyClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WebhookRelayHelpViewModel vm)
            return;

        await CopyToClipboardAsync(vm.ForwardBody);
    }

    private static async Task CopyToClipboardAsync(string text)
    {
        var copied = await ClipboardService.TrySetTextAsync(text, retryCount: 4, delayMs: 25);

        if (!copied)
            MessageBox.Show("复制失败，请稍后重试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class WebhookRelayHelpViewModel : INotifyPropertyChanged
{
    private readonly int _port;
    private string _sourceUrl = string.Empty;
    private string _sourceBody = string.Empty;
    private string _forwardUrl = string.Empty;
    private string _forwardBody = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SourceUrl
    {
        get => _sourceUrl;
        set
        {
            if (_sourceUrl == value)
                return;

            _sourceUrl = value;
            OnPropertyChanged();
            RefreshPreview();
        }
    }

    public string SourceBody
    {
        get => _sourceBody;
        set
        {
            if (_sourceBody == value)
                return;

            _sourceBody = value;
            OnPropertyChanged();
            RefreshPreview();
        }
    }

    public string ForwardUrl
    {
        get => _forwardUrl;
        private set
        {
            if (_forwardUrl == value)
                return;

            _forwardUrl = value;
            OnPropertyChanged();
        }
    }

    public string ForwardBody
    {
        get => _forwardBody;
        private set
        {
            if (_forwardBody == value)
                return;

            _forwardBody = value;
            OnPropertyChanged();
        }
    }

    public WebhookRelayHelpViewModel(int port = 5058, string? sourceUrl = null, string? sourceBody = null)
    {
        _port = port;
        SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? "https://example.com/webhook" : sourceUrl;
        SourceBody = string.IsNullOrWhiteSpace(sourceBody)
            ? """
{
  "time": "__TIME__",
  "content": "__CONTENT__"
}
"""
            : sourceBody;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        ForwardUrl = BuildForwardUrl(SourceUrl, _port);
        ForwardBody = BuildRelayBodyPreview(SourceUrl, SourceBody);
    }

    private static string BuildForwardUrl(string sourceUrl, int port)
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
            uri.Host.Equals("qyapi.weixin.qq.com", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.Equals("/cgi-bin/webhook/send", StringComparison.OrdinalIgnoreCase))
            return $"http://127.0.0.1:{port}{uri.PathAndQuery}";

        return $"http://127.0.0.1:{port}/";
    }

    private static string BuildRelayBodyPreview(string url, string body)
    {
        object? bodyValue = body;

        try
        {
            using var document = JsonDocument.Parse(body);
            bodyValue = document.RootElement.Clone();
        }
        catch
        {
            // Keep the original text when it is not valid JSON, so the preview
            // still matches what the relay service can receive.
        }

        var payload = new Dictionary<string, object?>
        {
            ["url"] = url,
            ["body"] = bodyValue
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
