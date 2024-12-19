using System.Net;

namespace Magic.Steam.Queries
{
    public class GameServerQueryEndPoint
    {
        public IPAddress Host { get; set; }
        public int QueryPort { get; internal set; }

        #region Overrides of Object

        public override string ToString()
        {
            return $"{Host}:{QueryPort}";
        }

        #endregion
    }
}