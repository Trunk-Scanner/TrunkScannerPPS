using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TrunkScannerPPS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string appVersion => "R01.00.00";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SplashScreen splash = new SplashScreen();
            PPS main = new PPS();
            main.Hide();
            splash.Show();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                splash.Close();

                main.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                main.Show();
            };
            timer.Start();
        }
    }
}
