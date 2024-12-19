using System.Collections.Generic;
using System.Linq;
using System.Net;
using Magic.Steam;

namespace ArmaBrowser.Data.DefaultImpl
{
    internal sealed class Arma3ServerRepositorySteam : IServerRepository
    {
        private const string SteamGameNameFilter = "Arma3";

        #region IServerRepository

        public IEnumerable<ISteamGameServer> DiscoverServer()
        {
            // https://developer.valvesoftware.com/wiki/Master_Server_Query_Protocol

            return ServerQueries.DiscoverQueryEndPoints(option => option.Filter.GameDir = SteamGameNameFilter)
                .Select(ep => (ISteamGameServer) new ServerItem {Host = ep.Host, QueryPort = ep.QueryPort});
        }

        public ISteamGameServer GetServerInfo(IPEndPoint gameServerQueryEndpoint)
        {
            return ServerQueries.GetServerInfo(gameServerQueryEndpoint);
        }

        #endregion IServerRepository
    }
}