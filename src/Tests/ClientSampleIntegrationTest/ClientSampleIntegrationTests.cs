// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FactoryOrchestrator;
using System;
using System.Net;
using Microsoft.FactoryOrchestrator.Client;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace Microsoft.FactoryOrchestrator.Test
{
    /// <summary>
    /// This is a manual test which uses the Microsoft.FactoryOrchestrator.ClientSample to verify the FactoryOrchestratorService is functioning properly.
    /// 
    /// It validates a variety of service functions, including task execution using the given test content.
    /// The amount of validation done is dependent on the given test content, but it can validate:
    /// - Service connection
    /// - File/Folder copy to and from service
    /// - App install, if apps are in the test content.
    /// - Loading TaskList(s) from file(s)
    /// - TaskList execution and status queries
    /// - Result logging to files
    /// 
    /// Test properties, all are optional:
    /// serviceIp - IP address of service to connect to. Default: localhost (127.0.0.1)
    /// testContentSrc - Folder with test content and FactoryOrchestratorXML files. Default: C:\TestContentSrc
    /// testContentDest - Folder on service to copy testContentSrc to. Tasks in FactoryOrchestratorXML should use this folder. Default: C:\TestContent
    /// logDest - Folder on client (test executee) to copy logs to. Default: %TEMP%\clientsampleintegrationtests
    /// verifyPassed - Boolean. If "true", ClientSampleIntegrationTests only passes if all Tasks run by the service passed. Default: true.
    /// 
    /// WARNING: This test resets the FactoryOrchestratorService it connects to! 
    /// 
    /// </summary>
    [TestClass]
    public class ClientSampleIntegrationTests
    {
        private static TestContext _classTestContext;
        private static string _serviceIp;
        private static string _testContentSrc;
        private static string _testContentDest;
        private static string _logDest;
        private static bool _verifyPassed;
        private static bool _cleanLogFolder;
        private static bool _testExecuted = false;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
#pragma warning disable CA1062
            _classTestContext = context;
            _verifyPassed = (_classTestContext.Properties["verifyPassed"] as bool?) ?? true;
            _cleanLogFolder = (_classTestContext.Properties["cleanLogFolder"] as bool?) ?? false;
            FactoryOrchestratorClient testClientConnection;
#pragma warning restore CA1062

            try
            {
                _serviceIp = _classTestContext.Properties["serviceIp"]?.ToString() ?? "127.0.0.1";
                Logger.LogMessage($"Attempting to connect to {_serviceIp}");
                testClientConnection = new FactoryOrchestratorClient(IPAddress.Parse(_serviceIp));
                // Try to connect to given IP, will throw exception if it fails
                testClientConnection.Connect().Wait();
            }
            catch (Exception e)
            {
                // TODO: Create FOServiceExe instance if one isn't detected and localhost (127.0.0.1) was used.
                Logger.LogMessage(e.ToString());
                Assert.Inconclusive("This test requires a running FactoryOrchestratorService!");
                return;
            }

            testClientConnection.ResetService(false, false).Wait();
            _testContentSrc = _classTestContext.Properties["testContentSrc"]?.ToString() ?? @"C:\TestContentSrc";
            _testContentDest = _classTestContext.Properties["testContentDest"]?.ToString() ?? @"C:\TestContent";

            _logDest  = _classTestContext.Properties["logDest"]?.ToString() ?? Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "clientsampleintegrationtests");
        }

        [TestMethod]
        public void RunClientSample()
        {
            _testExecuted = true;
            Logger.LogMessage($"Microsoft.FactoryOrchestrator.ClientSample {_serviceIp} {_testContentSrc} {_testContentDest} {_logDest}");
            Logger.LogMessage($"verifyPassed {_verifyPassed}");

            if (_cleanLogFolder && Directory.Exists(_logDest))
            {
                Directory.Delete(_logDest, true);
            }
            // Run client sample, will create a testlist based on files in _testContentSrc if one isn't found in _testContentSrc
            var result = Microsoft.FactoryOrchestrator.ClientSample.FactoryOrchestratorNETCoreClientSample.Main(new string[] { _serviceIp, _testContentSrc, _testContentDest, _logDest }).Result;

            if (_verifyPassed)
            {
                // Get details on all failed tasks, if any.
                // The GUIDs of failed TaskRuns from the ClientSample's execution are stored in the public "List<Guid> FailedRunGuids" parameter.
                var clientConnection = new FactoryOrchestratorClient(IPAddress.Parse(_serviceIp));
                clientConnection.Connect().Wait();

                string errorString = "";
                foreach(var guid in Microsoft.FactoryOrchestrator.ClientSample.FactoryOrchestratorNETCoreClientSample.FailedRunGuids)
                {
                    // Output the task path, the reason the task failed, and the entire task output to the error string.
                    // This helps ensure failures have a unique "fingerprint" depending on what task failed and how it failed.
                    var run = clientConnection.QueryTaskRun(guid).Result;
                    errorString = $"{run.TaskPath} failed with status {run.TaskStatus}.";
                    foreach(var line in run.TaskOutput)
                    {
                        errorString += $"\n{line}";
                    }
                }

                if (!string.IsNullOrWhiteSpace(errorString))
                {
                    Assert.Fail(errorString);
                }

                Assert.AreEqual(result, 0);
            }
            else
            {
                Assert.IsTrue(result == 0 || result == 1);
            }

            // Assume at least 1 log file was created
            var logFiles = Directory.EnumerateFiles(_logDest, "*.log", SearchOption.AllDirectories);
            Assert.AreNotEqual(logFiles.Count(), 0);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (_testExecuted)
            {
                // Reset service
                var clientConnection = new FactoryOrchestratorClient(IPAddress.Parse(_serviceIp));
                clientConnection.Connect().Wait();
                clientConnection.ResetService(false, false).Wait();
            }
        }
    }
}
