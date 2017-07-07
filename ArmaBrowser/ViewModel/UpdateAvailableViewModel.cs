﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ArmaBrowser.ViewModel
{
    internal class UpdateAvailableViewModel : ObjectNotify
    {
        private bool _isNewVersionAvailable;
        private string _newVersion;

        internal async Task CheckForUpdates()
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
                    {
                        releasesJson = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            if (!string.IsNullOrEmpty(releasesJson))
            {
                try
                {
                    var gitHubReleases = JsonConvert.DeserializeObject<GitHubReleaseInfo[]>(releasesJson,
                        new ExpandoObjectConverter());
                    var gitHubReleaseInfo = gitHubReleases.OrderByDescending(r => r.published_at)
                        .FirstOrDefault(info => info.assets.Any(a => a.browser_download_url.EndsWith(".zip")));
                    if (string.IsNullOrEmpty(gitHubReleaseInfo?.tag_name))
                    {
                        return;
                    }
                    var lastGithubVersion = gitHubReleaseInfo.tag_name.Substring(gitHubReleaseInfo.tag_name.LastIndexOf("_", StringComparison.OrdinalIgnoreCase) + 1);

                    if (new Version(lastGithubVersion) > new Version(currentVersion))
                    {
                        NewVersion = lastGithubVersion;
                        await DownloadUpdateAsync(gitHubReleaseInfo);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private async Task DownloadUpdateAsync(GitHubReleaseInfo gitHubReleaseInfo)
        {
            var uri = gitHubReleaseInfo.assets.FirstOrDefault(a => a.browser_download_url.EndsWith(".zip"))?.browser_download_url;
            if (string.IsNullOrEmpty(uri))
            {
                return;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(uri);
                    var name = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName();
                    var currentVersion = name.Version.ToString();
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(name.Name, currentVersion));
                    client.DefaultRequestHeaders.Accept.Clear();

                    using (var response = await client.GetAsync(uri))
                        if (response.IsSuccessStatusCode)
                        {
                            string temp = Path.Combine(System.IO.Path.GetTempPath(), "armabrowserupdates", gitHubReleaseInfo.tag_name);
                            Directory.CreateDirectory(temp);
                            using (var fileStream =
                                File.Open(
                                    Path.Combine(temp, client.BaseAddress.Segments.Last()), FileMode.OpenOrCreate))
                            {
                                fileStream.SetLength(0);
                                await response.Content.CopyToAsync(fileStream);
                            }
                        }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public bool IsNewVersionAvailable
        {
            get { return _newVersion != null; }
        }

        public string NewVersion
        {
            get { return _newVersion; }
            private set
            {
                if (value == _newVersion) return;
                _newVersion = value;
                OnPropertyChanged(nameof(IsNewVersionAvailable));
                OnPropertyChanged();
            }
        }
    }



    class GitHubReleaseInfo
    {
        public string tag_name { get; set; }

        public bool draft { get; set; }

        public GitHubAuthor author { get; set; }

        public GitHubAssets[] assets { get; set; }

        public string body { get; set; }

        public DateTime published_at { get; set; }
    }

    class GitHubAuthor
    {
        public int id { get; set; }

        public string login { get; set; }

    }

    class GitHubAssets
    {
        public string browser_download_url { get; set; }

        public GitHubAuthor uploader { get; set; }

        public int size { get; set; }

    }
}