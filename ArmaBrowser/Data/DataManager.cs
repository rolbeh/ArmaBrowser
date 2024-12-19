using ArmaBrowser.Data.DefaultImpl;

namespace ArmaBrowser.Data
{
    internal static class DataManager
    {
        public static IArma3DataRepository CreateNewDataRepository()
        {
            return new DataRepository();
        }

        public static Arma3ServerRepositorySteam CreateNewServerRepository()
        {
            return new Arma3ServerRepositorySteam();
        }
    }
}


//http://api.steampowered.com/ISteamNews/GetNewsForApp/V0001/?format=json&appid=107410&count=3
//http://api.steampowered.com/ISteamWebAPIUtil/GetSupportedAPIList/v0001/?format=xml
//http://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v0001/?format=json&appid=107410