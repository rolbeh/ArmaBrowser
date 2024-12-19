namespace ArmaBrowser.Logic
{
    internal class ModInstallPath
    {
        public ModInstallPath(string path, bool isDefault)
        {
            Path = path;
            IsDefault = isDefault;
        }

        public string Path { get; }

        public bool IsDefault { get; }
    }
}