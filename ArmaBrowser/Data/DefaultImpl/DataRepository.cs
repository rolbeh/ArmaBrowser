using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ArmaBrowser.Logic;
using Microsoft.Win32;

namespace ArmaBrowser.Data.DefaultImpl
{
    internal sealed class DataRepository : IArma3DataRepository
    {
        /// <summary>
        ///     Get the path of installation of Arma3
        /// </summary>
        /// <remarks>For the determination the Steam config files will used.</remarks>
        /// <returns>The path of the installation of Arma3, or an emty string</returns>
        public string GetArma3Folder()
        {
            try
            {
                Logger.Default.Push(@"SteamPath - ");
                var steamFolder = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", "");
                if (steamFolder != null)
                {
                    Logger.Default.PushLine(@"found");

                    // Test Standartinstallion
                    {
                        Logger.Default.Push(@"Arma 3 in default location - ");
                        var testPath = Path.Combine(steamFolder.ToString(), "SteamApps", "common", "ARMA 3", "arma3.exe");
                        if (File.Exists(testPath))
                        {
                            Logger.Default.PushLine(@"found");
                            return Path.GetDirectoryName(testPath);
                        }
                        Logger.Default.PushLine(@"not found");
                    }


                    // Multi-Locations 
                    Logger.Default.Push(@"steam library config - ");
                    var libraryConfigPath = Path.Combine(steamFolder.ToString(), "SteamApps", "libraryfolders.vdf");
                    if (File.Exists(libraryConfigPath))
                    {
                        Logger.Default.PushLine(@"found");
                        using (var reader = new SteamConfigReader(libraryConfigPath))
                        {
                            var xml = reader.ToXml();
                            Logger.Default.Push(@"Arma 3 in library location - ");
                            if (xml.DocumentElement != null)
                            {
                                foreach (var item in xml.DocumentElement.ChildNodes.Cast<XmlElement>())
                                {
                                    var valueNode = item.ChildNodes.OfType<XmlText>().FirstOrDefault();
                                    if (valueNode != null)
                                    {
                                        var folder = valueNode.Value;

                                        var testPath = Path.Combine(folder, "SteamApps", "common", "ARMA 3", "arma3.exe");
                                        if (File.Exists(testPath))
                                        {
                                            Logger.Default.PushLine(@"found");
                                            return Path.GetDirectoryName(testPath);
                                        }
                                    }
                                }
                            }
                            Logger.Default.PushLine(@"not found");
                        }
                    }
                    else
                        Logger.Default.PushLine(@"not found");

                    //var steamPath = System.IO.Path.Combine(steamFolder.ToString(), "config", "config.vdf");
                    //Logger.Default.Push(@"Steam config file "+ steamPath + " - ");
                    //if (!File.Exists(steamPath))
                    //{
                    //    Helper.Logger.Default.PushLine(@"not found");
                    //    return null;
                    //}
                    //Logger.Default.PushLine(@"found");
                    //using (var reader = new SteamConfigReader(steamPath))
                    //{
                    //    var baseInstallFolder_1 = reader.GetValueOf("\t\t\t\t\"BaseInstallFolder_1\"");
                    //    return Path.Combine(baseInstallFolder_1, "SteamApps", "common", "ARMA 3");
                    //}
                }
                else
                    Logger.Default.PushLine(@"not found");
                return "";
            }
            catch (Exception ex)
            {
                Logger.Default.PushLine(@":-( " + ex.GetType().Name);
            }
            return "";
        }

