namespace ArmaBrowser.Data
{
    internal interface IArma3DataRepository
    {
        string GetArma3Folder();

        IArmaAddon[] GetInstalledAddons(string baseFolder);

        //IServerRepository ServerListManager();
    }
}