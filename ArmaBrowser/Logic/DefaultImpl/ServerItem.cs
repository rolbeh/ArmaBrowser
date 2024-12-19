using System;
using System.Net;
using ArmaBrowser.Data;
using Magic.Steam;

namespace ArmaBrowser.Logic
{
    internal class ServerItem : ObjectNotify, IServerItem, IServerQueryAddress
    {
        private string _modsText;

        internal ServerItem()
        {
            this.Signatures = string.Empty;
        }

        public ServerItemGroup GroupName
        {
            get
            {
                if (this._isFavorite) return ServerItemGroup.Favorite;
                if (this.LastPlayed.HasValue) return ServerItemGroup.Recently;
                if (this.Port > 0) return ServerItemGroup.Found;
                return ServerItemGroup.NoResponse;
            }
            // ReSharper disable once ValueParameterNotUsed
            private set => this.OnPropertyChanged();
        }

        // ReSharper disable once UnusedMember.Global
        public string Endpoint => string.Format("{0}:{1}", this.Host, this.Port);

        public string PlayersState => string.Format("{0}/{1}", this._playersNum, this._maxPlayers);

        public bool IsFavorite
        {
            get => this._isFavorite;
            set
            {
                this._isFavorite = value;
                this.OnPropertyChanged();
                this.GroupName = ServerItemGroup.Favorite;
            }
        }

        public DateTime? LastPlayed
        {
            get => this._lastPlayed;
            set
            {
                this._lastPlayed = value;
                this.GroupName = ServerItemGroup.Recently;
                this.OnPropertyChanged();
            }
        }

        public string Gamename { get; internal set; }

        public string Mode
        {
            get => this._mode;
            set
            {
                this._mode = value;
                this.OnPropertyChanged();
            }
        }

        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                this.OnPropertyChanged();
            }
        }

        public string Mission
        {
            get => this._mission;
            set
            {
                this._mission = value;
                this.OnPropertyChanged();
            }
        }

        //public string Country
        //{
        //    get { return _country; }
        //    set
        //    {
        //        _country = value;
        //        if (_country != null) _country = _country.ToLower();
        //        _countryUrl = null;
        //    }
        //}

        //public string CountryUrl
        //{
        //    get { return _countryUrl ?? (_countryUrl = string.Format(imageUrl, _country ?? "--")); }
        //}

        public IPAddress Host { get; internal set; }

        public int Port { get; internal set; }

        public int QueryPort { get; internal set; }

        public string Version
        {
            get => this._version;
            set
            {
                if (this._version == value) return;
                this._version = value;
                this.OnPropertyChanged();
            }
        }

        //public string Version { get; internal set; }

        public string ModsText
        {
            get => this._modsText;
            internal set
            {
                this._modsText = value ?? string.Empty;
                this.Mods = this._modsText.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public string[] Mods { get; private set; }

        public string Modhashs { get; internal set; }

        public string Signatures
        {
            get => this._signatures;
            internal set => this._signatures = value ?? string.Empty;
        }

        public int CurrentPlayerCount
        {
            get => this._playersNum;
            set
            {
                this._playersNum = value;
                this.OnPropertyChanged("IsPlayerSlotsFull");
                this.OnPropertyChanged("PlayersState");
                this.OnPropertyChanged();
            }
        }

        public int MaxPlayers
        {
            get => this._maxPlayers;
            set
            {
                this._maxPlayers = value;
                this.OnPropertyChanged("IsPlayerSlotsFull");
                this.OnPropertyChanged("PlayersState");
                this.OnPropertyChanged();
            }
        }

        public ISteamGameServerPlayer[] CurrentPlayers
        {
            get => this._currentPlayers;
            internal set
            {
                if (this._currentPlayers == value) return;
                this._currentPlayers = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsPlayerSlotsFull => this._maxPlayers == this._playersNum;

        public string CurrentPlayersText
        {
            get => this._currentPlayersText;
            internal set
            {
                if (this._currentPlayersText == value) return;
                this._currentPlayersText = value;
                this.OnPropertyChanged();
            }
        }

        public string Island { get; internal set; }

        public string FullText => string.Format("{0} {1} {2}", this.Name, this.Mission, this.Island);

        public bool Passworded { get; set; }

        public int Ping
        {
            get => this._ping;
            internal set
            {
                if (this._ping == value) return;
                this._ping = value;
                this.OnPropertyChanged();
            }
        }


        public bool VerifySignatures { get; set; }
        //const string imageUrl = "http://arma3.swec.se/images/flags/png/{0}.png";

        #region Fields

        //private string _country = "--";
        //private string _countryUrl = string.Format(imageUrl, "--");

        private int _playersNum;
        private string _mission;
        private string _version;
        private string _currentPlayersText;
        private string _signatures;
        private int _maxPlayers;
        private string _name;
        private string _mode;
        private ISteamGameServerPlayer[] _currentPlayers;
        private int _ping;
        private DateTime? _lastPlayed;
        private bool _isFavorite;

        #endregion Field
    }
}