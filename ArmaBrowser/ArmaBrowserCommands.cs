using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArmaBrowser.Logic;
using ArmaBrowser.ViewModel;
using ArmaBrowser.Views.Controls;

namespace ArmaBrowser
{
    internal static class ArmaBrowserCommands
    {
        public static ICommand RefreshAddons { get; }
        public static ICommand ReloadServerList { get; }
        public static ICommand MarkAsFavorite { get; }

        public static ICommand OpenAddonFolder { get; }

        public static ICommand UploadAddon { get; }
        public static ICommand EasyInstallAddon { get; }

        public static ICommand StopReloadServerList { get; }

        static ArmaBrowserCommands()
        {

            RefreshAddons = new RoutedCommand("RefreshAddons", typeof(ArmaBrowserCommands));
            ReloadServerList = new RoutedUICommand("Refresh list", "ReloadServerList", typeof(ArmaBrowserCommands));
            StopReloadServerList = new RoutedUICommand("Stop refresh", "StopReloadServerList", typeof(ArmaBrowserCommands));

            MarkAsFavorite = new RoutedUICommand("Favorite", "MarkAsFavorite", typeof(ArmaBrowserCommands));
            OpenAddonFolder = new RoutedUICommand("Open", "OpenAddonFolder", typeof(ArmaBrowserCommands));

            UploadAddon = new RoutedUICommand("Upload", "UploadAddon", typeof(ArmaBrowserCommands));
            EasyInstallAddon = new RoutedUICommand("Install", "EasyInstallAddon", typeof(ArmaBrowserCommands));


            CommandManager.RegisterClassCommandBinding(typeof(MainWindow), new CommandBinding(ReloadServerList, ReloadServerList_OnExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(MainWindow), new CommandBinding(StopReloadServerList, StopReloadServerList_OnExecuted));

            CommandManager.RegisterClassCommandBinding(typeof(UIElement), new CommandBinding(MarkAsFavorite, MarkAsFavorite_OnExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(FrameworkElement), new CommandBinding(RefreshAddons, RefreshAddonsCommand_OnExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(FrameworkElement), new CommandBinding(OpenAddonFolder, OpenAddonFolder_OnExecuted));

            CommandManager.RegisterClassCommandBinding(typeof(Button), new CommandBinding(UploadAddon, UploadAddon_OnExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(AddonsControl), new CommandBinding(EasyInstallAddon, EasyInstallAddon_OnExecuted, EasyInstallAddon_CanExecute));
            
        }

        private static void StopReloadServerList_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is MainWindow mainWnd)
            {
                mainWnd.MyViewModel.StopAll();
            }
        }

        private static void ReloadServerList_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is MainWindow mainWnd)
            {
                mainWnd.MyViewModel.ReloadServerList();
            }
        }

        private static void EasyInstallAddon_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is IAddon addon && addon.IsEasyInstallable.HasValue && addon.IsEasyInstallable.Value &&
                            addon.KeyNames.Any() && addon.KeyNames.Any(k => !string.IsNullOrEmpty(k.Hash));
        }

        private static void EasyInstallAddon_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is IAddon addon)
            {
                if (sender is FrameworkElement element && element.DataContext is ServerListViewModel model)
                {
                    model.DownloadAddon(addon);
                }
            }
        }

        private static void UploadAddon_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is IAddon addon)
            {
                //if (PathHelper.CalculateFolderSize(addon.Path) < (1024*1024*20))
                {
                    LogicContext.UploadAddonAsync(addon).Wait(0);
                }
                //else
                //{
                //    MessageBox.Show(Application.Current.MainWindow, "At present, only addons with an installation size of 20 MB are allowed to upload.",
                //        "Addon is too big. ", MessageBoxButton.OK, MessageBoxImage.Stop);
                //}
            }
        }

        private static void OpenAddonFolder_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is string)
            {
                if (System.IO.Directory.Exists(e.Parameter.ToString()))
                {
                    System.Diagnostics.Process.Start(e.Parameter.ToString());
                }
                else
                {
                    MessageBox.Show("Folder not found");
                }
            }
        }

        private static void RefreshAddonsCommand_OnExecuted(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            ServerListViewModel dc = ((FrameworkElement) sender).DataContext as ViewModel.ServerListViewModel;
            dc?.RefreshAddons();

        }

        private static void MarkAsFavorite_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ViewModel.ServerListViewModel dataContext )
            {
                IServerItem item = dataContext.SelectedServerItem;
                if (item != null)
                {
                    item.IsFavorite = !item.IsFavorite;
                    dataContext.SaveFavorits(item);
                }
            }
        }
    }
}
