using System.Net;

namespace ArmaBrowser.Data
{
    internal interface IServerQueryAddress
    {
        IPAddress Host { get; }
        int QueryPort { get; }
    }
}