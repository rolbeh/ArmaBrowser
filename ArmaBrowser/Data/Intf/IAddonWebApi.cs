using System.Collections.Generic;
using System.Threading.Tasks;
using ArmaBrowser.Logic;

namespace ArmaBrowser.Data
{
    internal interface IAddonWebApi
    {
        Task PostInstalledAddonsKeysAsync(IEnumerable<IAddon> addons);
    }
}