using System;
using System.Net;
using Magic.Steam;

namespace ArmaBrowser.Logic
{
    internal interface IServerItem
    {
        bool IsFavorite { get; set; }

        DateTime? LastPlayed { get; set; }

        /// <summary>
        /// Get the endpoint to connect the server for gaming.
        /// </summary>
        string Endpoint { get; }

        /// <summary>
        ///     Arma3
        /// </summary>
        string Gamename { get; }

        string Mode { get; }

        /// <summary>
        ///     Name des GameServer
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Name der Mission
        /// </summary>
        string Mission { get; }

        /// <summary>
        ///     IP - Adresse
        /// </summary>
        IPAddress Host { get; }

        /// <summary>
        ///     GamePort
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Passwortgeschützt
        /// </summary>
        bool Passworded { get; }

        /// <summary>
        /// </summary>
        string Version { get; }

        /// <summary>
        /// </summary>
        string ModsText { get; }

        /// <summary>
        /// </summary>
        string[] Mods { get; }

        /// <summary>
        /// </summary>
        string Modhashs { get; }

        /// <summary>
        ///     Anzahl der maximalen Spieler
        /// </summary>
        int CurrentPlayerCount { get; }

        /// <summary>
        ///     maximale Spieleranzahl
        /// </summary>
        int MaxPlayers { get; set; }

        /// <summary>
        /// </summary>
        bool IsPlayerSlotsFull { get; }

        /// <summary>
        ///     Karte
        /// </summary>
        string Island { get; }

        bool VerifySignatures { get; }

        string Signatures { get; }

        string FullText { get; }

        /// <summary>
        ///     Name der aktuellen Spieler
        /// </summary>
        string CurrentPlayersText { get; }

        ISteamGameServerPlayer[] CurrentPlayers { get; }

        /// <summary>
        ///     QueryPort für Serverstates
        /// </summary>
        int QueryPort { get; }

        int Ping { get; }
    }
}