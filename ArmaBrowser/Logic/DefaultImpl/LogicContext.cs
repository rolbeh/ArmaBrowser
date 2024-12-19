using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using ArmaBrowser.Data;
using ArmaBrowser.Data.DefaultImpl;
using ArmaBrowser.Data.DefaultImpl.Rest;
using ArmaBrowser.Properties;
using Magic.Steam;

namespace ArmaBrowser.Logic
{
    internal class LogicContext : ObjectNotify, ILogicContext
    {
        private LogicContext()
        {
            this._defaultDataRepository = DataManager.CreateNewDataRepository();

            this._defaultServerRepository = DataManager.CreateNewServerRepository();
        }

        public LogicContext(ModInstallPath[] modFolders) : this()
        {
            this._modFolders = modFolders;
        }

        public void LookForArmaPath()
        {
            this._armaPath = this._defaultDataRepository.GetArma3Folder();
            this.OnPropertyChanged(nameof(this.ArmaPath));
            this._armaVersion = null;
            this.OnPropertyChanged(nameof(this.ArmaVersion));
        }

        public void ReloadServerItems(IPEndPoint[] lastAddresses, CancellationToken cancellationToken)
        {
            Data.DefaultImpl.ServerItem[] recently = this.ServerItems.Where(srv => srv.LastPlayed.HasValue)
                .Select(a => new Data.DefaultImpl.ServerItem {Host = a.Host, QueryPort = a.QueryPort})
                .ToArray();

            try
            {
                UiTask.Run(this.ServerItems.Clear, cancellationToken).Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Reloading canceled");
                return;
            }

            ISteamGameServer[] lastServer = lastAddresses.Distinct()
                .Select(a => (ISteamGameServer) new Data.DefaultImpl.ServerItem {Host = a.Address, QueryPort = a.Port})
                .ToArray();

            IEnumerable<ISteamGameServer> discoveredServer = this._defaultServerRepository.DiscoverServer()
                .Except(lastServer, ServerQueryAddressComparer.Default)
                .Except(recently, ServerQueryAddressComparer.Default);

            IEnumerable<ISteamGameServer> allServer = recently
                .Union(lastServer.Except(recently, ServerQueryAddressComparer.Default))
                .Union(discoveredServer);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            BufferBlock<IServerItem> buffer = new BufferBlock<IServerItem>();
            BatchBlock<IServerItem> batchBlock = new BatchBlock<IServerItem>(10, new GroupingDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });
            buffer.LinkTo(batchBlock, new DataflowLinkOptions {PropagateCompletion = true});
            ActionBlock<IServerItem[]> insertActionBlock = new ActionBlock<IServerItem[]>(
                items => { items.Each(i => this.ServerItems.Add(i)); },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1,
                    CancellationToken = cancellationToken,
                    SingleProducerConstrained = true,
                    TaskScheduler = UiTask.UiTaskScheduler
                });
            batchBlock.LinkTo(insertActionBlock, new DataflowLinkOptions {PropagateCompletion = true});

            int maxParallelCount = Environment.ProcessorCount * 2;
            const int maxBlockCount = 300;
            List<WaitHandle> waitArray = new List<WaitHandle>();
            List<ISteamGameServer> block = new List<ISteamGameServer>();

