using System;
using System.Net;
using ArmaBrowser.Logic;
using Magic.Steam;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ArmaBrowser.Design
{
    internal class ServerItem : IServerItem
    {
        public ServerItemGroup GroupName { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime? LastPlayed { get; set; }
        public string Endpoint { get; set; }
        public string Gamename { get; private set; }
        public string Mode { get; private set; }
        public string Name { get; set; }
        public string Mission { get; private set; }
        public IPAddress Host { get; private set; }
        public int Port { get; private set; }
        public bool Passworded { get; private set; }
        public string Version { get; private set; }
        public string ModsText { get; private set; }
        public string[] Mods { get; private set; }
        public string Modhashs { get; private set; }
        public int CurrentPlayerCount { get; private set; }
        public int MaxPlayers { get; set; }
        public bool IsPlayerSlotsFull { get; private set; }
        public string Island { get; private set; }
        public bool VerifySignatures { get; private set; }
        public string Signatures { get; private set; }
        public string FullText { get; private set; }
        public string CurrentPlayersText { get; private set; }
        public ISteamGameServerPlayer[] CurrentPlayers { get; private set; }
        public int QueryPort { get; private set; }
        public int Ping { get; private set; }
    }
}