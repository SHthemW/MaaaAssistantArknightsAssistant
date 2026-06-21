using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;

namespace Game_Daily_Routine_Launcher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ((INotifyCollectionChanged)LogListBox.Items).CollectionChanged += (_, _) =>
        {
            if (LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        };

        Closing += (_, _) => (DataContext as MainViewModel)?.Cleanup();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(hwnd, 0x00000002);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info)) return;

        var source = PresentationSource.FromVisual(this);
        var scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        var scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        var monitorWidth = (info.rcWork.Right - info.rcWork.Left) * scaleX;
        var monitorHeight = (info.rcWork.Bottom - info.rcWork.Top) * scaleY;
        var monitorLeft = info.rcWork.Left * scaleX;
        var monitorTop = info.rcWork.Top * scaleY;

        var maxWidth = monitorWidth * 0.85;
        var maxHeight = monitorHeight * 0.90;

        if (Width > maxWidth) Width = maxWidth;
        if (Height > maxHeight) Height = maxHeight;

        Left = monitorLeft + (monitorWidth - Width) / 2;
        Top = monitorTop + (monitorHeight - Height) / 2;

        SizeToContent = SizeToContent.Manual;
    }

    private void OnWebhookRelayHelpClicked(object sender, RoutedEventArgs e)
    {
        var helpWindow = new WebhookRelayHelpWindow
        {
            Owner = this,
            DataContext = new WebhookRelayHelpViewModel((DataContext as MainViewModel)?.WebhookRelayPort ?? 5058)
        };
        helpWindow.ShowDialog();
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}

public class MuteTextConverter : IValueConverter
{
    public static readonly MuteTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "取消静音" : "静音";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
