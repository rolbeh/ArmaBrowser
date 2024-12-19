namespace ArmaBrowser.Data.DefaultImpl.Rest
{
    internal class RestAddon
    {
        public string Name { get; set; }
        public string ModName { get; set; }
        public string DisplayText { get; set; }
        public string Version { get; set; }
        public RestAddonKey[] Keys { get; set; }
    }
}