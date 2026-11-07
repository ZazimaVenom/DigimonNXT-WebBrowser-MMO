using System.Windows;
namespace DigimonNXT.Client;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => {
            ClientLog.Write(args.Exception);
            MessageBox.Show("The client could not complete that action. Your server progress is safe. Restart the client and try again.\n\nDetails: " + args.Exception.Message, "Digimon NXT", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }
}