        public IArmaAddon[] GetInstalledAddons(string baseFolder)
        {
            if (string.IsNullOrWhiteSpace(baseFolder)) return new IArmaAddon[0];

            var addonFolders = Directory.EnumerateDirectories(baseFolder, "@*").ToArray();
            var result = new List<IArmaAddon>(addonFolders.Length);
            foreach (var addonFolder in addonFolders)
            {
                var item = new ArmaAddon
                {
                    Name = Path.GetFileName(addonFolder),
                    ModName = Path.GetFileName(addonFolder),
                    DisplayText = Path.GetFileName(addonFolder)?.Replace("@", ""),
                    Path = addonFolder
                };
                var addonModcpp = Path.Combine(addonFolder, "mod.cpp");
                if (File.Exists(addonModcpp))
                {
                    using (var reader = new StreamReader(addonModcpp))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (line != null && line.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                            {
                                var fQ = line.IndexOf('"') + 1;
                                var lQ = line.LastIndexOf('"');
                                item.ModName = line.Substring(fQ, lQ - fQ);
                                item.DisplayText = item.ModName;
                            }
                        }
                    }
                }

                // reading bisign files
                var addonFileFolder = Path.Combine(addonFolder, "addons");
                if (Directory.Exists(addonFileFolder))
                {
                    var keys = new List<AddonKey>(200);
                    var sb = new StringBuilder();
                    var bisignPaths = Directory.EnumerateFiles(addonFileFolder, "*.bisign");
                    foreach (var bisignPath in bisignPaths)
                    {
                        if (File.Exists(bisignPath))
                        {
                            try
                            {
                                using (
                                    var bisignStream = new FileStream(bisignPath, FileMode.Open, FileAccess.Read,
                                        FileShare.ReadWrite, 10))
                                using (var br = new BinaryReader(bisignStream, Encoding.ASCII, false))
                                {
                                    sb.Clear();
                                    while (bisignStream.Position < bisignStream.Length && br.PeekChar() > 0)
                                    {
                                        sb.Append(br.ReadChar());
                                    }

                                    var keyLen = br.ReadInt32();
                                    var bytes = br.ReadBytes(keyLen);

                                    keys.Add(new AddonKey
                                    {
                                        Name = sb.ToString(),
                                        PubK = bytes,
                                        Hash = bytes.ToBase64().ComputeSha1Hash()
                                    });
                                    break;
                                }
                            }
                            catch
                            {
                                // ignore all erros
                            }
                        }
                    }
                    if (keys.Count > 0)
                        item.KeyNames = keys.Distinct().ToArray();
                }


                //var addonKeyFolder = addonFolder; // Path.Combine(addonFolder, "keys");
                //if (Directory.Exists(addonKeyFolder))
                //{
                //    var addonKeyFiles = Directory.EnumerateFiles(addonKeyFolder, "*.bikey", SearchOption.AllDirectories).ToArray();
                //    item.KeyNames = addonKeyFiles.Select(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f))).ToArray();
                //}
                //addonKeyFolder = Path.Combine(addonFolder, "key");
                //if (Directory.Exists(addonKeyFolder))
                //{
                //    var addonKeyFiles = Directory.EnumerateFiles(addonKeyFolder, "*.bikey").ToArray();
                //    item.KeyNames = addonKeyFiles.Select(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f))).ToArray();
                //}

                result.Add(item);
            }
            return result.ToArray();
        }

        //public ISteamGameServer[] GetServerList()
        //{
        //    throw new NotImplementedException();
        //    //const string url = "http://arma3.swec.se/server/list.xml";
        //    //try
        //    //{
        //    //    using (var webClient = new WebClient())
        //    //    {
        //    //        var bytes = webClient.DownloadData(url);
        //    //        using (var mem = new MemoryStream(bytes, false))
        //    //        {
        //    //            return ParseArmaSwecServerlist.GetServerList(mem);
        //    //        }
        //    //    }
        //    //}
        //    //catch
        //    //{
        //    //    if (File.Exists("Serverlist.xml"))
        //    //    {
        //    //        using (var fs = new FileStream("Serverlist.xml", FileMode.Open))
        //    //        {
        //    //            return ParseArmaSwecServerlist.GetServerList(fs);
        //    //        }
        //    //    }
        //    //    return new IServerVo[0];
        //    //}
        //}

        //    return new ServerRepositoryStream();
        //{

        //public IServerRepository ServerListManager()
        //}
    }
}