using System;
using System.Windows;
using System.Diagnostics;
using ArmaBrowser.Logic;
using ArmaBrowser.ViewModel;


namespace ArmaBrowser
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    partial class App
    {
        public App()
        {
            if (UpdateAvailableViewModel.ExistNewUpdate())
            {
                UpdateAvailableViewModel.RunUpdate();
            }

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            if (string.IsNullOrEmpty(ArmaBrowser.Properties.Settings.Default.Id)
                || ArmaBrowser.Properties.Settings.Default.Id.Length != 32)
            {
                ArmaBrowser.Properties.Settings.Default.Id = Guid.NewGuid().ToByteArray().ToHexString();
                ArmaBrowser.Properties.Settings.Default.Save();

            }

            if (!ArmaBrowser.Properties.Settings.Default.Upgraded)
            {
                ArmaBrowser.Properties.Settings.Default.Upgrade();
                ArmaBrowser.Properties.Settings.Default.Upgraded = true;
                ArmaBrowser.Properties.Settings.Default.Save();
            }
        }


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ServiceHub.Instance.Set(new AppPathService());
            ServiceHub.Instance.Set(new FavoriteService());

            MainWindow = new MainWindow();
            MainWindow.Show();

        }

        
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine(e.Exception);
            try
            {
                var exception = e.Exception as AggregateException;
                if (exception != null)
                {
                    var i = 0;
                    foreach (var item in exception.InnerExceptions)
                    {
                        i++;
                        Debug.WriteLine(i.ToString() + ". Exception: ");

                        Debug.WriteLine(item);
                        Debug.WriteLine("----------------------------");
                    }
                }
            }
            catch 
            {
                // ignore
            }
            
        }
    }
}