            foreach (ISteamGameServer steamGameServer in allServer)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                block.Add(steamGameServer);
                if (block.Count >= maxBlockCount)
                {
                    waitArray.Add(this.EnsureNewQueryingThread(cancellationToken, maxParallelCount, block, buffer));
                }
            }

            if (block.Any())
            {
                waitArray.Add(this.EnsureNewQueryingThread(cancellationToken, maxParallelCount, block, buffer));
            }

            WaitHandle.WaitAll(waitArray.ToArray());
            buffer.Complete();
            batchBlock.Completion.Wait(5000);
            insertActionBlock.Completion.Wait(5000);
        }

        public void ReloadServerItem(IPEndPoint iPEndPoint, CancellationToken cancellationToken)
        {
            ObservableCollection<IServerItem> dest = this.ServerItems;
            try
            {
                UiTask.Run(dest.Clear, cancellationToken).Wait(cancellationToken);
            }

            catch (OperationCanceledException)
            {
                Trace.WriteLine("Reloading canceled");
                return;
            }

            ISteamGameServer vo =
                this._defaultServerRepository.GetServerInfo(new IPEndPoint(iPEndPoint.Address, iPEndPoint.Port + 1));
            if (cancellationToken.IsCancellationRequested || vo == null)
            {
                return;
            }

            ServerItem item = new ServerItem();
            AssignProperties(item, vo);

            UiTask.Run((dest2, item2) => dest2.Add(item2), dest, item).Wait(0);
        }


        public Collection<IAddon> Addons
        {
            get
            {
                if (this._addons == null)
                {
                    this._addons = new ObservableCollection<IAddon>();


                    if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    {
                        this._addons.Add(new Addon
                        {
                            Name = "no Sign Mod1",
                            DisplayText = "DisplayText CannotActivated (no Sign Mod1)",
                            ModName = "@ModName",
                            Version = "000.00.0",
                            KeyNames = new[] {new AddonKey {Name = "key"}}
                        });

                        this._addons.Add(new Addon
                        {
                            Name = "new Sign Mod2",
                            DisplayText = "DisplayText CanActivated (new Sign Mod2)",
                            ModName = "@ModName",
                            Version = "000.00.0",
                            KeyNames = new[] {new AddonKey {Name = "key"}},
                            CanActived = true
                        });
                    }
                    else
                    {
                        this.ReloadAddons();
                    }
                }

                return this._addons;
            }
        }


        public void Open(IServerItem serverItem, IAddon[] addons, bool runAsAdmin = false)
        {
            string addonArgs = string.Empty;
            if (addons.Any())
            {
                addonArgs = $@" ""-mod={string.Join(";", addons.Select(i => i.CommandlinePath).ToArray())}""";
            }

            string serverArgs = serverItem != null
                ? $" -connect={serverItem.Host} -port={serverItem.Port}"
                : string.Empty;
            string path = Settings.Default.ArmaPath;
            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(path, "arma3battleye.exe"),
                Arguments =
                    " 2 1 1 -exe Arma3_x64 -skipintro -noSplash -noPause -world=empty " + addonArgs + serverArgs,
                // -malloc=system
                WorkingDirectory = path,
                UseShellExecute = true
            };

            if (runAsAdmin)
            {
                psInfo.Verb = "runsas";
            }

            try
            {
                Process ps = Process.Start(psInfo);
                if (ps != null)
                {
                    ps.EnableRaisingEvents = true;
                    ps.Exited += this.ps_Exited;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }


        public void RefreshServerInfo(IServerItem[] serverItems)
        {
            if (serverItems == null)
            {
                return;
            }

            foreach (ServerItem serverItem in serverItems.Cast<ServerItem>())
            {
                if (serverItem?.Host == null)
                {
                    return;
                }

                if (serverItem.QueryPort == 0)
                {
                    return;
                }


                ISteamGameServer dataItem = null;
                try
                {
                    dataItem = this._defaultServerRepository.GetServerInfo(new IPEndPoint(serverItem.Host,
                        serverItem.QueryPort));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                if (dataItem == null)
                {
                    serverItem.Ping = 999;
                    continue;
                }

                UiTask.Run(AssignProperties, serverItem, dataItem).Wait();
                //AssignProperties(serverItem, dataItem);
            }
        }

        public async Task RefreshServerInfoAsync(IServerItem[] items)
        {
            await Task.Run(() => this.RefreshServerInfo(items));
        }

        private WaitHandle EnsureNewQueryingThread(CancellationToken cancellationToken, int maxParallelCount,
                                                   List<ISteamGameServer> block, BufferBlock<IServerItem> buffer)
        {
            while (this.ReloadThreads.Count >= maxParallelCount)
            {
                Task.Delay(150, cancellationToken).Wait(cancellationToken);
            }

            lock (this.ReloadThreads)
            {
                LoadingServerListContext threadContext =
                    this.NewQueryThread(buffer, cancellationToken, block.ToArray());
                block.Clear();
                return threadContext.Reset;
            }
        }

        private LoadingServerListContext NewQueryThread(BufferBlock<IServerItem> dest, CancellationToken token,
                                                        ISteamGameServer[] ips)
        {
            Thread thread = new Thread(this.LoadingServerList)
            {
                Name = "UDP " + this.ReloadThreads.Count + 1,
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            LoadingServerListContext threadContext = new LoadingServerListContext
            {
                Dest = dest,
                Ips = ips,
                Token = token
            };
            lock (this._reloadThreadsLock)
            {
                UiTask.Run(this.ReloadThreads.Add, threadContext).Wait(100);
            }

            thread.Start(threadContext);
            return threadContext;
        }

        private void LoadingServerList(object loadingServerListContext)
        {
            LoadingServerListContext state = (LoadingServerListContext) loadingServerListContext;

            BufferBlock<IServerItem> innerBuffer = new BufferBlock<IServerItem>();
            using (innerBuffer.LinkTo(state.Dest))
            {
                IObserver<IServerItem> asObserver = innerBuffer.AsObserver();
                foreach (ISteamGameServer dataItem in state.Ips)
                {
                    if (state.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    ServerItem item = this.AddGameServer(dataItem, asObserver, state.Token).Result;
                    if (item != null)
                    {
                        state.ProgressValue++;
                        state.Ping = item.Ping;
                    }
                }

                asObserver.OnCompleted();
            }

            state.Finished();
            Task.Delay(TimeSpan.FromSeconds(0.5))
                .ContinueWith((t, ctx) =>
                    {
                        lock (this._reloadThreadsLock)
                        {
                            this.ReloadThreads.Remove((LoadingServerListContext) ctx);
                        }
                    }
                    , state, UiTask.UiTaskScheduler)
                .Wait();
            //UiTask.Run(ctx => ReloadThreads.Remove(ctx), state).Wait();
        }

        private async Task<ServerItem> AddGameServer(ISteamGameServer dataItem, IObserver<IServerItem> dest,
                                                     CancellationToken cancellationToken)
        {
            IPEndPoint serverQueryEndpoint = new IPEndPoint(dataItem.Host, dataItem.QueryPort);

            ISteamGameServer vo = this._defaultServerRepository.GetServerInfo(serverQueryEndpoint);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            ServerItem item = new ServerItem();
            if (vo != null)
            {
                AssignProperties(item, vo);
            }
            else
            {
                item.Name = $"{serverQueryEndpoint.Address}:{serverQueryEndpoint.Port - 1}";
                item.Host = serverQueryEndpoint.Address;
                item.QueryPort = serverQueryEndpoint.Port;
            }

            dest.OnNext(item);
            await Task.FromResult(item);
            //await UiTask.Run((dest2, item2) => dest2.OnNext(item2), dest, item);
            return item;
        }

        private static void AssignProperties(ServerItem item, ISteamGameServer vo)
        {
            string[] keyWordsParts = vo.Keywords.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToArray();
            Dictionary<string, string> keyWords = new Dictionary<string, string>(keyWordsParts.Length);

            foreach (string s in keyWordsParts)
            {
                string k = s.Substring(0, 1);
                if (!keyWords.ContainsKey(k))
                {
                    keyWords.Add(k, s.Substring(1));
                }
            }

            //serverItem1.Country = dataItem1.Country;
            item.Gamename = vo.Gamename;
            item.Host = vo.Host;
            item.Island = vo.Map;
            item.Mission = vo.Mission;
            item.Mode = keyWords.FirstOrDefault(k => k.Key == "t").Value ?? string.Empty; //dataItem1.Mode;
            item.Modhashs = vo.Modhashs;
            item.ModsText = vo.Mods;
            item.Name = vo.Name;
            item.CurrentPlayerCount = vo.CurrentPlayerCount;
            item.MaxPlayers = vo.MaxPlayers;
            item.Port = vo.Port;
            item.QueryPort = vo.QueryPort;
            item.Signatures = vo.Signatures;
            item.VerifySignatures = vo.VerifySignatures;
            item.Version = vo.Version;
            item.Passworded = keyWords.FirstOrDefault(k => k.Key == "l").Value == "t"; //dataItem1.HasPassword;
            item.Ping = vo.Ping;
            if (vo.Players != null)
            {
                item.CurrentPlayersText = string.Join(", ", vo.Players.Select(p => p.Name).OrderBy(s => s));
                item.CurrentPlayers = vo.Players.OrderBy(p => p.Name).ToArray();
            }
        }

        internal async void ReloadAddons()
        {
            if (this._addons == null)
            {
                this._addons = new ObservableCollection<IAddon>();
            }

            this._addons.Clear();

            string armaPath = Settings.Default.ArmaPath;

            await this.ReloadAddonsAsync(armaPath, true);
            await this.ReloadAddonsAsync(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments,
                    Environment.SpecialFolderOption.DoNotVerify) +
                Path.DirectorySeparatorChar + "Arma 3" + Path.DirectorySeparatorChar, true);

            await this.ReloadAddonsAsync(
                this.ArmaPath + Path.DirectorySeparatorChar + "!Workshop" + Path.DirectorySeparatorChar, false);

            foreach (ModInstallPath modInstallPath in this._modFolders)
            {
                await this.ReloadAddonsAsync(modInstallPath.Path, modInstallPath.IsDefault);
            }

            AddonWebApi.PostInstalledAddonsKeysAsync(this._addons.ToArray()).Wait(0);
        }

        private async Task ReloadAddonsAsync(string path, bool isArmaDefaultPath)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                IArmaAddon[] items = await Task.Run(() => this._defaultDataRepository.GetInstalledAddons(path));
                foreach (IArmaAddon item in items)
                {
                    this._addons.Add(new Addon
                    {
                        Name = item.Name,
                        DisplayText = $"{item.DisplayText} ({item.Name})",
                        Path = item.Path,
                        ModName = item.ModName,
                        Version = item.Version,
                        KeyNames = item.KeyNames,
                        IsInstalled = true,
                        IsArmaDefaultPath = isArmaDefaultPath
                    });
                }
            }
        }

        //public void TestJson()
        //{
        //    _defaultBrowserServerRepository.Test();
        //}

        private void ps_Exited(object sender, EventArgs e)
        {
            //Todo Move to ViewModel Code
            UiTask.Run(() =>
            {
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Activate();
                }
            }).Wait(0);
        }

        internal IServerItem ToServerItem(string hostQueryEndpoint)
        {
            int pos = hostQueryEndpoint.IndexOf(':');
            string address = hostQueryEndpoint.Substring(0, pos);
            int.TryParse(hostQueryEndpoint.Substring(pos + 1), out int port);

            return new ServerItem { Host = IPAddress.Parse(address), QueryPort = port, Name = address };
        }

        internal IServerItem[] AddServerItems(IEnumerable<string> hostQueryAddresses)
        {
            List<IServerItem> result = new List<IServerItem>();
            foreach (string hostQueryAddress in hostQueryAddresses)
            {
                IServerItem serverItem = this.ToServerItem(hostQueryAddress);
                this.ServerItems.Add(serverItem);
                result.Add(serverItem);
            }

            return result.ToArray();
        }

        internal void AddServerItems(IEnumerable<IServerItem> serverItems)
        {
            IEnumerable<IServerItem> items = serverItems.ToArray();
            foreach (IServerItem serverItem in items)
            {
                this.ServerItems.Add(serverItem);
            }
        }

        private async Task<IEnumerable<RestAddonInfoResult>> GetAddonInfosAsync(params string[] addonKeynames)
        {
            return await AddonWebApi.GetAddonInfosAsync(addonKeynames);
        }

        //internal void AddAddonUri(IAddon addon, string uri)
        //{
        //    //Task.Run(() =>
        //    //{
        //    //    var webApi = new AddonWebApi();
        //    //    webApi.AddAddonDownloadUri(addon,uri);
        //    //});
        //}

        internal static async Task UploadAddonAsync(IAddon addon)
        {
            RestAddonInfoResult[] infos =
                await AddonWebApi.GetAddonInfosAsync(addon.KeyNames.Select(k => k.Hash).ToArray());
            if (!infos.Any() || infos.Any(a => a.easyinstall))
            {
                return;
            }

            AddonWebApi.UploadAddon(addon);
        }

        internal async void DownloadAddonAsync(IAddon addon)
        {
            if (addon.IsEasyInstallable.HasValue && addon.IsEasyInstallable.Value && addon.KeyNames.Any() &&
                addon.KeyNames.Any(k => !string.IsNullOrEmpty(k.Hash)))
            {
                Addon installedAddon = await Task.Run(() =>
                {
                    string targetFolder =
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments,
                            Environment.SpecialFolderOption.DoNotVerify) +
                        Path.DirectorySeparatorChar + @"ArmaBrowser" + Path.DirectorySeparatorChar +
                        "Arma 3" +
                        Path.DirectorySeparatorChar + "Addons" + Path.DirectorySeparatorChar;
                    string hash = addon.KeyNames.First(k => !string.IsNullOrEmpty(k.Hash)).Hash;
                    AddonWebApi.DownloadAddon(addon, hash, targetFolder);

                    IArmaAddon[] addons = this._defaultDataRepository.GetInstalledAddons(targetFolder);
                    IArmaAddon item = addons.FirstOrDefault(a => a.KeyNames.Any(k => k.Hash == hash));
                    if (item != null)
                    {
                        return new Addon
                        {
                            Name = item.Name,
                            DisplayText = $"{item.DisplayText} ({item.Name})",
                            Path = item.Path,
                            ModName = item.ModName,
                            Version = item.Version,
                            KeyNames = item.KeyNames,
                            IsInstalled = true,
                            IsEasyInstallable = true,
                            CanActived = addon.CanActived,
                            IsActive = addon.CanActived
                        };
                    }

                    return null;
                });

                if (installedAddon != null)
                {
                    int idx = this._addons.IndexOf(addon);
                    this._addons[idx] = installedAddon;
                }
            }
        }

        public async Task UpdateAddonInfos(string[] hostAddonKeyNames)
        {
            IEnumerable<RestAddonInfoResult> addonInfosTask = await this.GetAddonInfosAsync(hostAddonKeyNames);

            IEnumerable<RestAddonInfoResult> addonInfosTaskResult = addonInfosTask;

            //await addonInfosTask.ContinueWith(parentTask =>
            {
                //if (parentTask.Status != TaskStatus.RanToCompletion) return;

                //var addonInfosTaskResult = parentTask.Result;
                foreach (RestAddonInfoResult addonInfo in addonInfosTaskResult)
                {
                    IAddon updAddon = this.Addons.FirstOrDefault(
                        a =>
                            a.KeyNames.Any(
                                tag => tag.Hash.Equals(addonInfo.hash, StringComparison.OrdinalIgnoreCase)));

                    if (updAddon != null)
                    {
                        updAddon.IsEasyInstallable = addonInfo.easyinstall;
                    }
                    else
                    {
                        if (addonInfo.easyinstall)
                        {
                            this.Addons.Add(new Addon
                            {
                                Name = addonInfo.name,
                                ModName = addonInfo.name,
                                DisplayText = addonInfo.name,
                                KeyNames = new[] {new AddonKey {Name = addonInfo.keytag, Hash = addonInfo.hash}},
                                //DownlandUris = new Uri[] { new Uri("http://www.armabrowser.de/"), },
                                IsInstalled = false,
                                IsEasyInstallable = addonInfo.easyinstall,
                                CanActived = true
                            });
                        }
                    }
                }
            } //);
        }

        private class ServerQueryAddressComparer : IEqualityComparer<ISteamGameServer>
        {
            internal static readonly ServerQueryAddressComparer Default = new ServerQueryAddressComparer();

            bool IEqualityComparer<ISteamGameServer>.Equals(ISteamGameServer x, ISteamGameServer y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                return x.Host + " " + x.QueryPort == y.Host + " " + y.QueryPort;
            }

            public int GetHashCode(ISteamGameServer obj)
            {
                return (obj.Host + " " + obj.QueryPort).GetHashCode();
            }
        }

        #region Fields

        private readonly IArma3DataRepository _defaultDataRepository;
        private ObservableCollection<IAddon> _addons;

        private readonly Arma3ServerRepositorySteam _defaultServerRepository;

        //private Data.IArmaBrowserServerRepository _defaultBrowserServerRepository;
        private string _armaPath;
        private string _armaVersion;
        private readonly ModInstallPath[] _modFolders;
        private readonly object _reloadThreadsLock = new object();

        #endregion Fields

        #region Properties

        public ObservableCollection<IServerItem> ServerItems { get; } = new ObservableCollection<IServerItem>();

        public ObservableCollection<LoadingServerListContext> ReloadThreads { get; } =
            new ObservableCollection<LoadingServerListContext>();

        public string ArmaVersion
        {
            get
            {
                if (this._armaVersion == null)
                {
                    string folder = Settings.Default.ArmaPath;
                    if (!string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "arma3.exe")))
                    {
                        FileVersionInfo version = FileVersionInfo.GetVersionInfo(Path.Combine(folder, "arma3.exe"));
                        this._armaVersion =
                            $"{version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart:000}{version.FilePrivatePart}";
                    }
                    else
                    {
                        this._armaVersion = "unknown";
                    }
                }

                return this._armaVersion;
            }
        }

        public string ArmaPath => this._armaPath ?? (this._armaPath = this._defaultDataRepository.GetArma3Folder());

        #endregion Properties
    }

    internal class LoadingServerListContext : ObjectNotify
    {
        private readonly ManualResetEvent _reset;
        private bool _isRemoving;
        private int _ping;
        private int _progressValue;

        public LoadingServerListContext()
        {
            this._reset = new ManualResetEvent(false);
        }

        public ISteamGameServer[] Ips { get; set; }
        public BufferBlock<IServerItem> Dest { get; set; }
        public CancellationToken Token { get; set; }

        public WaitHandle Reset => this._reset;

        public int MaximumValue => this.Ips?.Length ?? 0;

        public int ProgressValue
        {
            get => this._progressValue;
            set
            {
                if (value == this._progressValue)
                {
                    return;
                }

                this._progressValue = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Percent));
            }
        }


        public double Percent
        {
            get
            {
                if (this.MaximumValue <= 0)
                {
                    return 0;
                }

                return this.ProgressValue / (this.MaximumValue * 1d);
            }
        }

        public int Ping
        {
            get => this._ping;
            set
            {
                if (value == this._ping)
                {
                    return;
                }

                this._ping = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsRemoving
        {
            get => this._isRemoving;
            private set
            {
                if (value == this._isRemoving)
                {
                    return;
                }

                this._isRemoving = value;
                this.OnPropertyChanged();
            }
        }

        public void Finished()
        {
            this.IsRemoving = true;
            this._reset.Set();
        }
    }
}