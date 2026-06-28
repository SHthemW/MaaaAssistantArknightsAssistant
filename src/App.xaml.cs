using System.Windows;

namespace Game_Daily_Routine_Launcher;

public partial class App : Application
{
    public static bool IsAutoRun { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RuntimeLogService.Initialize();
        RuntimeLogService.WriteMessage($"启动参数：{string.Join(' ', e.Args)}");

        DispatcherUnhandledException += (_, args) =>
        {
            RuntimeLogService.WriteException("发生未处理的异常", args.Exception);
            MessageBox.Show($"发生未处理的异常:\n{args.Exception.Message}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        IsAutoRun = e.Args.Contains("--autorun", StringComparer.OrdinalIgnoreCase);

        if (ShouldExitSilentlyForAutoRun())
        {
            RuntimeLogService.WriteMessage("自动运行启动，但当前不在允许的定时启动窗口，程序静默退出。");
            Shutdown();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        RuntimeLogService.WriteMessage($"程序退出，退出码：{e.ApplicationExitCode}。");
        base.OnExit(e);
    }

    private static bool ShouldExitSilentlyForAutoRun()
    {
        if (!IsAutoRun)
            return false;

        var config = new ConfigService().Load();
        var now = TimeOnly.FromDateTime(DateTime.Now);
        return !config.IsInScheduledTimeRange(now);
    }
}
