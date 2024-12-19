using System.Collections.Generic;
using Magic.Steam;

namespace ArmaBrowser.Data
{
    internal interface IServerRepository
    {
        IEnumerable<ISteamGameServer> DiscoverServer();
    }
}