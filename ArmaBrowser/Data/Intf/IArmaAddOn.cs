using System;
using System.Collections.Generic;
using ArmaBrowser.Logic;


namespace ArmaBrowser.Data
{
    interface IArmaAddon
    {
        string Name { get; }
        string ModName { get; }
        string DisplayText { get; }
        string Version { get; }
        IEnumerable<AddonKey> KeyNames { get; }
        string Path { get; }
    }

    class AddonKey
    {
        public AddonKey()
        {
            PubK = new byte[0];
            Hash = PubK.ToBase64().ComputeSha1Hash();
        }

        public string Name { get; set; }
        public byte[] PubK { get; set; }
        public string Hash { get; set; }

        public override string ToString()
        {
            return Name;
        }

        //public static bool operator ==(AddonKey a, string b)
        //{
        //    // If one is null, but not both, return false.
        //    if (((object)a == null) || ((object)b == null))
        //    {
        //        return false;
        //    }

        //    return a.Name == b;
        //}

        //public static bool operator ==(string b, AddonKey a)
        //{
        //    // If one is null, but not both, return false.
        //    if (((object)a == null) || ((object)b == null))
        //    {
        //        return false;
        //    }

        //    return a.Name == b;
        //}

        //public static bool operator !=(AddonKey a, string b)
        //{
        //    return (a == null ? null : a.Name) != b;
        //}

        //public static bool operator !=(string b, AddonKey a)
        //{
        //    return (a == null ? null : a.Name) != b;
        //}
    }
}
