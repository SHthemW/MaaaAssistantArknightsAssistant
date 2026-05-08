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

        var workTop = info.rcWork.Top * scaleY;
        var workBottom = info.rcWork.Bottom * scaleY;
        var workLeft = info.rcWork.Left * scaleX;
        var workRight = info.rcWork.Right * scaleX;

        if (Top < workTop)
            Top = workTop;
        if (Top > workBottom - 40)
            Top = workBottom - 40;
        if (Left < workLeft - Width + 100)
            Left = workLeft;
        if (Left > workRight - 100)
            Left = workRight - 100;
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
