using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magic.Steam;
using Magic.Steam.Queries;

namespace QuerySteamGameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<GameServerQueryEndPoint> steamGameServerQueryEndPoints = ServerQueries.DiscoverQueryEndPoints(option =>
            {
                //option.Filter.GameDir = filter;
                option.Filter.Appid = "107410";
                //option.Filter.PasswordProtected = true;
            });
            var addressCount = 0;
            foreach (GameServerQueryEndPoint steamGameServerQueryEndPoint in steamGameServerQueryEndPoints)
            {
                addressCount++;
                Console.WriteLine(steamGameServerQueryEndPoint.ToString());
            }
            Console.WriteLine();
            Console.WriteLine($"address count: {addressCount}");
            Console.ReadLine();
        }
    }
}
