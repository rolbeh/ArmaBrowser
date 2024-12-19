using ArmaBrowser.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ArmaBrowser.ViewModel
{
    class ServerListViewModel : ObjectNotify
    {
        #region Fields

        private readonly LogicContext _context;
        private ListCollectionView _serverItemsView;
        private string _textFilter = string.Empty;
        private System.Net.IPEndPoint _ipEndPointFilter;
        private IServerItem _selectedServerItem;
        private ICollection<LoadingServerListContext> _reloadContexts; 
        private IAddon _selectedAddon;
        private long _loadingBusy;
        private string _selectedEndPoint;
        private bool _runAsAdmin;
        private bool _launchWithoutHost;
        private System.Net.IPEndPoint[] _lastItems = new System.Net.IPEndPoint[0];

        private CancellationTokenSource _reloadingCts = new CancellationTokenSource();
        
        private bool _isJoining;
        private readonly string _version;
        private int _totalPlayerCount;

        #endregion Fields

        #region Properties
        
        public UpdateAvailableViewModel UpdateAvailable { get; }

        public Collection<IServerItem> ServerItems
        {
            get { return this._context.ServerItems; }
        }

        public ICollection<LoadingServerListContext> ReloadContexts
        {
            get
            {
                return this._reloadContexts ?? (this._reloadContexts = new ReadOnlyObservableCollection<LoadingServerListContext>(this._context.ReloadThreads));
            }
        }

        public Collection<IAddon> Addons
        {
            get { return this._context.Addons; }
        }
        
        public ListCollectionView ServerItemsView
        {
            get { return this._serverItemsView ?? this.CreateServerItemsView(); }
        }

        private ListCollectionView CreateServerItemsView()
        {
            this._serverItemsView = new ListCollectionView(this.ServerItems) { Filter = this.OnServerItemsFilter };

            this._serverItemsView.IsLiveSorting = true;

            // Sorting
            this._serverItemsView.SortDescriptions.Add(new SortDescription { PropertyName = "GroupName", Direction = ListSortDirection.Ascending });
            this._serverItemsView.SortDescriptions.Add(new SortDescription { PropertyName = "CurrentPlayerCount", Direction = ListSortDirection.Descending });

            // Grouping           
            this._serverItemsView.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));

            return this._serverItemsView;
        }

        public string TextFilter
        {
            get { return this._textFilter; }
            set
            {
                if (this._textFilter == value) return;
                this._textFilter = value;

                this._ipEndPointFilter = null;

                if (!string.IsNullOrEmpty(this._textFilter))
                {
                    this._textFilter = this._textFilter.Trim();
                    var doppelpunktpos = this._textFilter.IndexOf(':');
                    if (doppelpunktpos > 6 && (this._textFilter.Length > doppelpunktpos + 4))
                    {
                        var ipstr = this._textFilter.Substring(0, doppelpunktpos);
                        var portstr = this._textFilter.Substring(doppelpunktpos + 1, this._textFilter.Length - doppelpunktpos - 1);
                        System.Net.IPAddress ip;
                        int port;
                        if (System.Net.IPAddress.TryParse(ipstr, out ip)
                                && Int32.TryParse(portstr, out port)
                                && port > System.Net.IPEndPoint.MinPort
                                && port < System.Net.IPEndPoint.MaxPort)
                        {
                            this._ipEndPointFilter = new System.Net.IPEndPoint(ip, port);
                        }
                    }
                }

                this.OnPropertyChanged();

                Properties.Settings.Default.TextFilter = this._textFilter;
                this.ServerItemsView.Refresh();
                this.RememberLastVisiblyItems();
            }
        }

        private void RememberLastVisiblyItems()
        {
            this._lastItems = this.ServerItemsView.Cast<IServerItem>().Take(50).Select(i => { return new System.Net.IPEndPoint(i.Host, i.QueryPort); }).ToArray();

        }

        public IAddon SelectedAddon
        {
            get { return this._selectedAddon; }
            set
            {
                if (this._selectedAddon == value) return;
                this._selectedAddon = value;
                this.OnPropertyChanged();
                //ServerItemsView.Refresh();
            }
        }

        public IServerItem SelectedServerItem
        {
            get { return this._selectedServerItem; }
            set
            {
                if (this._selectedServerItem == value) return;
                this._selectedServerItem = value;
                this._selectedEndPoint = this._selectedServerItem == null ? string.Empty : string.Format("{0}:{1}", this._selectedServerItem.Host, this._selectedServerItem.Port);

                this.RefreshServerInfoAsync(new[] {this._selectedServerItem });

                this.RefreshUsedAddons();
                this.OnPropertyChanged();

            }
        }

        public bool RunAsAdmin
        {
            get { return this._runAsAdmin; }
            set
            {
                this._runAsAdmin = value;
                this.OnPropertyChanged();
            }
        }

        #endregion Properties

        #region AddonFolder

        public string ArmaBrowserAddonFolder
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify) +
                    System.IO.Path.DirectorySeparatorChar + @"ArmaBrowser" + System.IO.Path.DirectorySeparatorChar + "Arma 3" +
                    System.IO.Path.DirectorySeparatorChar + "Addons" + System.IO.Path.DirectorySeparatorChar;
            }
        }

        public string Arma3UserAddonFolder
        {
            get
            {
                return
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments,
                        Environment.SpecialFolderOption.DoNotVerify) +
                    System.IO.Path.DirectorySeparatorChar + "Arma 3" + System.IO.Path.DirectorySeparatorChar;
            }
        }
        
        public string Arma3SteamAddonFolder
        {
            get
            {
                return this.ArmaPath + System.IO.Path.DirectorySeparatorChar + "!Workshop" + System.IO.Path.DirectorySeparatorChar;
            }
        }

        #endregion

        public ServerListViewModel()
        {
            if (!System.IO.Directory.Exists(this.ArmaBrowserAddonFolder))
                System.IO.Directory.CreateDirectory(this.ArmaBrowserAddonFolder);

            this.UpdateAvailable = new UpdateAvailableViewModel();
            this.UpdateAvailable.CheckForNewReleases().Wait(0);

            this._version = this.GetType().Assembly.GetName().Version.ToString();
            
            UiTask.Initialize();
            ModInstallPath[] modFolders =
            {
                new ModInstallPath(this.ArmaBrowserAddonFolder, false),
                new ModInstallPath(this.Arma3UserAddonFolder, false)
            };
            this._context = new LogicContext(modFolders);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            this._context.ServerItems.CollectionChanged += this._serverItems_CollectionChanged;
            this._context.PropertyChanged += this.Context_PropertyChanged;

            this.TextFilter = Properties.Settings.Default.TextFilter;
            this._selectedEndPoint = Properties.Settings.Default.LastPlayedHost;

            this.LookForInstallation();

            IServerItem[] recentlyServerItems = Properties.Settings.Default.RecentlyHosts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Reverse()
                .Select(this._context.ToServerItem)
                .ToArray();
            IServerItem[] favoriteServerItems = ServiceHub.Instance.GetService<FavoriteService>().Get();
            IServerItem[] serverItems = favoriteServerItems.Union(recentlyServerItems).Distinct(ServerItemComparer.Default).ToArray();

            this._context.AddServerItems(serverItems);
            foreach (var item in recentlyServerItems)
            {
                item.LastPlayed = DateTime.Now;
            }
            var refreshRecentlyServerItemsTask = this._context.RefreshServerInfoAsync(serverItems);
            refreshRecentlyServerItemsTask.ContinueWith((t,o) => this.ServerItemsView.Refresh(), null, TaskScheduler.FromCurrentSynchronizationContext());
            
            Task.Run((Action) this.EndlessRefreshSelecteItem);
        }

        async void RefreshServerInfoAsync(IServerItem[] serverItems)
        {
            await this._context.RefreshServerInfoAsync(serverItems);
            //ServerItemsView.Refresh(); // LostFocus Problem      
        }

        void _serverItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null) this.TotalPlayerCount += (e.NewItems.Cast<IServerItem>().First()).CurrentPlayerCount;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.TotalPlayerCount = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Context_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ArmaVersion": //_context.ArmaVersion
                    this.OnPropertyChanged(nameof(this.ArmaVersion));
                    break;
            }
        }
        
        public async void ReloadServerList()
        {
            this.BeginLoading();

            this._reloadingCts.Cancel();
            CancellationTokenSource oldTokenSource = this._reloadingCts;
            this._reloadingCts = new CancellationTokenSource();
            CancellationToken token = this._reloadingCts.Token;

            oldTokenSource.Cancel(false);
            try
            {
                await Task.Factory.StartNew(o => this.ReloadInternal((CancellationToken)o), token, token)
                    .ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        this.RememberLastVisiblyItems();
                    }
                }, token);
            }
            catch (Exception)
            {
                // ignored
            }

            this.EndLoading();
        }
        
        public bool LoadingBusy
        {
            get { return Interlocked.Read( ref this._loadingBusy) > 0; }
        }

        private void BeginLoading()
        {
            Interlocked.Add(ref this._loadingBusy, +1L);
            this.OnPropertyChanged("LoadingBusy");
        }

        private void EndLoading()
        {
            if (Interlocked.Read(ref this._loadingBusy) > 0)
                Interlocked.Add(ref this._loadingBusy, -1L);
            this.OnPropertyChanged("LoadingBusy");
        }

        void ReloadInternal(CancellationToken cancellationToken)
        {
            if (this._ipEndPointFilter != null)
                this._context.ReloadServerItem(this._ipEndPointFilter, cancellationToken);
            else
                this._context.ReloadServerItems(this._lastItems, cancellationToken);

            this.RefreshUsedAddons();
        }

        private bool OnServerItemsFilter(object o)
        {
            var result = true;
            var item = (IServerItem)o;

            if (this._ipEndPointFilter != null)
            {
                result = item.Host.Equals(this._ipEndPointFilter.Address) && item.Port == this._ipEndPointFilter.Port;
                return result;
            }

            if (!string.IsNullOrWhiteSpace(this._textFilter))
            {
                var strs = this._textFilter.Trim().Split(' ').ToArray();

                foreach (var s in strs)
                {
                    if (s == string.Empty) continue;

                    if (!result)
                        break;

                    if (s[0] == '-')
                    {
                        var s2 = s.Substring(1);
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        result = result && !(item.FullText.IndexOf(s2, StringComparison.CurrentCultureIgnoreCase) > -1);
                        continue;
                    }
                    if (s[0] == '>')
                    {
                        var s2 = s.Substring(1);
                        int playerCount;
                        if (Int32.TryParse(s2, out playerCount))
                        {
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            result = result && item.CurrentPlayerCount > playerCount;
                            continue;
                        }
                    }
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    result = result && (item.FullText.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) > -1);

                }

            }
            //if (result && _selectedAddon != null)
            //{
            //    result = item.Mods.Contains(_selectedAddon.ModName);
            //}

            return result;

        }

        private async void RefreshUsedAddons()
        {
            var selectedItem = this._selectedServerItem;
            
            if (this._selectedServerItem == null || this._selectedServerItem.Mods == null)
            {
                return;
            }
            //if (_selectedServerItem.Mods != null)
            //{
            int sortNr = 1;
            var hostAddons = this._selectedServerItem.Mods.Select(m => new { SortNr = sortNr++, ModName = m });

            var endpoint = string.Format("{0}:{1}", this._selectedServerItem.Host, this._selectedServerItem.Port);
            var hostCfgItem = HostConfigCollection.Default.Cast<HostConfig>()
                                .FirstOrDefault(h => h.EndPoint == endpoint);

            if (hostCfgItem != null)
            {
                int sortNr2 = 1;
                hostAddons = hostCfgItem.PossibleAddons.Split(';').Select(m => new { SortNr = sortNr2++, ModName = m }).ToArray();
            }

            var mods = (from mod in this.Addons
                        join selectedMod in hostAddons on mod.ModName equals selectedMod.ModName into selectedMods
                        from selectedMod in selectedMods.DefaultIfEmpty()
                        let sortnr = selectedMod == null ? 0 : selectedMod.SortNr
                        let selectedModName = selectedMod == null ? null : selectedMod.ModName
                        where mod.IsInstalled
                        orderby sortnr
                        select new { mod, selectedMod = selectedModName, Sortnr = sortnr }).ToArray();
            //}

            var s = selectedItem.Signatures ?? string.Empty;
            var hostAddonKeyNames = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            await this._context.UpdateAddonInfos(hostAddonKeyNames);

            // Addons automatisch ab- oder aus- wählen
            foreach (var item in mods)
            {
                item.mod.IsActive = item.mod.IsInstalled && !string.IsNullOrWhiteSpace(item.selectedMod);
               
                var canActive = false;
                if (hostAddonKeyNames.Any())
                {
                    foreach (var addonKey in item.mod.KeyNames.Select(k => k.Name))
                    {
                        if (hostAddonKeyNames.Contains(addonKey))
                        {
                            canActive = true;
                            break;
                        }
                    }
                }
                item.mod.CanActived = canActive;
                if (Properties.Settings.Default.SelectAllAceptedAddons)
                    item.mod.IsActive = canActive;

            }


            //Task<IEnumerable<RestAddonInfoResult>> addonInfosTask = await _context.GetAddonInfosAsync(hostAddonKeyNames);
            //addonInfosTask.ContinueWith(parentTask =>
            //{
            //    if (parentTask.Status != TaskStatus.RanToCompletion) return;

            //    var msgStr = new StringBuilder();

            //    foreach (var addonInfo in parentTask.Result)
            //    {
            //        msgStr.AppendLine(addonInfo.name);

            //        var updAddon =
            //            Addons.FirstOrDefault(
            //                a =>
            //                    a.KeyNames.Any(
            //                        tag => tag.Hash.Equals(addonInfo.hash, StringComparison.OrdinalIgnoreCase)));

            //        //if (!Addons.Any(a => a.ModName.Equals(addonInfo.name, StringComparison.OrdinalIgnoreCase)))
            //        if (updAddon == null)
            //        {
            //            UiTask.Run(a =>
            //            {
            //                Addons.Add(new Addon()
            //                {
            //                    Name = a.name,
            //                    ModName = a.name,
            //                    DisplayText = a.name,
            //                    KeyNames = new[] { new Data.AddonKey { Name = a.keytag, Hash = a.hash} },
            //                    //DownlandUris = new Uri[] { new Uri("http://www.armabrowser.de/"), },
            //                    IsInstalled = false,
            //                    IsEasyInstallable = addonInfo.easyinstall
            //                });
            //            }, addonInfo);
            //        }
            //    }

                //MessageBox.Show(msgStr.ToString());



            //});

        }

        /// <summary>
        /// Steam install folder of Arma3
        /// </summary>
        public string ArmaPath
        {
            get
            {
                return this._context.ArmaPath;
            }
        }

        public string ArmaVersion
        {
            get
            {
                return this._context.ArmaVersion;
            }
        }

        public bool IsJoinig
        {
            get { return this._isJoining; }
            set
            {
                this._isJoining = value;
                this.OnPropertyChanged();
            }
        }

        public string Version
        {
            get { return this._version; }
        }


        public bool LaunchWithoutHost
        {
            get { return this._launchWithoutHost; }
            set
            {
                this._launchWithoutHost = value;
                this.OnPropertyChanged();
            }
        }


        public int TotalPlayerCount
        {
            get { return this._totalPlayerCount; }
            set
            {
                this._totalPlayerCount = value;
                this.OnPropertyChanged();
            }
        }

        internal void OpenArma()
        {
            var host = this._selectedServerItem;
            if (this._launchWithoutHost)
                host = null;

            if (host != null && host.Port <= System.Net.IPEndPoint.MinPort)
                return;

            var endpoint = host != null ? string.Format("{0}:{1}", host.Host, host.Port) : string.Empty;
            var usedAddons = this.Addons.Where(a => a.IsActive).OrderBy(a => a.ActivationOrder).ToArray();
            var hostCfgItem = HostConfigCollection.Default.Cast<HostConfig>().FirstOrDefault(h => h.EndPoint == endpoint);
            if (hostCfgItem == null)
            {
                hostCfgItem = new HostConfig
                {
                    EndPoint = endpoint,
                    PossibleAddons = string.Empty
                };
                HostConfigCollection.Default.Add(hostCfgItem);
                Properties.Settings.Default.LastPlayedHost = endpoint;
            }
            hostCfgItem.PossibleAddons = string.Join(";", usedAddons.OrderBy(a => a.ActivationOrder).Select(a => a.ModName).ToArray());


            this.SaveHistory();

            this._context.Open(host, usedAddons.ToArray());
        }

        private void SaveHistory()
        {
            if (this._selectedServerItem == null) return;

            this._selectedServerItem.LastPlayed = DateTime.Now;

            // limit addresses to 10 entries
            {
                var items = this._context.ServerItems.Where(srv => srv.LastPlayed.HasValue).OrderByDescending(srv => srv.LastPlayed).Skip(10);
                foreach (var item in items)
                {
                    item.LastPlayed = null;
                }
            }

            // Save histroy addresses
            {
                var items = this._context.ServerItems.Where(srv => srv.LastPlayed.HasValue).OrderByDescending(srv => srv.LastPlayed).Select(srv => string.Format("{0}:{1}", srv.Host, srv.QueryPort)).ToArray();
                Properties.Settings.Default.RecentlyHosts = string.Join(" ", items);
            }


            // Save favorits
            {
                var items = this._context.ServerItems.Where(srv => srv.IsFavorite).Select(srv => string.Format("{0}:{1}", srv.Host, srv.QueryPort)).ToArray();
                Properties.Settings.Default.Favorits = string.Join(" ", items);
            }

            Properties.Settings.Default.Save();
        }

        //internal void TestJson()
        //{
        //    _context.TestJson();
        //}

        private async void EndlessRefreshSelecteItem()
        {
            while (true)
            {
                var mainwin = await UiTask.Run(() => Application.Current.MainWindow);

                if (mainwin == null) break;
                try
                {
                    var task = UiTask.Run(() => mainwin.WindowState != WindowState.Minimized);
                    task.Wait();
                    if (task.IsCompleted && task.Result && !this._isJoining)
                    {
                        var item = this.SelectedServerItem;
                        if (item != null)
                        {
                            this._context.RefreshServerInfo(new[] { item });
                        }
                    }
                    await Task.Delay(3000);
                }
                catch
                {
                    // ignore
                }
            }
        }

        internal void LookForInstallation()
        {
            this._context.LookForArmaPath();
            this.OnPropertyChanged("ArmaPath");
            this.OnPropertyChanged("ArmaVersion");
            Properties.Settings.Default.ArmaPath = this._context.ArmaPath;
        }

        internal void SaveFavorits(IServerItem item)
        {
            if (item.IsFavorite)
            {
                ServiceHub.Instance.GetService<FavoriteService>().Add(item);
            }
            else
            {
                ServiceHub.Instance.GetService<FavoriteService>().Remove(item);
            }
        }

        internal void StopAll()
        {
            this._reloadingCts.Cancel();
            this._reloadingCts = new CancellationTokenSource();
        }

        internal void RefreshAddons()
        {
            this._context.ReloadAddons();
        }

        public void DownloadAddon(IAddon addon)
        {
            this._context.DownloadAddonAsync(addon);
        }

        private class ServerItemComparer : IEqualityComparer<IServerItem>
        {
            internal static readonly ServerItemComparer Default = new ServerItemComparer();

            bool IEqualityComparer<IServerItem>.Equals(IServerItem x, IServerItem y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                return x.Host + " " + x.Port == y.Host + " " + y.Port;
            }

            public int GetHashCode(IServerItem obj)
            {
                return (obj.Host + " " + obj.Port).GetHashCode();
            }
        }
    }

}
