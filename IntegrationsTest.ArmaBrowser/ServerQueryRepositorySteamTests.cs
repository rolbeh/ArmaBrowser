using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Magic.Steam;
using Magic.Steam.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationsTest.ArmaBrowser
{
    [TestClass]
    [DeploymentItem("TestData", "TestData")]
    public class ServerQueryRepositorySteamTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ReadRules_Version_134787_176_77_11_19()
        {
            using (FileStream unframedFile = File.OpenRead(@"TestData\ServerRules\V_1.56.134787_176.77.11.19.rdefrag"))
            using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
            {
                var array = ServerQueries.ReadRuleFile(data, true).ToArray();

                Assert.AreEqual("modNames:1-13", array[0].Key);
                Assert.AreEqual("Arma 3", array[0].Name);

                Assert.AreEqual("modNames:2-13", array[1].Key);
                Assert.AreEqual("@bwa3_comp_ace", array[1].Name);

                Assert.AreEqual("modNames:3-13", array[2].Key);
                Assert.AreEqual("Advanced Combat Environment 3.5.1", array[2].Name);

                Assert.AreEqual("modNames:4-13", array[3].Key);
                Assert.AreEqual("Bundeswehr Mod", array[3].Name);

                Assert.AreEqual("modNames:5-13", array[4].Key);
                Assert.AreEqual("RHS: Armed Forces of the Russian Federation", array[4].Name);

                Assert.AreEqual("modNames:6-13", array[5].Key);
                Assert.AreEqual("@rhs_usf3", array[5].Name);

                Assert.AreEqual("modNames:7-13", array[6].Key);
                Assert.AreEqual("CUP Terrains - Maps 1.0.1", array[6].Name);

                Assert.AreEqual("modNames:8-13", array[7].Key);
                Assert.AreEqual("CUP Terrains - Core 1.0.1", array[7].Name);

                Assert.AreEqual("modNames:9-13", array[8].Key);
                Assert.AreEqual("JSRS3: DragonFyre EDEN 1.2", array[8].Name);

                Assert.AreEqual("modNames:10-13", array[9].Key);
                Assert.AreEqual("Blastcore: Skies", array[9].Name);

                Assert.AreEqual("modNames:11-13", array[10].Key);
                Assert.AreEqual("@blastcore_a3", array[10].Name);

                Assert.AreEqual("modNames:12-13", array[11].Key);
                Assert.AreEqual("MCC Sandbox", array[11].Name);

                Assert.AreEqual("modNames:13-13", array[12].Key);
                Assert.AreEqual("Community Base Addons v2.3.1", array[12].Name);
            }
        }

        [TestMethod]
        public void ReadRules_Version_134627_188_165_245_178()
        { 
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.56.134627_188.165.245.178.rdecoded")))
            {
                array = ServerQueries.ReadRuleFile(file,true).ToArray();
            }
            Assert.AreEqual(89, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_134787_188_165_32_82()
        {
            SteamServerRule[] array;
            using (FileStream unframedFile = File.OpenRead(@"TestData\ServerRules\V_1.56.134787_188.165.32.82.rdefrag"))
            using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
            {
                array = ServerQueries.ReadRuleFile(data, true).ToArray();
            }
            Assert.AreEqual(18, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_134787_90_116_171_48()
        {
            SteamServerRule[] array;
            using (FileStream unframedFile = File.OpenRead(@"TestData\ServerRules\V_1.56.134787_90.116.171.48.rdefrag"))
            using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
            {
                array = ServerQueries.ReadRuleFile(data, true).ToArray();
            }
            Assert.AreEqual(11, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_134787_62_141_38_102()
        {
            SteamServerRule[] array;
            using (FileStream unframedFile = File.OpenRead(@"TestData\ServerRules\V_1.56.134787_62.141.38.102.rdefrag"))
            using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
            {
                array = ServerQueries.ReadRuleFile(data, true).ToArray();
            }
            Assert.AreEqual(9, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_134787_5_9_74_118()
        {
            SteamServerRule[] array;
            using (FileStream unframedFile = File.OpenRead(@"TestData\ServerRules\V_1.56.134787_5.9.74.118.rdefrag"))
            using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
            {
                array = ServerQueries.ReadRuleFile(data, true).ToArray();
            }
            Assert.AreEqual(32, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_135288_81_0_236_102()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.59.135288_81.0.236.102.rules")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }
            Assert.AreEqual(0, array.Length);
        }

        [TestMethod]
        public void ReadRules_Version_134787_87_229_120_234()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.56.134787_87.229.120.234.rdecoded")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }

            Assert.AreEqual("modNames:1-48", array[0].Key);
            Assert.AreEqual("TRYK's Multi-Play Unifrom's pack", array[0].Name);

            Assert.AreEqual("modNames:2-48", array[1].Key);
            Assert.AreEqual("Task Force Arrowhead Radio 0.9.8", array[1].Name);

            Assert.AreEqual("modNames:3-48", array[2].Key);
            Assert.AreEqual("@sthud_a3", array[2].Name);

            Assert.AreEqual("modNames:4-48", array[3].Key);
            Assert.AreEqual("SMA", array[3].Name);

            Assert.AreEqual("modNames:5-48", array[4].Key);
            Assert.AreEqual("Ryan's Zombies & Demons", array[4].Name);

            Assert.AreEqual("modNames:6-48", array[5].Key);
            Assert.AreEqual("@rhs_usf3", array[5].Name);

            Assert.AreEqual("modNames:7-48", array[6].Key);
            Assert.AreEqual("RHS: Armed Forces of the Russian Federation", array[6].Name);

            Assert.AreEqual("modNames:8-48", array[7].Key);
            Assert.AreEqual("@rh_pistol_a3", array[7].Name);

            Assert.AreEqual("modNames:9-48", array[8].Key);
            Assert.AreEqual("@rds_civpack", array[8].Name);

            Assert.AreEqual("modNames:10-48", array[9].Key);
            Assert.AreEqual("@rds_ag_comp", array[9].Name);

            Assert.AreEqual("modNames:11-48", array[10].Key);
            Assert.AreEqual("R3F Armes 3.4", array[10].Name);

            Assert.AreEqual("modNames:12-48", array[11].Key);
            Assert.AreEqual("Kunduz, Afghanistan", array[11].Name);

            Assert.AreEqual("modNames:13-48", array[12].Key);
            Assert.AreEqual("MRT Accessory Functions", array[12].Name);

            Assert.AreEqual("modNames:14-48", array[13].Key);
            Assert.AreEqual("MELB v0.00003", array[13].Name);

            Assert.AreEqual("modNames:15-48", array[14].Key);
            Assert.AreEqual("Leights OPFOR Pack", array[14].Name);

            Assert.AreEqual("modNames:16-48", array[15].Key);
            Assert.AreEqual("konyo's MH-47E Chinook. v1.5", array[15].Name);

            Assert.AreEqual("modNames:17-48", array[16].Key);
            Assert.AreEqual("@hueypack", array[16].Name);

            Assert.AreEqual("modNames:18-48", array[17].Key);
            Assert.AreEqual("@hlcmods_m60e4", array[17].Name);

            Assert.AreEqual("modNames:19-48", array[18].Key);
            Assert.AreEqual("@hlcmods_g3", array[18].Name);

            Assert.AreEqual("modNames:20-48", array[19].Key);
            Assert.AreEqual("@hlcmods_fal", array[19].Name);

            Assert.AreEqual("modNames:21-48", array[20].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[20].Name);

            Assert.AreEqual("modNames:22-48", array[21].Key);
            Assert.AreEqual("@hlcmods_M14", array[21].Name);

            Assert.AreEqual("modNames:23-48", array[22].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[22].Name);

            Assert.AreEqual("modNames:24-48", array[23].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[23].Name);

            Assert.AreEqual("modNames:25-48", array[24].Key);
            Assert.AreEqual("[GGzerosum] M1 Garand", array[24].Name);

            Assert.AreEqual("modNames:26-48", array[25].Key);
            Assert.AreEqual("FHQ Accessories Pack", array[25].Name);

            Assert.AreEqual("modNames:27-48", array[26].Key);
            Assert.AreEqual("@fata_a3", array[26].Name);

            Assert.AreEqual("modNames:28-48", array[27].Key);
            Assert.AreEqual("F/A-18 Super Hornet pack", array[27].Name);

            Assert.AreEqual("modNames:29-48", array[28].Key);
            Assert.AreEqual("@eods", array[28].Name);

            Assert.AreEqual("modNames:30-48", array[29].Key);
            Assert.AreEqual("@em", array[29].Name);

            Assert.AreEqual("modNames:31-48", array[30].Key);
            Assert.AreEqual("CUP Terrains - Maps 1.0.1", array[30].Name);

            Assert.AreEqual("modNames:32-48", array[31].Key);
            Assert.AreEqual("CUP Terrains - Core 1.0.1", array[31].Name);

            Assert.AreEqual("modNames:33-48", array[32].Key);
            Assert.AreEqual("cTab - Blue Force Tracking", array[32].Name);

            Assert.AreEqual("modNames:34-48", array[33].Key);
            Assert.AreEqual("CAF AGGRESSORS v 1.5", array[33].Name);

            Assert.AreEqual("modNames:35-48", array[34].Key);
            Assert.AreEqual("@burneslcac", array[34].Name);

            Assert.AreEqual("modNames:36-48", array[35].Key);
            Assert.AreEqual("Zade's BackpackonChest-Mod", array[35].Name);

            Assert.AreEqual("modNames:37-48", array[36].Key);
            Assert.AreEqual("@asdg_jm", array[36].Name);

            Assert.AreEqual("modNames:38-48", array[37].Key);
            Assert.AreEqual("@asdg_attachments", array[37].Name);

            Assert.AreEqual("modNames:39-48", array[38].Key);
            Assert.AreEqual("Advanced Combat Environment 3.5.1", array[38].Name);

            Assert.AreEqual("modNames:40-48", array[39].Key);
            Assert.AreEqual("75th Ranger Regiment", array[39].Name);

            Assert.AreEqual("modNames:41-48", array[40].Key);
            Assert.AreEqual("TAC Vests (TRYK)", array[40].Name);

            Assert.AreEqual("modNames:42-48", array[41].Key);
            Assert.AreEqual("@HiddenIdentityPackv3", array[41].Name);

            Assert.AreEqual("modNames:43-48", array[42].Key);
            Assert.AreEqual("HAF Units Mod", array[42].Name);

            Assert.AreEqual("modNames:44-48", array[43].Key);
            Assert.AreEqual("@DBMask", array[43].Name);

            Assert.AreEqual("modNames:45-48", array[44].Key);
            Assert.AreEqual("Community Base Addons v2.3.1", array[44].Name);

            Assert.AreEqual("modNames:46-48", array[45].Key);
            Assert.AreEqual("@ASDG_JR", array[45].Name);

            Assert.AreEqual("modNames:47-48", array[46].Key);
            Assert.AreEqual("Arma 3: ALiVE", array[46].Name);

            Assert.AreEqual("modNames:48-48", array[47].Key);
            Assert.AreEqual("31St Support Unit", array[47].Name);
        }

        [TestMethod]
        public void ReadRules_Version_134627_82_211_2_97()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.56.134627_82.211.2.97.rdecoded")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }
            
            Assert.AreEqual("modNames:1-38", array[0].Key);
            Assert.AreEqual("@server", array[0].Name);

            Assert.AreEqual("modNames:2-38", array[1].Key);
            Assert.AreEqual("TAC Vests", array[1].Name);

            Assert.AreEqual("modNames:3-38", array[2].Key);
            Assert.AreEqual("@Realistic Tank Crew", array[2].Name);

            Assert.AreEqual("modNames:4-38", array[3].Key);
            Assert.AreEqual("R_Unfold", array[3].Name);

            Assert.AreEqual("modNames:5-38", array[4].Key);
            Assert.AreEqual("FoxFort Camo Pack", array[4].Name);

            Assert.AreEqual("modNames:6-38", array[5].Key);
            Assert.AreEqual("@BRIDGE_Knocking", array[5].Name);

            Assert.AreEqual("modNames:7-38", array[6].Key);
            Assert.AreEqual("Advanced Towing", array[6].Name);

            Assert.AreEqual("modNames:8-38", array[7].Key);
            Assert.AreEqual("LAxemann's DynaSound", array[7].Name);

            Assert.AreEqual("modNames:9-38", array[8].Key);
            Assert.AreEqual("@mine_detector", array[8].Name);

            Assert.AreEqual("modNames:10-38", array[9].Key);
            Assert.AreEqual("@l_suppress", array[9].Name);

            Assert.AreEqual("modNames:11-38", array[10].Key);
            Assert.AreEqual("LAxemann's Mount", array[10].Name);

            Assert.AreEqual("modNames:12-38", array[11].Key);
            Assert.AreEqual("@hlcmods_g3", array[11].Name);

            Assert.AreEqual("modNames:13-38", array[12].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[12].Name);

            Assert.AreEqual("modNames:14-38", array[13].Key);
            Assert.AreEqual("@hlc_wp_mp5", array[13].Name);

            Assert.AreEqual("modNames:15-38", array[14].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[14].Name);

            Assert.AreEqual("modNames:16-38", array[15].Key);
            Assert.AreEqual("Half-Life Creations Mod Set", array[15].Name);

            Assert.AreEqual("modNames:17-38", array[16].Key);
            Assert.AreEqual("TF47 LAUNCHERS", array[16].Name);

            Assert.AreEqual("modNames:18-38", array[17].Key);
            Assert.AreEqual("@RH_m4", array[17].Name);

            Assert.AreEqual("modNames:19-38", array[18].Key);
            Assert.AreEqual("@RH_acc", array[18].Name);

            Assert.AreEqual("modNames:20-38", array[19].Key);
            Assert.AreEqual("@RDS_Civ", array[19].Name);

            Assert.AreEqual("modNames:21-38", array[20].Key);
            Assert.AreEqual("MELB v0.00003", array[20].Name);

            Assert.AreEqual("modNames:22-38", array[21].Key);
            Assert.AreEqual("@Ares", array[21].Name);

            Assert.AreEqual("modNames:23-38", array[22].Key);
            Assert.AreEqual("@reshmaan", array[22].Name);

            Assert.AreEqual("modNames:24-38", array[23].Key);
            Assert.AreEqual("@POOK", array[23].Name);

            Assert.AreEqual("modNames:25-38", array[24].Key);
            Assert.AreEqual("@HiddenIdentityPackV2", array[24].Name);

            Assert.AreEqual("modNames:26-38", array[25].Key);
            Assert.AreEqual("CUP Weapons ACE Compatibility 1.2", array[25].Name);

            Assert.AreEqual("modNames:27-38", array[26].Key);
            Assert.AreEqual("CUP Weapons 1.5", array[26].Name);

            Assert.AreEqual("modNames:28-38", array[27].Key);
            Assert.AreEqual("CUP Vehicles - 1.2", array[27].Name);

            Assert.AreEqual("modNames:29-38", array[28].Key);
            Assert.AreEqual("CUP Units 1.2.1", array[28].Name);

            Assert.AreEqual("modNames:30-38", array[29].Key);
            Assert.AreEqual("CUP Terrains - Full 1.0.1", array[29].Name);

            Assert.AreEqual("modNames:31-38", array[30].Key);
            Assert.AreEqual("@BWA3_Weapons_ACE", array[30].Name);

            Assert.AreEqual("modNames:32-38", array[31].Key);
            Assert.AreEqual("@bw_addition", array[31].Name);

            Assert.AreEqual("modNames:33-38", array[32].Key);
            Assert.AreEqual("Arma Role Play Objects 2 ", array[32].Name);

            Assert.AreEqual("modNames:34-38", array[33].Key);
            Assert.AreEqual("Advanced Combat Radio Environment 2", array[33].Name);

            Assert.AreEqual("modNames:35-38", array[34].Key);
            Assert.AreEqual("Advanced Combat Environment 3.5.1", array[34].Name);

            Assert.AreEqual("modNames:36-38", array[35].Key);
            Assert.AreEqual("@fata_a3", array[35].Name);

            Assert.AreEqual("modNames:37-38", array[36].Key);
            Assert.AreEqual("Community Base Addons v2.3.1", array[36].Name);

            Assert.AreEqual("modNames:38-38", array[37].Key);
            Assert.AreEqual("Bundeswehr Mod", array[37].Name);
        }

        [TestMethod]
        public void ReadRules_Version_135357_81_19_212_115()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.56.135357_81.19.212.115.rules")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();

                Assert.AreEqual("modNames:1-2", array[0].Key);
                Assert.AreEqual("PLAYERUNKNOWN's Battle Royale for Arma 3", array[0].Name);
                Assert.AreEqual("modNames:2-2", array[1].Key);
                Assert.AreEqual("mark", array[1].Name);
                Assert.AreEqual("sigNames:1-3", array[2].Key);
                Assert.AreEqual("a3", array[2].Name);
                Assert.AreEqual("sigNames:2-3", array[3].Key);
                Assert.AreEqual("Lappihuan", array[3].Name);
                Assert.AreEqual("sigNames:3-3", array[4].Key);
                Assert.AreEqual("PUBattleRoyale.0.8.0.0", array[4].Name);
            }

        }

        [TestMethod]
        public void ReadRules_V_1_70_141838_91_121_245_89()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.70.141838_91.121.245.89.rules")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }

            Assert.AreEqual("sigNames:1-27", array[0].Key);
            Assert.AreEqual("a3", array[0].Name);

            Assert.AreEqual("sigNames:2-27", array[1].Key);
            Assert.AreEqual("badbenson", array[1].Name);

            Assert.AreEqual("sigNames:3-27", array[2].Key);
            Assert.AreEqual("cba_3.2.0.170224-83331e61", array[2].Name);

            Assert.AreEqual("sigNames:4-27", array[3].Key);
            Assert.AreEqual("cba_3.2.1.170227-f25d7113", array[3].Name);

            Assert.AreEqual("sigNames:5-27", array[4].Key);
            Assert.AreEqual("cba_3.3.0.170502-feb4157a", array[4].Name);

            Assert.AreEqual("sigNames:6-27", array[5].Key);
            Assert.AreEqual("cba_3.3.1.170504-c1706f9a", array[5].Name);

            Assert.AreEqual("sigNames:7-27", array[6].Key);
            Assert.AreEqual("cyprus-pushtohear", array[6].Name);

            Assert.AreEqual("sigNames:8-27", array[7].Key);
            Assert.AreEqual("defk0nNL", array[7].Name);

            Assert.AreEqual("sigNames:9-27", array[8].Key);
            Assert.AreEqual("DynaSound2_0", array[8].Name);

            Assert.AreEqual("sigNames:10-27", array[9].Key);
            Assert.AreEqual("DynaSound", array[9].Name);

            Assert.AreEqual("sigNames:11-27", array[10].Key);
            Assert.AreEqual("ES", array[10].Name);

            Assert.AreEqual("sigNames:12-27", array[11].Key);
            Assert.AreEqual("esa", array[11].Name);

            Assert.AreEqual("sigNames:13-27", array[12].Key);
            Assert.AreEqual("eutw", array[12].Name);

            Assert.AreEqual("sigNames:14-27", array[13].Key);
            Assert.AreEqual("eutw_tools", array[13].Name);

            Assert.AreEqual("sigNames:15-27", array[14].Key);
            Assert.AreEqual("EUTW_Tools_2", array[14].Name);

            Assert.AreEqual("sigNames:16-27", array[15].Key);
            Assert.AreEqual("Feint", array[15].Name);

            Assert.AreEqual("sigNames:17-27", array[16].Key);
            Assert.AreEqual("FEINT", array[16].Name);

            Assert.AreEqual("sigNames:18-27", array[17].Key);
            Assert.AreEqual("jsrs-apex-handling", array[17].Name);

            Assert.AreEqual("sigNames:19-27", array[18].Key);
            Assert.AreEqual("jsrs-apex", array[18].Name);

            Assert.AreEqual("sigNames:20-27", array[19].Key);
            Assert.AreEqual("jsrs_soundmod", array[19].Name);

            Assert.AreEqual("sigNames:21-27", array[20].Key);
            Assert.AreEqual("LHM_Glasses2_3", array[20].Name);

            Assert.AreEqual("sigNames:22-27", array[21].Key);
            Assert.AreEqual("L_ES", array[21].Name);

            Assert.AreEqual("sigNames:23-27", array[22].Key);
            Assert.AreEqual("Megas_Sound", array[22].Name);

            Assert.AreEqual("sigNames:24-27", array[23].Key);
            Assert.AreEqual("shacktac", array[23].Name);

            Assert.AreEqual("sigNames:25-27", array[24].Key);
            Assert.AreEqual("stui_1.2.2", array[24].Name);

            Assert.AreEqual("sigNames:26-27", array[25].Key);
            Assert.AreEqual("tools", array[25].Name);

            Assert.AreEqual("sigNames:27-27", array[26].Key);
            Assert.AreEqual("tools2", array[26].Name);

        }

        [TestMethod]
        public void ReadRules_V_1_62_137494_151_80_99_159()
        {
            SteamServerRule[] array;
            using (SteamDecodedBytes file = new SteamDecodedBytes(File.OpenRead(@"TestData\ServerRules\V_1.62.137494_151.80.99.159.rules")))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }
            Assert.AreEqual("modNames:1-3", array[0].Key);
            Assert.AreEqual("Community Base Addons v1.1.23", array[0].Name);

            Assert.AreEqual("modNames:2-3", array[1].Key);
            Assert.AreEqual("Task Force Arrowhead Radio 0.9.8", array[1].Name);

            Assert.AreEqual("modNames:3-3", array[2].Key);
            Assert.AreEqual("UNSUNG 3.0 Charlie", array[2].Name);

        }

        [TestMethod]
        public void ReadRules_V_1_70_141838_31_214_203_70()
        {
            SteamServerRule[] array;

            byte[] rawBytes;
            using (var file = File.OpenRead(@"TestData\ServerRules\V_1.70.141838_31.214.203.70.rdat"))
            {
                rawBytes = new byte[file.Length];
                file.Read(rawBytes, 0, rawBytes.Length);
            }

            var defragmentSteamBytes = rawBytes.DefragmentSteamBytes_1_56();

            using (SteamDecodedBytes file = new SteamDecodedBytes(defragmentSteamBytes.DecodeSteamRuleFile_1_56().Data))
            {
                array = ServerQueries.ReadRuleFile(file, true).ToArray();
            }

            Assert.AreEqual("modNames:1-3", array[0].Key);
            Assert.AreEqual("Extended Base Mod", array[0].Name);

            Assert.AreEqual("modNames:2-3", array[1].Key);
            Assert.AreEqual("@ExileServer", array[1].Name);

            Assert.AreEqual("modNames:3-3", array[2].Key);
            Assert.AreEqual("Exile Mod", array[2].Name);

            Assert.AreEqual("sigNames:1-3", array[3].Key);
            Assert.AreEqual("ExtendedBase3.1", array[3].Name);

            Assert.AreEqual("sigNames:2-3", array[4].Key);
            Assert.AreEqual("a3", array[4].Name);

            Assert.AreEqual("sigNames:3-3", array[5].Key);
            Assert.AreEqual("exile", array[5].Name);

            Assert.AreEqual(6, array.Length);

        }

        [TestMethod, TestCategory("olnylocal")]
        public void ReadRules_Temp_Folder_NoExceptions()
        {
            string localTestDataFolder = @"E:\Temp\Data\";

            if (!Directory.Exists(localTestDataFolder))
                return;
            
            BlockingCollection<string> failedFiles = new BlockingCollection<string>(100);

            string[] ruleFiles = Directory.GetFiles(localTestDataFolder);
            Parallel.ForEach(ruleFiles, ruleFile =>
            {
                if (!ruleFile.EndsWith(".rdefrag"))
                    return;
                try
                {
                    using (FileStream unframedFile = File.OpenRead(ruleFile))
                    using (SteamDecodedBytes data = (new SteamDefragmentedBytes(ToArray(unframedFile))).DecodeSteamRuleFile_1_56())
                    {
                        // ReSharper disable once UnusedVariable
                        SteamServerRule[] steamServerRules = ServerQueries.ReadRuleFile(data).ToArray();
                    }
                    //Trace.WriteLine($"OK!   File '{ruleFile}' ");
                }
                catch (Exception exception)
                {
                    failedFiles.Add(ruleFile);
                    Trace.WriteLine($"FAIL! File '{ruleFile}' {exception.GetType().Name} {exception.Message}");
                }
            });

            foreach (var failedFile in failedFiles)
            {
                TestContext.WriteLine($"FAIL! File '{failedFile}' ");
            }
            Assert.AreEqual(0, failedFiles.Count);
        }

        private static byte[] ToArray(FileStream stream)
        {
            long oldPos = stream.Position;
            stream.Position = 0;
            byte[] result = new byte[stream.Length];
            stream.Read(result, 0, result.Length);
            stream.Seek(oldPos, SeekOrigin.Begin);
            return result;
        }

        // ReSharper disable once UnusedMember.Local
        private static void PrintAsserts(SteamServerRule[] array)
        {
            var i = 0;
            foreach (var steamServerRule in array)
            {
                Console.WriteLine($@"Assert.AreEqual(""{steamServerRule.Key}"", array[{i}].Key);");
                Console.WriteLine($@"Assert.AreEqual(""{steamServerRule.Name}"", array[{i}].Name);");
                Console.WriteLine();
                i++;
            }
        }
    }
}