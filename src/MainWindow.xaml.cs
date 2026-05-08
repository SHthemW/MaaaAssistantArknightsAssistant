using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
