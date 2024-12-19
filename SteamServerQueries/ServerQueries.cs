using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Magic.Steam.Queries;

namespace Magic.Steam
{
    public static class ServerQueries
    {

        private const byte PlayerRequestByte = 0x55;
        private const byte RequestChallengeByte = 0x41;
        private const byte ReponseHeanderByte = 0x44;
        private const byte RuleRequestByte = 0x56;
        private const byte RuleReponseByte = 0x45;
        private const string StringZero = "\0";
        static readonly Encoding CharEncoding = Encoding.GetEncoding(1252);

        [PublicAPI]
        public static IEnumerable<GameServerQueryEndPoint> DiscoverQueryEndPoints(
            [CanBeNull] Action<GameServerQueryOption> optionAction)
        {
            return DiscoverQueryEndPointsIndistinct(optionAction).Distinct(new SteamGameServerQueryEndPointComparer());
        }

        private static IEnumerable<GameServerQueryEndPoint> DiscoverQueryEndPointsIndistinct([CanBeNull]Action<GameServerQueryOption> optionAction)
        {
            var option   = new GameServerQueryOption();
            optionAction?.Invoke(option);

            // https://developer.valvesoftware.com/wiki/Master_Server_Query_Protocol

            var addressBytes = new byte[4];
            var portBytes = new byte[2];


            var dnsEntry = System.Net.Dns.GetHostEntry(option.MasterServerName);
            //var ip = dnsEntry.AddressList.Length > 1 ? dnsEntry.AddressList[1] : dnsEntry.AddressList[0];
            foreach (var ip in dnsEntry.AddressList.OrderBy(n => Guid.NewGuid()).ToArray())
            {
                Debug.WriteLine($"Read from  {ip}");
                var roundtrips = 0;
                IPEndPoint lastEndPoint = new IPEndPoint(0, 0);

                string filter = option.Filter.ToString();
                Debug.WriteLine("Filter: " + filter);

                using (System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient())
                {
                    var hasErros = false;
                    var timeoutReties = 5;
                    int lastRequestTick = 0;

                    udp.Connect(ip, 27011);

                    while (true)
                    {
                        byte[] buffer = null;
                        try
                        {
                            string request = "1ÿ";
                            request += lastEndPoint + StringZero;
                            request += filter;
                            request += StringZero;

                            var bytes = CharEncoding.GetBytes(request);

                            //udp.Connect("146.66.155.8", 27019);
                            IPEndPoint endp = udp.Client.RemoteEndPoint as IPEndPoint;
                            udp.Client.ReceiveTimeout = 900;

                            // throttling request max 30 request per minute
                            // I go save with 25 requests
                            int elapsedTime = Environment.TickCount - lastRequestTick;
                            int pause = (60 / 20) * 1000;
                            int wait = pause - elapsedTime;
                            if (wait > 0)
                            {
                                Debug.WriteLine($"wait {wait} milsec");
                                Thread.Sleep(wait);
                            }
                            var sendlen = udp.Send(bytes, bytes.Length);
                            lastRequestTick = Environment.TickCount;
                            
                            roundtrips++;
                            buffer = udp.Receive(ref endp);
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            if (timeoutReties > 0)
                            {
                                timeoutReties--;
                                Debug.WriteLine("wait 63 sec");
                                Thread.Sleep(63000);
                                continue;
                            }
                            break;
                        }
                        catch (TimeoutException)
                        {
                            hasErros = true;
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            hasErros = true;
                            break;
                        }

                        using (var mem = new System.IO.MemoryStream(buffer, false))
                        using (var br = new System.IO.BinaryReader(mem, Encoding.UTF8))
                        {
                            var i = br.ReadUInt32();
                            i = br.ReadUInt16();

                            while (mem.Position < mem.Length)
                            {

                                addressBytes[0] = br.ReadByte();
                                addressBytes[1] = br.ReadByte();
                                addressBytes[2] = br.ReadByte();
                                addressBytes[3] = br.ReadByte();

                                portBytes[0] = br.ReadByte();
                                portBytes[1] = br.ReadByte();

                                int port = portBytes[0] << 8 | portBytes[1];


                                if (addressBytes.All(b => b == 0))
                                    break;

                                lastEndPoint = new IPEndPoint(new IPAddress(addressBytes), port);

                                var item = new GameServerQueryEndPoint
                                {
                                    Host = lastEndPoint.Address,
                                    QueryPort = lastEndPoint.Port
                                };
                                yield return item;
                            }

                            Debug.WriteLine("RoundTrips {0} - {1}", roundtrips, lastEndPoint.ToString());
                            
                            if (addressBytes.All(b => b == 0))
                            {
                                Debug.WriteLine("finished GameServerQueryEndPoints successfull");
                                break;
                            }
                        }
                    }
                    if (!hasErros)
                        break;
                }
            }
        }
        
