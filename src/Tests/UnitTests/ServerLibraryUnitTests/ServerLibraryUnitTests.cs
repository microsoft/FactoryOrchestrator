// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Xml.Schema;
using Microsoft.FactoryOrchestrator.Server;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;

namespace Microsoft.FactoryOrchestrator.Test
{
    [TestClass]

    public class ServerLibraryUnitTests
    {
        static TaskManager t;
        static string DeploymentDir;
        static bool validXmlLoaded = false;

        [ClassInitialize]
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                t.LoadTaskListsFromXmlFile(Path.Combine(DeploymentDir, "Valid_Windows.xml"));
            }
            validXmlLoaded = true;
        }

        [TestMethod]
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
