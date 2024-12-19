using System.Collections.Generic;
using System.Linq;

namespace ArmaBrowser.Data.DefaultImpl
{
    internal class ArmaAddon : IArmaAddon
    {
        public ArmaAddon()
        {
            KeyNames = Enumerable.Empty<AddonKey>();
        }

        public string Name { get; internal set; }

        public string DisplayText { get; internal set; }

        public string Version { get; internal set; }

        public string Path { get; internal set; }

        public string ModName { get; set; }

        public IEnumerable<AddonKey> KeyNames { get; set; }
    }
}