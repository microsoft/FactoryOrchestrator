using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Xml.Schema;
using Microsoft.FactoryOrchestrator.Server;
using System.IO;
using System.Xml;

namespace Microsoft.FactoryOrchestrator.Test
{
    [TestClass]

    public class ServerLibraryUnitTests
    {
        static TaskManager t;
        static string DeploymentDir;
        static bool validXmlLoaded = false;

        [ClassInitialize]
        [DeploymentItem("Valid.xml")]
        public static void Init(TestContext context)
        {
#pragma warning disable CA1062
            DeploymentDir = context.DeploymentDirectory;
#pragma warning restore CA1062
            t = new TaskManager(DeploymentDir, "TempStateFile.xml");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            t.Dispose();
        }

        [TestMethod]
        public void TestValidXmlLoad()
        {
            if (validXmlLoaded)
            {
                return;
            }

            t.LoadTaskListsFromXmlFile(Path.Combine(DeploymentDir, "Valid.xml"));
            validXmlLoaded = true;
        }

        [TestMethod]
        [DeploymentItem("DupGuid1.xml")]
        public void TestDuplicateGuidInXmlAndKnownTaskList()
        {
            if (!validXmlLoaded)
            {
                TestValidXmlLoad();
            }

            string[] expectedGuids = { "8886302c-070c-4e19-b9e3-1b76f8c03f4e", "9fe8d491-546b-4622-b5a2-035e68f69f36" };
            try
            {
                t.LoadTaskListsFromXmlFile(Path.Combine(DeploymentDir, "DupGuid1.xml"));
                Assert.Fail("XML should not have loaded successfully!");
            }
            catch (FactoryOrchestratorXmlException e)
            {
                foreach (var guid in expectedGuids)
                {
                    Assert.IsTrue(e.Message.Contains(guid, StringComparison.OrdinalIgnoreCase), $"guid {guid} should be in error string!");
                }
            }
        }

        [TestMethod]
        [DeploymentItem("DupGuid2.xml")]
        public void TestDuplicateGuidInXmlAndKnownTaskList2()
        {
            if (!validXmlLoaded)
            {
                TestValidXmlLoad();
            }

            string[] expectedGuids = { "8886302c-070c-4e19-b9e3-1b76f8c03f4e" };
            try
            {
                t.LoadTaskListsFromXmlFile(Path.Combine(DeploymentDir, "DupGuid2.xml"));
                Assert.Fail("XML should not have loaded successfully!");
            }
            catch (FactoryOrchestratorXmlException e)
            {
                foreach (var guid in expectedGuids)
                {
                    Assert.IsTrue(e.Message.Contains(guid, StringComparison.OrdinalIgnoreCase), $"guid {guid} should be in error string!");
                }
            }
        }
    }
}
