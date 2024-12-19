using System;
using System.IO;

namespace ArmaBrowser.Logic
{
    internal sealed class AppPathService
    {
        public string UserSettingsPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile,
                    Environment.SpecialFolderOption.DoNotVerify), "ArmaBrowser");

        // ReSharper disable once UnusedMember.Global
        public void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}