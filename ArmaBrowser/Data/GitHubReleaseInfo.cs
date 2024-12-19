using System;
// ReSharper disable InconsistentNaming

namespace ArmaBrowser.Data
{
    internal class GitHubReleaseInfo
    {
        public string tag_name { get; set; }

        public bool draft { get; set; }

        public GitHubAuthor author { get; set; }

        public GitHubAssets[] assets { get; set; }

        public string body { get; set; }

        public DateTime published_at { get; set; }
    }
}