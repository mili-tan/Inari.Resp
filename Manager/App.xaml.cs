using System.Linq;
using System.Windows;

namespace Inari.Resp.Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            MessageBox.Show(e.Args.FirstOrDefault());
        }
    }
}
