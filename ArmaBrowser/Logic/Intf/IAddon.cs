using System;
using System.Collections.Generic;
using ArmaBrowser.Data;

namespace ArmaBrowser.Logic
{
    internal interface IAddon
    {
        string Name { get; }
        string ModName { get; }
        string DisplayText { get; }
        string Version { get; }
        // ReSharper disable once UnusedMemberInSuper.Global
        string Path { get; }
        bool IsActive { get; set; }
        bool CanActived { get; set; }
        long ActivationOrder { get; }
        IEnumerable<AddonKey> KeyNames { get; }
        bool IsInstalled { get; }
        // ReSharper disable once UnusedMember.Global
        IEnumerable<Uri> DownlandUris { get; }

        // ReSharper disable once UnusedMember.Global
        bool IsInstallable { get; }
        bool IsArmaDefaultPath { get; }
        string CommandlinePath { get; }
        bool? IsEasyInstallable { get; set; }
    }
}