using System.Collections.Generic;

namespace Magic.Steam.Queries
{
    public class SteamGameServer : ISteamGameServer
    {
        /// <summary>
        /// Arma3
        /// </summary>
        public string Gamename { get; set; }

        /// <summary>
        /// Name der Mission
        /// </summary>
        public string Mission { get; set; }

        /// <summary>
        /// Länder Code z.B.'DE'
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// IP - Adresse
        /// </summary>
        public System.Net.IPAddress Host { get; set; }

        /// <summary>
        /// GamePort
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// QueryPort für Serverstates
        /// </summary>
        public int QueryPort { get; set; }

        /// <summary>
        /// Version des Servers
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Name des GameServer
        /// </summary>
        public string Name { get; set; }

        public string Mods { get; set; }

        public string Modhashs { get; set; }

        /// <summary>
        /// Anzahl der aktuellen Spieler
        /// </summary>
        public int CurrentPlayerCount { get; set; }

        /// <summary>
        /// maximale Spieleranzahl
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Karte
        /// </summary>
        public string Map { get; set; }

        public string Signatures { get; set; }

        public string Mode { get; set; }

        public bool Passworded { get; set; }

        public IEnumerable<string> CurrentPlayers { get; set; }

        public int Ping { get; set; }

        public IEnumerable<ISteamGameServerPlayer> Players { get; set; }

        public string Keywords { get; set; }

        public bool VerifySignatures { get; set; }
    }
}