using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArmaBrowser.Logic;

namespace ArmaBrowser.Views.Controls
{
    /// <summary>
    /// Interaktionslogik für AddonsControl.xaml
    /// </summary>
    partial class AddonsControl : UserControl
    {
        public AddonsControl()
        {
            InitializeComponent();
            AddonsListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayText", System.ComponentModel.ListSortDirection.Ascending));
            AddonsListBox.Items.IsLiveSorting = true;
            //AddonsListBox.Items.GroupDescriptions.Add(new PropertyGroupDescription("IsInstalled"));
            //AddonsListBox.Items.GroupDescriptions.Add(new PropertyGroupDescription("IsEasyInstallable"));
        }

        private void ListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                ((ListBox)sender).SelectedIndex = -1;
            }
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;

            var items = AddonsListBox.Items.Cast<IAddon>();
            foreach (var item in items)
            {
                if (item.CanActived && !item.IsActive)
                    item.IsActive = true;
            }
        }
 
    }
}
