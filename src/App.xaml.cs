using System.Windows;

namespace Game_Daily_Routine_Launcher;

public partial class App : Application
{
    public static bool IsAutoRun { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"发生未处理的异常:\n{args.Exception.Message}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        IsAutoRun = e.Args.Contains("--autorun", StringComparer.OrdinalIgnoreCase);

        if (IsAutoRun)
        {
            var config = new ConfigService().Load();
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (!config.IsInScheduledTimeRange(now))
            {
                Shutdown();
                return;
            }
        }
    }
}
