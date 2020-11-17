using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Xml.Schema;
using System.IO;
using System.Xml;
using Microsoft.FactoryOrchestrator.Client;
using System.Net;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Collections.Generic;

namespace Microsoft.FactoryOrchestrator.Test
{
    [TestClass]
    public class PowerShellUnitTests
    {
        /// <summary>
        /// Verifies FactoryOrchestratorClientSync & FactoryOrchestratorClient are equivalent.
        /// Also indirectly verifies every IFactoryOrchestratorService API is accessible as FactoryOrchestratorClientSync inherits from IFactoryOrchestratorService.
        /// </summary>
        [TestMethod]
        public void VerifyEqualMethods()
        {
            List<string> allowedPwshOnly = new List<string>() { "get_AsyncClient" };
            List<string> allowedClientOnly = new List<string>() {};
            var client = typeof(FactoryOrchestratorClient).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => !allowedClientOnly.Contains(x.Name)).Select(x => x.Name).Distinct();
            var pwsh = typeof(FactoryOrchestratorClientSync).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => !allowedPwshOnly.Contains(x.Name)).Select(x => x.Name).Distinct();

            var onlyClient = client.Except(pwsh);
            foreach (var api in onlyClient)
            {
                Logger.LogMessage($"{api} is only in client");
            }

            var onlyPwsh = pwsh.Except(client);
            foreach (var api in onlyPwsh)
            {
                Logger.LogMessage($"{api} is only in PowerShell");
            }

            Assert.AreEqual(onlyClient.Count(), 0);
            Assert.AreEqual(onlyPwsh.Count(), 0);
        }
    }
}
