// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.ClientSample
{
    /// <summary>
    /// 
    /// </summary>
    public static class FactoryOrchestratorNETCoreClientSample
    {
        /// <summary>
        /// Gets the failed TaskRun guids. This is used for Factory Orchestrator testing only, not by this sample code.
        /// See src\Tests\ClientSampleIntegrationTest\ClientSampleIntegrationTests.cs for details.
        /// </summary>
        /// <value>
        /// The failed run guids.
        /// </value>
        public static List<Guid> FailedRunGuids { get; private set; }

        public static async Task<int> Main(string[] args)
        {
            FailedRunGuids = new List<Guid>();
            return await RunAsync(args);
        }

        /// <summary>
        /// Main method for Client Sample. Factory Orchestrator service methods are all async.
        /// 1) Connect to DUT
        /// 2) Copy files and FactoryOrchestratorXML to DUT
        /// 3) Install UWP apps that were copied to DUT under \apps folder
        /// 4) Load TaskLists from FactoryOrchestratorXML
        /// 5) Execute loaded TaskLists
        /// 6) Print results
        /// 7) Copy logs to host PC
        /// </summary>
        private static async Task<int> RunAsync(string[] args)
        {
            TimeStarted = DateTime.Now;
            bool passed = false;
            try
            {
                if (ValidateArgs(args))
                {
                    await ConnectToFactoryOrchestrator(Ip);
                    // Set system time to accurate value
                    await Client.RunExecutable("cmd.exe", $"/C \"time {DateTime.Now.ToLongTimeString()}\"");
                    var FOXMLs = await CopyFilesToDUT(TestDir, DestDir);
                    // Install UWP apps found in DestDir\apps folder
                    await InstallAppsOnDUT(Path.Combine(DestDir, "apps"));
                    var taskListSummaries = await LoadFactoryOrchestratorXMLs(DestDir, FOXMLs);
                    await ExecuteTaskLists(taskListSummaries);
                    passed = await PrintFinalResult();
                    await CopyLogsFromDUT(LogFolder);
                    var TimeFinished = DateTime.Now;

                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.Completed, TimeFinished - TimeStarted));
                    return passed ? 0 : 1;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{Resources.Exception} {e.HResult} {e.Message}");
                return e?.HResult ?? -2147467259; // -2147467259 == E_FAIL
            }
        }

        /// <summary>
        /// Verifies arguments to program are valid.
        /// </summary>
        /// <returns><c>true</c> if NOT a Discover query.</returns>
        private static bool ValidateArgs(string[] args)
        {
            try
            {
                if (args.Length == 2)
                {
                    if ("--Discover".Equals(args[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        uint seconds;
                        if (!UInt32.TryParse(args[1], out seconds))
                        {
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidSeconds, args[1]));
                        }

                        DiscoverServices((int)seconds);
                        return false;
                    }
                    else
                    {
                        throw new ArgumentException(Resources.NotEnoughArgs);
                    }
                }

                if (args.Length != 4)
                {
                    throw new ArgumentException(Resources.NotEnoughArgs);
                }

                IPAddress ip;
                if (!IPAddress.TryParse(args[0], out ip))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidIp, args[0]));
                }
                Ip = ip;

                TestDir = args[1];
                if (!Directory.Exists(TestDir))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidDir, TestDir));
                }

                DestDir = args[2];
                LogFolder = args[3];
            }
            catch (ArgumentException e)
            {
                PrintUsage(e);
            }

            return true;
        }

        private static void DiscoverServices(int seconds)
        {
            Console.WriteLine(Resources.LookingForServices);
            var discovered = FactoryOrchestratorClient.DiscoverFactoryOrchestratorDevices(seconds);
            if (discovered.Any())
            {
                Console.WriteLine(Resources.FoundServices);
                foreach (var client in discovered)
                {
                    Console.WriteLine($"{client.HostName} - {client.OSVersion} - {client.IpAddress}:{client.Port}");
                }
            }
            else
            {
                Console.WriteLine(Resources.NoDevicesFound);
            }
        }

        /// <summary>
        /// Prints usage if arguments were invalid.
        /// </summary>
        private static void PrintUsage(ArgumentException e)
        {
            Console.WriteLine();
            Console.WriteLine(Resources.Usage);
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.ClientSampleUsage, Environment.GetCommandLineArgs()[0]));
            Console.WriteLine();
            throw e;
        }

        /// <summary>
        /// Instantiates a FactoryOrchestratorClient object to communicate with the Service.
        /// </summary>
        /// <param name="ip">Ip to connect to</param>
        private static async Task ConnectToFactoryOrchestrator(IPAddress ip)
        {
            Client = new FactoryOrchestratorClient(ip);
            while (!await Client.TryConnect())
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.WaitingForService, ip.ToString()));
                await Task.Delay(2000);
            }

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.ConnectedToIp, ip.ToString()));
        }

        /// <summary>
        /// Copies all files in target folder to the DUT.
        /// </summary>
        /// <param name="testDir">Target folder</param>
        /// <param name="destDir">Destination folder</param>
        /// <returns>Filename of any XML files found.</returns>
        private static async Task<List<string>> CopyFilesToDUT(string testDir, string destDir)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.CopyingFiles, testDir, destDir));

            var bytes = await Client.SendDirectoryToDevice(testDir, destDir);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.CopyComplete, bytes));

            return Directory.EnumerateFiles(testDir, "*.xml").ToList();
        }

        /// <summary>
        /// Install any UWP apps that were copied to the DUT. Each app must be in its own folder that is a DIRECT subdirectory of destDir. with any dependencies under a subdirectory called dependencies.
        /// The app folder can contain one .cer file for the app, if needed. Apps and dependencies must be the correct architecture for the DUT.
        ///
        /// For example:
        /// destDir \
        ///             App1 \
        ///                   App1.appxbundle
        ///                   Dependencies \
        ///                                 x64 \
        ///                                      App1Dependency.appx
        ///             App2 \
        ///                   App2.msixbundle
        ///                   App2.cer
        ///                   Dependencies \
        ///                                 App2Dependency.msix
        /// </summary>
        /// <param name="destDir">Root folder on DUT where apps were copied to</param>
        private static async Task InstallAppsOnDUT(string destDir)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.LookingForApps, destDir));

            List<string> dirs;
            try
            {
                dirs = await Client.EnumerateDirectories(destDir, false);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            var mainApps = new List<string>();
            var certificates = new List<string>();
            var dependencyApps = new List<List<string>>();

            foreach (var dir in dirs)
            {
                // Look for an app in each sub-directory of destDir, non-recursive
                var files = await Client.EnumerateFiles(dir, false);
                if (files.Count == 0)
                {
                    continue;
                }

                var apps = files.Where(x => x.EndsWith(".appx", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".appxbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msixbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msix", StringComparison.InvariantCultureIgnoreCase));

                if (!apps.Any())
                {
                    continue;
                }

                if (apps.Count() > 1)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Resources.TooManyApps, dir));
                }

                // One & only one app was found, add to list
                mainApps.Add(apps.First());
                
                // Check for a certificate
                var certs = files.Where(x => x.EndsWith(".cer", StringComparison.InvariantCultureIgnoreCase));

                if (certs.Count() > 1)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Resources.TooManyCerts, dir));
                }

                // Add cert or null if no cert
                certificates.Add(certs.DefaultIfEmpty(null).FirstOrDefault());

                var interiorDirs = await Client.EnumerateDirectories(dir, false);
                if (interiorDirs.Count > 0 && interiorDirs.Any(x => x.EndsWith(@"\dependencies", StringComparison.InvariantCultureIgnoreCase)))
                {
                    // App directory has 'dependencies' folder, see if it contains apps
                    var depsForApp = await Client.EnumerateFiles(Path.Combine(dir, "dependencies"), true);
                    if (depsForApp.Count > 0)
                    {
                        depsForApp = depsForApp.Where(x => x.EndsWith(".appx", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".appxbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msixbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msix", StringComparison.InvariantCultureIgnoreCase)).ToList();

                        if (depsForApp.Count == 0)
                        {
                            // no dependent apps
                            dependencyApps.Add(null);
                        }
                        else
                        {
                            // has dependent apps
                            dependencyApps.Add(depsForApp);
                        }
                    }
                }
                else
                {
                    // no dependent apps
                    dependencyApps.Add(null);
                }
            }

            // We have found all the apps, dependencies, and certs, start installing
            for (int i = 0; i < mainApps.Count; i++)
            {
                var mainApp = mainApps[i];
                var depAppsForCurrentApp = dependencyApps[i];
                var certForCurrentApp = certificates[i];

                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.InstallingApp, mainApp));
                await Client.InstallApp(mainApp, depAppsForCurrentApp, certForCurrentApp);
            }
        }

        /// <summary>
        /// Imports any FactoryOrchestratorXML files found on a target folder on the DUT.
        /// </summary>
        /// <param name="destDir">Target folder</param>
        /// <param name="FOXMLs">Filenames of XML files to import</param>
        private static async Task<List<TaskListSummary>> LoadFactoryOrchestratorXMLs(string destDir, List<string> FOXMLs)
        {
            if (FOXMLs?.Count > 0)
            {
                Console.WriteLine(Resources.LoadingFOXML);

                foreach (var xmlFilename in FOXMLs)
                {
                    await Client.LoadTaskListsFromXmlFile(Path.Combine(destDir, Path.GetFileName(xmlFilename)));
                }
            }
            else
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.CreatingListFromDirectory, destDir));
                await Client.CreateTaskListFromDirectory(destDir);
            }

            Console.WriteLine($"{Environment.NewLine}TaskLists:");

            var tasklistSummaries = await Client.GetTaskListSummaries();
            foreach (var summary in tasklistSummaries)
            {
                Console.WriteLine(summary.ToString());
            }

            return tasklistSummaries.ToList();
        }

        /// <summary>
        /// Runs TaskLists, printing out results.
        /// </summary>
        /// <param name="tasklistSummaries">List of TaskListSummary objects returned by GetTaskListSummaries Method</param>
        private static async Task ExecuteTaskLists(List<TaskListSummary> tasklistSummaries)
        {
            foreach (var summary in tasklistSummaries)
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.ExecutingTaskList, summary.Name));
                await Client.RunTaskList(summary.Guid);

                var taskList = await Client.QueryTaskList(summary.Guid);

                while (taskList.IsRunningOrPending)
                {
                    System.Threading.Thread.Sleep(5000);
                    taskList = await Client.QueryTaskList(summary.Guid);

                    if (taskList.IsRunningOrPending)
                    {
                        var runningTasks = taskList.Tasks.Where(x => x.IsRunningOrPending);

                        Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}---- {taskList.ToString()} ----");
                        Console.WriteLine($"---- {Resources.RunningTasks} ----");
                        foreach (var task in runningTasks)
                        {
                            Console.Write($"{task.Name}");
                            if (task.LatestTaskRunRunTime != null)
                            {
                                Console.Write($": {string.Format(CultureInfo.CurrentCulture, Resources.RunningFor, (task.LatestTaskRunRunTime).GetValueOrDefault().TotalSeconds)}");
                            }
                            Console.WriteLine();
                        }
                    }
                }

                Console.WriteLine(Resources.TaskListComplete);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Prints out the final result of all the executed Tasks and TaskLists.
        /// </summary>
        private static async Task<bool> PrintFinalResult()
        {
            var tasklistSummaries = (await Client.GetTaskListSummaries()).OrderBy(x => x.Name);
            var allPassed = tasklistSummaries.All(x => x.Status == TaskStatus.Passed);
            Console.WriteLine($"---- {Resources.SummaryHeader} ----");
            Console.Write($"{Resources.OverallResult}: ");
            if (allPassed)
            {
                Console.WriteLine(Resources.Passed);
            }
            else
            {
                Console.WriteLine(Resources.Failed);
            }
            Console.WriteLine($"{Resources.TotalTime} {DateTime.Now - TimeStarted}");

            foreach (var summary in tasklistSummaries)
            {
                var taskList = await Client.QueryTaskList(summary.Guid);
                Console.WriteLine($"---- {Resources.ResultsFor} {taskList.Name} ----");
                Console.WriteLine($"{Resources.OverallResult}: {taskList.TaskListStatus}");
                foreach (var task in taskList.Tasks)
                {

                    Console.WriteLine($"{task.Name}: {task.LatestTaskRunStatus} {Resources.WithExitCode} {task.LatestTaskRunExitCode.GetValueOrDefault()}. {string.Format(CultureInfo.CurrentCulture, Resources.TaskTime, task.LatestTaskRunRunTime.GetValueOrDefault().TotalSeconds)}");

                    if (task.LatestTaskRunStatus != TaskStatus.Passed)
                    {
                        // Add to FailedRunGuids. This is used for Factory Orchestrator testing only, not by this sample code.
                        // See src\Tests\ClientSampleIntegrationTest\ClientSampleIntegrationTests.cs for details.
                        FailedRunGuids.Add((Guid)task.LatestTaskRunGuid);
                    }
                }
            }

            return allPassed;
        }

        /// <summary>
        /// Copies all Factory Orchestrator logs to the local PC.
        /// </summary>
        /// <param name="destinationPath">Target folder on local PC.</param>
        private static async Task CopyLogsFromDUT(string destinationPath)
        {
            var logDir = await Client.GetLogFolder();
            var bytes = await Client.GetDirectoryFromDevice(logDir, destinationPath);

            Console.WriteLine($"{Resources.LogsCopiedTo} {destinationPath}. {string.Format(CultureInfo.CurrentCulture, Resources.TotalSize, bytes)}");
        }

        private static FactoryOrchestratorClient Client;
        public static DateTime TimeStarted { get; private set; }
        public static IPAddress Ip { get; private set; }
        public static string TestDir { get; private set; }
        public static string DestDir { get; private set; }
        public static string LogFolder { get; private set; }
    }
}
