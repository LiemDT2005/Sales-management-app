using System.Configuration;
using System.Data;
using System.Windows;

namespace SMA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            var loginWindow = new Views.Auth.Login();
            loginWindow.Show();
        }
    }
}
