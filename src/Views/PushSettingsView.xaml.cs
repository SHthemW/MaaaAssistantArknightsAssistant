using System.Windows;
using System.Windows.Controls;

namespace Game_Daily_Routine_Launcher.Views;

public partial class PushSettingsView : UserControl
{
    public PushSettingsView()
    {
        InitializeComponent();
    }

    private void OnWebhookRelayHelpClicked(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        var owner = Window.GetWindow(this);
        var helpWindow = new WebhookRelayHelpWindow
        {
            Owner = owner,
            DataContext = new WebhookRelayHelpViewModel(vm?.WebhookRelayPort ?? 5058, vm?.WebhookRelaySourceUrl)
        };

        helpWindow.ShowDialog();
    }
}
