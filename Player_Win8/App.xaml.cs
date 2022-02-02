using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace JAudioPlayer
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            OperatingSystem os = Environment.OSVersion;

            if (os.Version.Major < 6 || (os.Version.MajorRevision == 6 && os.Version.Minor < 2))
            {
                MessageBox.Show("This program can only be run on Wndows 8. Use the other version instead.", "OS version", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }

            this.MainWindow = new MainWindow();
            this.MainWindow.Show();
        }
    }
}
