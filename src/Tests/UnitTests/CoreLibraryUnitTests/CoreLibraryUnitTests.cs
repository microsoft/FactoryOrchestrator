using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Xml.Schema;
using System.IO;
using System.Xml;

namespace Microsoft.FactoryOrchestrator.Test
{
    [TestClass]
    public class CoreLibraryUnitTests
    {
        [TestMethod]
        [DeploymentItem("DuplicateGuids.xml")]
        public void TestDuplicateGuidInXml()
        {
            string[] expectedGuids = { "ba3fa48b-b555-490d-9082-3d93dca22e70", "7bac1a15-0e62-4d49-8a54-27077409d229", "ba3fa48b-b555-490d-9082-3d93dca22e72" };
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DuplicateGuids.xml");
                FactoryOrchestratorXML.Load(path);
                Assert.Fail("XML should not have loaded successfully!");
            }
            catch (XmlException e)
            {
                Assert.AreEqual(e.InnerException.GetType(), typeof(XmlSchemaValidationException));
                foreach (var guid in expectedGuids)
                {
                    Assert.IsTrue(e.InnerException.Message.Contains(guid, StringComparison.OrdinalIgnoreCase), $"guid {guid} should be in error string!");
                }
            }
        }
    }
}
