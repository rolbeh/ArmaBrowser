using System.Linq;
using ArmaBrowser.Data;
using ArmaBrowser.Data.DefaultImpl;
using Magic.Steam;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationsTest.ArmaBrowser
{
    [TestClass]
    public class ServerRepositoryTests
    {
        [TestMethod, TestCategory("onlylocal")]
        public void GetServerListTest()
        {
            //given 
            IServerRepository serverRepository = new Arma3ServerRepositorySteam();

            // when 
            ISteamGameServer[] serverList = serverRepository.DiscoverServer().ToArray();

            // Then

            Assert.IsTrue(serverList.Length > 0);
        }
    }
}
