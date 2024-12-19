using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using ArmaBrowser.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ArmaBrowser.ViewModel
{
    internal class UpdateAvailableViewModel : ObjectNotify
    {
        private static readonly string TempBaseDirectory;
        private static readonly string UpdateInfoFilepath;
        private static readonly string CurrentVersion;
        private static readonly string AppName;
        private static readonly string AppInstallDirectoryPath;

        static UpdateAvailableViewModel()
        {
            var name = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName();
            AppInstallDirectoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(UpdateAvailableViewModel)).Location);
            AppName = name.Name; 
            CurrentVersion = name.Version.ToString();
            TempBaseDirectory = Path.Combine(Path.GetTempPath(), "armabrowserupdates");
            UpdateInfoFilepath = Path.Combine(TempBaseDirectory, "updateinfo.json");
        }

        internal async Task CheckForNewReleases()
        {
            var name = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName();
            var currentVersion = name.Version.ToString();

            string releasesJson = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://api.github.com/");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(name.Name, currentVersion));
                client.DefaultRequestHeaders.Accept.Clear();

                using (var response = await client.GetAsync("repos/sonabit/armabrowser/releases"))
                {
                    if (response.IsSuccessStatusCode)
                        releasesJson = await response.Content.ReadAsStringAsync();
                }
            }
            if (!string.IsNullOrEmpty(releasesJson))
                try
                {
                    var gitHubReleases =
                        JsonConvert.DeserializeObject<GitHubReleaseInfo[]>(releasesJson, new ExpandoObjectConverter());

                    var gitHubReleaseInfo = gitHubReleases.OrderByDescending(r => r.published_at)
                        .FirstOrDefault(info => info.assets.Any(a => a.browser_download_url.EndsWith(".zip")));
                    if (string.IsNullOrEmpty(gitHubReleaseInfo?.tag_name))
                        return;
                    var lastGithubVersion =
                        gitHubReleaseInfo.tag_name.Substring(
                            gitHubReleaseInfo.tag_name.LastIndexOf("_", StringComparison.OrdinalIgnoreCase) + 1);

                    if (IsNewerVersion(lastGithubVersion, currentVersion))
                    {
                        await DownloadUpdateAsync(gitHubReleaseInfo);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
        }

        private static bool IsNewerVersion(string newVersion, string currentVersion)
        {
            return new Version(newVersion) > new Version(currentVersion);
        }

        public static bool ExistNewUpdate()
        {
            try
            {
                if (!File.Exists(UpdateInfoFilepath))
                    return false;
                UpdateInfo updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(File.ReadAllText(UpdateInfoFilepath));
                return IsNewerVersion(updateInfo.version, CurrentVersion) 
                    && File.Exists(updateInfo.updaterFilepath);
            }
            catch (Exception )
            {

                return false;
            }
        }

        public static void RunUpdate()
        {
            try
            {
                if (!File.Exists(UpdateInfoFilepath))
                    return ;
                UpdateInfo updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(File.ReadAllText(UpdateInfoFilepath));
                if (!IsNewerVersion(updateInfo.version, CurrentVersion))
                    return;
                if (File.Exists(updateInfo.updaterFilepath)
                    && File.Exists(updateInfo.updaterFilepath)
                    && File.Exists(Path.Combine(TempBaseDirectory, updateInfo.packageFilepath)))
                {
                    ProcessStartInfo ps =
                        new ProcessStartInfo(updateInfo.updaterFilepath, $"{updateInfo.packageFilepath} {AppInstallDirectoryPath} --wait-exit-pid {Process.GetCurrentProcess().Id}")
                        {
                             UseShellExecute = false,
                             CreateNoWindow = false,
                             WorkingDirectory = TempBaseDirectory
                        };
                    Process.Start(ps);
                    Environment.Exit(0);
                }
            }
            catch (Exception)
            {
                // ignore all
            }
        }

        private async Task DownloadUpdateAsync(GitHubReleaseInfo gitHubReleaseInfo)
        {
            var uri = gitHubReleaseInfo.assets.FirstOrDefault(a => a.browser_download_url.EndsWith(".zip"))
                ?.browser_download_url;
            if (string.IsNullOrEmpty(uri))
                return;
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(uri);
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(AppName, CurrentVersion));
                    client.DefaultRequestHeaders.Accept.Clear();

                    using (var response = await client.GetAsync(uri))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string tempArchiveDirectory = Path.Combine(TempBaseDirectory, gitHubReleaseInfo.tag_name);
                            string relativeArchiveFilePath = Path.Combine(gitHubReleaseInfo.tag_name, client.BaseAddress.Segments.Last());
                            string tempArchiveFileName = Path.Combine(TempBaseDirectory, relativeArchiveFilePath);

                            Directory.CreateDirectory(tempArchiveDirectory);
                            using (var fileStream = File.Open(tempArchiveFileName, FileMode.OpenOrCreate))
                            {
                                fileStream.SetLength(0);
                                await response.Content.CopyToAsync(fileStream);
                            }
                            using (ZipArchive zipArchive = ZipFile.OpenRead(tempArchiveFileName))
                            {
                                var zipArchiveEntry = zipArchive.Entries.FirstOrDefault(
                                    entry => entry.FullName.EndsWith("ArmaBrowserUpdater.exe",
                                        StringComparison.OrdinalIgnoreCase));
                                if (zipArchiveEntry?.FullName != null)
                                {
                                    // ReSharper disable once AssignNullToNotNullAttribute
                                    using (var entryStream = zipArchiveEntry.Open())
                                    using (var fileStream = File.OpenWrite(Path.Combine(tempArchiveDirectory,
                                        Path.GetFileName(zipArchiveEntry.FullName))))
                                    {
                                        fileStream.SetLength(0);
                                        await entryStream.CopyToAsync(fileStream);
                                    }
                                }
                                else
                                {
                                    if (AppInstallDirectoryPath != null && File.Exists(Path.Combine(AppInstallDirectoryPath, "ArmaBrowserUpdater.exe")))
                                    {
                                        File.Copy(Path.Combine(AppInstallDirectoryPath, "ArmaBrowserUpdater.exe"),
                                            Path.Combine(tempArchiveDirectory, "ArmaBrowserUpdater.exe"), true);
                                    }
                                }
                                
                                UpdateInfo updateInfo = new UpdateInfo
                                {
                                    updaterFilepath = Path.Combine(tempArchiveDirectory, "ArmaBrowserUpdater.exe"),
                                    version = gitHubReleaseInfo.tag_name,
                                    draft = gitHubReleaseInfo.draft,
                                    packageFilepath = relativeArchiveFilePath
                                };
                                File.WriteAllText(Path.Combine(TempBaseDirectory, "updateinfo.json"), JsonConvert.SerializeObject(updateInfo));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    internal class UpdateInfo
    {
        public string updaterFilepath { get; set; }

        public string version { get; set; }

        public string packageFilepath { get; set; }

        public bool draft { get; internal set; }
    }
}