using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaBrowser.Logic
{
    internal interface ILogicContext
    {
        ObservableCollection<IServerItem> ServerItems { get; }

        Collection<IAddon> Addons { get; }

        string ArmaPath { get; }

        //void TestJson();

        //event EventHandler<string> LiveAction;

        string ArmaVersion { get; }

        void LookForArmaPath();

        void ReloadServerItems(IPEndPoint[] lastAddresses, CancellationToken cancellationToken);

        void ReloadServerItem(IPEndPoint iPEndPoint, CancellationToken cancellationToken);

        void Open(IServerItem selectedServerItem, IAddon[] addon, bool runAsAdmin = false);

        void RefreshServerInfo(IServerItem[] items);

        Task RefreshServerInfoAsync(IServerItem[] items);
    }
}