        class SteamGameServerQueryEndPointComparer : IEqualityComparer<GameServerQueryEndPoint>
        {
            #region Implementation of IEqualityComparer<in SteamGameServerQueryEndPoint>

            public bool Equals(GameServerQueryEndPoint x, GameServerQueryEndPoint y)
            {
                return y != null && x != null 
                    && x.QueryPort == y.QueryPort && x.Host.ToString() == y.Host.ToString();
            }

            public int GetHashCode(GameServerQueryEndPoint obj)
            {
                return obj.QueryPort.GetHashCode() ^ 234
                       + obj.Host.GetHashCode() ^ 234;
            }

            #endregion
        }


        public static SteamGameServer GetServerInfo(GameServerQueryEndPoint gameServerQueryEndpoint)
        {
            return GetServerInfo(new IPEndPoint(gameServerQueryEndpoint.Host, gameServerQueryEndpoint.QueryPort));
        }

        public static SteamGameServer GetServerInfo(IPEndPoint gameServerQueryEndpoint)
        {
            byte[] startBytes = new byte[] {0xFF, 0xFF, 0xFF, 0xFF};
            byte Header = 0x54;
            string qryAsString = "Source Engine Query";

            List<byte> qry = new List<byte>();
            qry.AddRange(startBytes);
            qry.Add(Header);
            qry.AddRange(Encoding.Default.GetBytes(qryAsString));
            qry.Add(0x00);

            ServerQueryResponse item = null;
            var sw = new System.Diagnostics.Stopwatch();
            var buffer = new byte[2048];
            using (System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient())
            {
                try
                {
                    udp.Connect(gameServerQueryEndpoint);

                    udp.AllowNatTraversal(true);
                    udp.Client.ReceiveTimeout = 300;

                    sw.Start();
                    udp.Send(qry.ToArray(), qry.Count);

                    System.Net.Sockets.SocketError errorCode;
                    var receivedLenght = udp.Client.Receive(buffer, 0, 2048, System.Net.Sockets.SocketFlags.None,
                        out errorCode);
                    sw.Stop();
                    if (errorCode != System.Net.Sockets.SocketError.Success) return null;
                    if (receivedLenght == buffer.Length)
                        throw new Exception("Buffer zu klein");


                    var receivedBytes = new byte[receivedLenght];
                    Buffer.BlockCopy(buffer, 0, receivedBytes, 0, receivedLenght);
                    //var receivedBytes = udp.Receive(ref endp);

                    var enconding = System.Text.Encoding.UTF8; // System.Text.Encoding.ASCII;

                    using (var mem = new System.IO.MemoryStream(receivedBytes))
                    using (var br = new System.IO.BinaryReader(mem, CharEncoding))
                    {
                        br.ReadInt32();
                        br.ReadByte();

                        item = new ServerQueryResponse();
                        item.IpAdresse = gameServerQueryEndpoint.Address.ToString();
                        item.ProtocolVersion = br.ReadByte();

                        item.Ping = (int) sw.ElapsedMilliseconds;

                        var count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                        item.GameServerName = enconding.GetString(br.ReadBytes(count));
                        br.ReadByte(); // null-byte

                        count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                        item.Map = enconding.GetString(br.ReadBytes(count));
                        br.ReadByte(); // null-byte
                        //item.Map = ReadStringNullTerminated(br, enconding);

                        count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                        item.Folder = enconding.GetString(br.ReadBytes(count));
                        br.ReadByte(); // null-byte

                        count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                        item.Game = enconding.GetString(br.ReadBytes(count));
                        br.ReadByte(); // null-byte

                        item.ID = br.ReadInt16();

                        item.CurrentPlayerCount = br.ReadByte();

                        item.MaxPlayerCount = br.ReadByte();

                        item.CurrentBotsCount = br.ReadByte();

                        item.ServerType = (ServerQueryRequestType) br.ReadByte();

                        item.Password = br.ReadByte() == 1;

                        item.VAC = br.ReadByte() == 1;

                        item.Mode = br.ReadByte() == 1;

                        count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                        item.Version = enconding.GetString(br.ReadBytes(count));
                        br.ReadByte(); // null-byte

                        var edfCode = br.ReadByte();

                        item.Data = receivedBytes;

                        if ((edfCode & 0x80) == 0x80)
                            item.GamePort = br.ReadUInt16();



                        if ((edfCode & 0x10) == 0x10)
                            item.ServerSteamId = br.ReadInt64();

                        //if ((edfCode & 0x40) == 0x40)
                        //    Console.WriteLine("spectator server");

                        if ((edfCode & 0x20) == 0x20)
                        {
                            count = Array.FindIndex(receivedBytes, (int) mem.Position, IsNULL) - (int) mem.Position;
                            item.Keywords = enconding.GetString(br.ReadBytes(count));
                            br.ReadByte(); // null-byte
                        }

                        if ((edfCode & 0x01) == 0x01)
                            item.GameID = br.ReadInt64();

                        // https://community.bistudio.com/wiki/STEAMWORKSquery
                        // https://developer.valvesoftware.com/wiki/Master_Server_Query_Protocol
                    }

                    RequestRules(item, udp);

                    if (item.CurrentPlayerCount > 0)
                    {
                        udp.Client.ReceiveTimeout = Math.Max(300, Convert.ToInt32((6d * item.CurrentPlayerCount)));
                        RequestPlayers(item, udp);
                    }
                }
                catch
                {
                    sw.Stop();
                }

            }
            return item != null
                ? new SteamGameServer
                {
                    Host = gameServerQueryEndpoint.Address,
                    QueryPort = gameServerQueryEndpoint.Port,
                    Name = item.GameServerName,
                    //Gamename = SteamGameNameFilter,
                    Map = item.Map,
                    Mission = item.Game,
                    MaxPlayers = item.MaxPlayerCount,
                    CurrentPlayerCount = item.CurrentPlayerCount,
                    Passworded = item.Password,
                    Port = item.GamePort,
                    Version = item.Version,
                    Keywords = item.Keywords,
                    CurrentPlayers = new string[0],
                    Ping = item.Ping,
                    Players = item.Players?.Cast<ISteamGameServerPlayer>().ToArray(),
                    Mods = GetValue("modNames", item.KeyValues),
                    Modhashs = GetValue("modHashes", item.KeyValues),
                    Signatures = GetValue("sigNames", item.KeyValues),
                    VerifySignatures = item.Keywords.Contains(",vt,")
                }
                : null;
        }

