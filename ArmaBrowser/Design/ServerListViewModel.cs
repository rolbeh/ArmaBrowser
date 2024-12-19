using System.Collections.Generic;
using ArmaBrowser.Logic;

namespace ArmaBrowser.Design
{
    internal abstract class DesignServerListViewModel
    {
        protected DesignServerListViewModel()
        {
            ServerItemsView = new[]
            {
                new ServerItem
                {
                    Name = "Recently",
                    GroupName = ServerItemGroup.Recently
                },
                new ServerItem
                {
                    Name = "Favorite",
                    GroupName = ServerItemGroup.Favorite
                },
                new ServerItem
                {
                    Name = "Found",
                    GroupName = ServerItemGroup.Found
                }
            };
        }

        public IEnumerable<ServerItem> ServerItemsView { get; }

        public ServerItem SelectedServerItem { get; set; }
    }
}