using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Serialization;
using ArmaBrowser.Logic;
using ArmaBrowser.Properties;

namespace ArmaBrowser
{
    using WinInterop = System.Windows.Interop;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal sealed partial class MainWindow
    {
        private CancellationTokenSource _joinServerCancellationTokenSrc;

        public MainWindow()
        {
            this._joinServerCancellationTokenSrc = new CancellationTokenSource();
            this.SourceInitialized += this.win_SourceInitialized;

            this.IsVisibleChanged += this.MainWindow_IsVisibleChanged;
            if (Settings.Default.MainWindowState > -1)
            {
                this.Height = Settings.Default.MainWindowHeight;
                this.Width = Settings.Default.MainWindowWidth;
                this.Top = Settings.Default.MainWindowTop;
                this.Left = Settings.Default.MainWindowLeft;
            }

            this.InitializeComponent();

            //Keyboard.AddGotKeyboardFocusHandler(this, GlobalKeyborad_OnGotKeyboard);
            //Keyboard.AddLostKeyboardFocusHandler(this, GlobalKeyborad_OnGotKeyboard);

            this.AutoJoinView.Visibility = Visibility.Collapsed;
            this.TabListBox.SelectedIndex = 0;
            this.Test.Freeze();
        }

        //private void GlobalKeyborad_OnGotKeyboard(object sender, KeyboardFocusChangedEventArgs args)
        //{
        //    Debug.WriteLine("{0} - {1}  --> {2} - {3}",
        //            args.OldFocus,
        //            args.OldFocus != null ? ((FrameworkElement)args.OldFocus).Name : "", 
        //            args.NewFocus, 
        //            args.NewFocus != null ? ((FrameworkElement)args.NewFocus).Name : ""
        //        );

        //}

        private void win_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WinInterop.WindowInteropHelper(this).Handle;
            WinInterop.HwndSource hwndSource = WinInterop.HwndSource.FromHwnd(handle);
            hwndSource?.AddHook(Win.WindowProc);
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!this.IsInitialized && e.NewValue.Equals(true))
            {
                return;
            }

            this.IsVisibleChanged -= this.MainWindow_IsVisibleChanged;
            if (Settings.Default.MainWindowState > -1)
            {
                //Task.Delay(300).ContinueWith(t =>
                //{
                this.WindowState = (WindowState) Settings.Default.MainWindowState;
                //}, TaskScheduler.FromCurrentSynchronizationContext());                
            }
        }

        private void OpenCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =
                (this.MyViewModel.SelectedServerItem != null && this.MyViewModel.SelectedServerItem.Port > 0 ||
                 this.MyViewModel.LaunchWithoutHost)
                && Directory.Exists(Settings.Default.ArmaPath);
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.JoinServer();
        }

        private void CancelJoiningServer()
        {
            CancellationTokenSource old = this._joinServerCancellationTokenSrc;
            this._joinServerCancellationTokenSrc = new CancellationTokenSource();
            old.Cancel();
            old.Dispose();
        }

        private async void JoinServer()
        {
            this.AutoJoinView.Visibility = Visibility.Visible;
            try
            {
                this.AutoJoinView.DataContext = this.MyViewModel.SelectedServerItem;
                this.CancelJoiningServer();
                bool canJoinServer = await this.WaitForSlotAsync(this._joinServerCancellationTokenSrc.Token);

                if (canJoinServer)
                {
                    this.MyViewModel.OpenArma();
                    this.WindowState = WindowState.Minimized;
                }
            }
            finally
            {
                this.AutoJoinView.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<bool> WaitForSlotAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                while (this.MyViewModel.SelectedServerItem != null
                       && this.MyViewModel.SelectedServerItem.IsPlayerSlotsFull)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }

                    Thread.Sleep(1000);
                }

                return true;
            }, token);
        }

        private void PoweredByHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void ServerListControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Task.Delay(500).ContinueWith(t => MyViewModel.Reload());
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(HostConfigCollection));
            using (TextWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, HostConfigCollection.Default);
                Settings.Default.HostConfigs = textWriter.ToString();
                Settings.Default.Save();
            }

            try
            {
                this.MyViewModel.StopAll();
            }
            catch
            {
                // ignore
            }
        }

        private void CvsSelectedAddons_OnFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is IAddon item)
            {
                e.Accepted = item.IsActive;
            }
        }

        private void TextBlock_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Directory.Exists(Settings.Default.ArmaPath))
            {
                Process.Start(Settings.Default.ArmaPath);
            }
            else
            {
                MessageBox.Show("Arma 3 installation not found");
            }
        }

        private void AutoJoinControl_Canceled(object sender, RoutedEventArgs e)
        {
            this.CancelJoiningServer();
        }

        private void PreferencesContextMenuButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button) sender;
            this.PreferencesContextMenu.PlacementTarget = btn;
            this.PreferencesContextMenu.Placement = PlacementMode.Bottom;
            ContextMenuService.SetPlacement(btn, PlacementMode.Bottom);

            this.PreferencesContextMenu.IsOpen = true;
        }
    }
}