        private static void RequestPlayers(ServerQueryResponse item, UdpClient udp)
        {
            try
            {
                IPEndPoint endp = null;

                var challengeRequest = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, PlayerRequestByte, 0xFF, 0xFF, 0xFF, 0xFF };
                var sendlen = udp.Send(challengeRequest, 9);
                if (sendlen != 9)
                    return;

                var challengeRespose = udp.Receive(ref endp); // challenge response

                if (challengeRespose[4] != RequestChallengeByte)
                {
                    System.Diagnostics.Trace.WriteLine("Error in RequestPlayers - Challenge response");
                    return;
                }

                challengeRespose[4] = PlayerRequestByte; // change to player request
                sendlen = udp.Send(challengeRespose, 9);
                if (sendlen != 9)
                {
                    return;
                }

                var playerRespose = udp.Receive(ref endp);

                if (playerRespose[4] != ReponseHeanderByte)
                {
                    System.Diagnostics.Trace.WriteLine("Error in RequestPlayers - Players response");
                    return;
                }

                byte playerCount = playerRespose[5];
                var offset = 6;
                var result = new List<ServerQueryResponse.Player>(byte.MaxValue);
                for (byte i = 0; i < playerCount; i++)
                {

                    var p = new ServerQueryResponse.Player();
                    p.Idx = playerRespose[offset];
                    offset += 1;
                    string s;
                    offset += ReadStringNullTerminated(playerRespose, offset, out s, Encoding.UTF8);
                    p.Name = s;
                    p.Score = BitConverter.ToInt32(playerRespose, offset);
                    offset += 4;
                    p.OnlineTime = TimeSpan.FromSeconds(Convert.ToDouble(BitConverter.ToSingle(playerRespose, offset)));

                    offset += 4;
                    result.Add(p);
                }
                item.Players = result;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static int ReadStringNullTerminated(byte[] source, int offset, out string result, Encoding encoding)
        {
            var len = Array.FindIndex(source, offset, b => b == byte.MinValue) - offset;
            result = encoding.GetString(source, offset, len);
            return len + 1/*null-byte*/;
        }

        private static void RequestRules(ServerQueryResponse item, System.Net.Sockets.UdpClient udp)
        {
            IPEndPoint endp = null;
            // Rules auslesen
            var challengeRequest = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, RuleRequestByte, 0xFF, 0xFF, 0xFF, 0xFF };
            udp.Send(challengeRequest, 9);

            byte[] respose = udp.Receive(ref endp);

            if (respose[4] != RequestChallengeByte)
            {
                System.Diagnostics.Debug.WriteLine("Error in RequestRules - Challenge response");
                return;
            }
            respose[4] = RuleRequestByte;
            udp.Send(respose, 9);
            respose = udp.Receive(ref endp);

            if (respose[4] != RuleReponseByte)
            {
                // no valid response
                return;
            }

            Version version = new Version(item.Version);
            if (version.Major >= 1 && version.Minor >= 56)
            {
                SteamDefragmentedBytes unFramed = respose.DefragmentSteamBytes_1_56();
#if DEBUG
                bool writeFile = Directory.Exists(@"E:\temp\Data");
                string filename = @"E:\temp\Data\V_" + item.Version + "_" + item.IpAdresse;
                if (writeFile)
                {
                    if (respose.Length > 7)
                    {
                        using (var file = new FileStream(filename + ".dat", FileMode.OpenOrCreate))
                        {
                            file.Write(item.Data, 0, item.Data.Length);
                            file.SetLength(item.Data.Length);
                        }

                        using (var file = new FileStream(filename + ".rdat", FileMode.OpenOrCreate))
                        {
                            file.Write(respose, 0, respose.Length);
                            file.SetLength(respose.Length);
                        }

                        using (var file = new FileStream(filename + ".rdefrag", FileMode.OpenOrCreate))
                        {
                            file.Write(unFramed.Bytes, 0, unFramed.Bytes.Length);
                            file.SetLength(unFramed.Bytes.Length);
                        }
                    }
                }
#endif
                if (respose[4] != 0x45)
                {
                    System.Diagnostics.Debug.WriteLine("Error in RequestRules - Rules response not valid");
                    return;
                }

                using (var decodedMem = unFramed.DecodeSteamRuleFile_1_56())
                {
#if DEBUG
                    if (writeFile)
                        using (var file = new FileStream(filename + ".rules", FileMode.OpenOrCreate))
                        {
                            decodedMem.Data.CopyTo(file);
                        }
                    decodedMem.Data.Seek(0, SeekOrigin.Begin);
#endif
                    var mods = ReadRules(decodedMem, version);
                    foreach (var keyValuePair in mods)
                    {
                        item.KeyValues.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
            }
            else
            {
                item.KeyValues.Add("sigNames:1:1", "Version " + item.Version + " not supported");
            }
        }

        [System.Diagnostics.Contracts.Pure]
        private static IEnumerable<KeyValuePair<string, string>> ReadRules(SteamDecodedBytes respose, Version version)
        {
            if (version.Major == 1)
            {
                if (version.Minor <= 54)
                {
                    return Enumerable.Empty<KeyValuePair<string, string>>();// not supported any more
                }
                if (version.Minor >= 56)
                {
                    return ReadRules_1_56(respose);
                }
            }
            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadRules_1_56(SteamDecodedBytes respose)
        {
            return ReadRuleFile(respose).Select(r => new KeyValuePair<string, string>(r.Key, r.Name + ";"));
        }

        public static IEnumerable<SteamServerRule> ReadRuleFile(SteamDecodedBytes steamDecoded, bool trace = false, bool writeAssertCode = false)
        {
            Stream file = steamDecoded.Data;
            file.Seek(0, SeekOrigin.Begin);
            using (var br = new BinaryReader(file, Encoding.ASCII, true))
            {
                var versionByte = br.ReadByte();
                var someExtras = br.ReadByte();
                var dlcFlags1 = br.ReadUInt16();
                var flags2 = br.ReadUInt16();
                Debug.WriteLineIf(trace, String.Format("B1: {0:x}", versionByte));
                Debug.WriteLineIf(trace, String.Format("B2: {0:x}", someExtras));
                Debug.WriteLineIf(trace, $"DLC Flags: {Convert.ToString(dlcFlags1, 2).PadLeft(16, '0')} (0x{dlcFlags1:x})");
                Debug.WriteLineIf(trace, $"Flags2   : {Convert.ToString(flags2, 2).PadLeft(16, '0')} (0x{flags2:x})");


                var dlcFlags = Enum.GetValues(typeof(DlcFlags)).Cast<DlcFlags>();
                foreach (var dlcFlag in dlcFlags)
                {
                    if ((((DlcFlags)dlcFlags1) & dlcFlag) == dlcFlag)
                    {
                        var hash = br.ReadUInt32();
                        Debug.WriteLineIf(trace, $"{dlcFlag.ToString().PadRight(10,' ')}({Convert.ToString((ushort)dlcFlag, 2).PadLeft(16, '0')}) DlcHash: {hash:x}");
                    }
                }

                //file.Seek(18, SeekOrigin.Begin);
                Debug.WriteLineIf(trace, String.Format("Position: {0}", file.Position));

                Debug.WriteIf(trace, "ModCount: ");
                int modCount = br.ReadByte();

                Debug.WriteLineIf(trace, modCount);
                var modNr = 0;
                var idx = 0;
                while (modNr < modCount && file.Position < file.Length)
                {
                    modNr++;

                    string modName = null;


                    Debug.WriteLineIf(trace, "");
                    Debug.WriteLineIf(trace, $"Mod {modNr}");

                    var modHash = br.ReadUInt32();
                    var pubId = ReadPublisherId(br);

                    byte modNameLength = br.ReadByte();

                    file.Position -= 1;

                    if (file.Position + modNameLength > file.Length)
                    {
                        break;
                    }

                    modName = br.ReadString();

                    Debug.WriteLineIf(trace, $"Mod StrLen: {modNameLength}");
                    var rule = new SteamServerRule(modHash, pubId, modName)
                    {
                        //Key = $"sigNames:{count}-{modCount}"
                        Key = $"modNames:{modNr}-{modCount}"
                    };
                    yield return rule;
                    Debug.WriteLineIf(trace, rule);
                    Debug.WriteLineIf(writeAssertCode, $"Assert.AreEqual(\"{rule.Key}\", array[{idx}].Key);");
                    Debug.WriteLineIf(writeAssertCode, $"Assert.AreEqual(\"{rule.Name}\", array[{idx}].Name);");
                    Debug.WriteLineIf(writeAssertCode, "");
                    idx++;
                }

                // sigNames
                if (file.Position < file.Length)
                {
                    modCount = br.ReadByte();
                    modNr = 0;
                    while (modNr < modCount && file.Position < file.Length)
                    {
                        modNr++;
                        byte modNameLength = br.ReadByte();

                        if (file.Position + modNameLength > file.Length)
                        {
                            break;
                        }

                        string modName = CharEncoding.GetString(br.ReadBytes(modNameLength));

                        Debug.WriteLineIf(trace, $"Mod StrLen: {modNameLength}");
                        var rule = new SteamServerRule(0, 0, modName)
                        {
                            Key = $"sigNames:{modNr}-{modCount}"
                        };
                        yield return rule;
                        Debug.WriteLineIf(trace, rule);
                        Debug.WriteLineIf(writeAssertCode, $"Assert.AreEqual(\"{rule.Key}\", array[{idx}].Key);");
                        Debug.WriteLineIf(writeAssertCode, $"Assert.AreEqual(\"{rule.Name}\", array[{idx}].Name);");
                        Debug.WriteLineIf(writeAssertCode, "");
                        idx++;
                    }
                }

                Debug.WriteLineIf(writeAssertCode, $"Assert.AreEqual({idx}, array.Length);");
                file.Dispose();
            }
        }

        
        [Flags]
        private enum  DlcFlags : ushort
        {
            // ReSharper disable UnusedMember.Local
            Dlc1 = 1,
            Dlc2 = 1 << 1,
            Dlc3 = 1 << 2,
            Dlc4 = 1 << 3,
            Dlc5 = 1 << 4,
            Dlc6 = 1 << 5,
            Dlc7 = 1 << 6,
            Dlc8 = 1 << 7,
            Dlc9 = 1 << 8,
            Dlc10 = 1 << 9,
            Dlc11 = 1 << 10,
            Dlc12 = 1 << 11,
            Dlc13 = 1 << 12,
            Dlc14 = 1 << 13,
            Dlc15 = 1 << 14,
            Dlc16 = 1 << 15 
            // ReSharper restore UnusedMember.Local
        }

        private static uint ReadPublisherId(BinaryReader reader)
        {
            byte len = reader.ReadByte();
            if (len == 1)
            {
                return reader.ReadByte();
            }
            return reader.ReadUInt32();
        }

        private static string GetValue(string keyWord, Dictionary<string, string> dic)
        {
            var sb = new StringBuilder();
            var keyWordLen = keyWord.Length;
            var key = dic.Keys.FirstOrDefault(k => k.Length >= keyWordLen && keyWord == k.Substring(0, keyWordLen));
            if (key != null)
            {
                var idx = key.LastIndexOf('-') + 1;
                if (Int32.TryParse(key.Substring(idx, key.Length - idx), out var count))
                {
                    for (int i = 0; i < count; i++)
                    {
                        string value;
                        if (dic.TryGetValue(string.Format("{0}:{1}-{2}", keyWord, i + 1, count), out value))
                        {
                            sb.Append(value);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        class ServerQueryResponse
        {

            public ServerQueryResponse()
            {
                Keywords = string.Empty;
                KeyValues = new Dictionary<string, string>();
            }

            // ReSharper disable NotAccessedField.Local InconsistentNaming
            public byte ProtocolVersion;
            public string GameServerName;
            public string Map;
            
            public string Folder;
            public string Game;
            
            public short ID;
            public byte CurrentPlayerCount;
            public byte MaxPlayerCount;
            public byte CurrentBotsCount;
            public ServerQueryRequestType ServerType;
            public bool Password;
            
            public bool VAC;
            public bool Mode;
            public string Version;

            public Int32 GamePort;
            public byte[] Data;
            public long ServerSteamId;

            public string Keywords;

            public long GameID;
            // ReSharper restore NotAccessedField.Local InconsistentNaming
            public int Ping;

            public Dictionary<string, string> KeyValues { get; private set; }

            public class Player : ISteamGameServerPlayer
            {
                public string Name { get; set; }

                public Int32 Score { get; set; }

                public TimeSpan OnlineTime { get; set; }

                public byte Idx { get; set; }
            }
            
            public List<Player> Players { get; internal set; }

            public string IpAdresse { get; set; }
        }

        enum ServerQueryRequestType
        {
            // ReSharper disable UnusedMember.Local
            DedicatedServer = 0x64, // 'd'
            NonDedicatedServer = 0x6C, // 'l'
            RelayServer = 0x70, // 'p'
            // ReSharper restore UnusedMember.Local
        }

        // ReSharper disable once InconsistentNaming
        private static bool IsNULL(byte b)
        {
            return b == Byte.MinValue;
        }
    }

    public interface ISteamGameServerPlayer
    {
        byte Idx { get; }

        string Name { get; }

        Int32 Score { get; }

        TimeSpan OnlineTime { get; }

    }

    public interface ISteamGameServer
    {

        string Gamename { get; }
        string Mode { get; }
        string Name { get; }
        string Mission { get; }
        string Country { get; }
        System.Net.IPAddress Host { get; }
        int Port { get; }
        /// <summary>
        /// QueryPort für Serverstates
        /// </summary>
        int QueryPort { get; set; }

        string Version { get; }
        string Mods { get; }
        string Modhashs { get; }
        /// <summary>
        /// maximale Spieleranzahl
        /// </summary>
        int MaxPlayers { get; set; }
        int CurrentPlayerCount { get; }
        string Map { get; }
        string Signatures { get; }
        bool Passworded { get; }

        int Ping { get; }
        
        IEnumerable<ISteamGameServerPlayer> Players { get; }

        string Keywords { get; }

        bool VerifySignatures { get; }
    }
}
