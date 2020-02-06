using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace FactoryOrchestratorClientSample
{
    /// <summary>
    /// 
    /// </summary>
    class FactoryOrchestratorNETCoreClientSample
    {
        static async Task Main(string[] args)
        {
            await RunAsync(args);
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
        private static async Task RunAsync(string[] args)
        {
            TimeStarted = DateTime.Now;

            try
            {
                ValidateArgs(args);
                await ConnectToFactoryOrchestrator(Ip);
                // Set system time to accurate value
                await Client.RunExecutable("cmd.exe", $"/C \"time {DateTime.Now.ToLongTimeString()}\"");
                var FOXMLs = await CopyFilesToDUT(TestDir, DestDir);
                // Install UWP apps found in DestDir\apps folder
                await InstallAppsOnDUT(Path.Combine(DestDir, "apps"));
                var taskListSummaries = await LoadFactoryOrchestratorXMLs(DestDir, FOXMLs);
                await ExecuteTaskLists(taskListSummaries);
                await PrintFinalResult();
                await CopyLogsFromDUT(LogFolder);
                var TimeFinished = DateTime.Now;

                Console.WriteLine($"Copied test files, ran tests, and gathered logs in {TimeFinished - TimeStarted}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Fatal Exeption! {e.HResult} {e.Message}");
            }
        }

        /// <summary>
        /// Verifies arguments to program are valid.
        /// </summary>
        private static void ValidateArgs(string[] args)
        {
            try
            {
                if (args.Length != 4)
                {
                    throw new ArgumentException("Specify IP, test content, dest dir, and log output folder!");
                }

                if (!IPAddress.TryParse(args[0], out Ip))
                {
                    throw new ArgumentException($"{args[0]} is not a valid IP address!");
                }

                TestDir = args[1];
                if (!Directory.Exists(TestDir))
                {
                    throw new ArgumentException($"{TestDir} is not a valid directory path!");
                }

                DestDir = args[2];
                LogFolder = args[3];
            }
            catch (ArgumentException e)
            {
                PrintUsage(e);
            }
        }

        /// <summary>
        /// Prints usage if arguments were invalid.
        /// </summary>
        private static void PrintUsage(ArgumentException e)
        {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("dotnet ClientSample.dll <IP Address of DUT> <Folder on this PC with test content AND FactoryOrchestratorXML files> <Destination folder on DUT> <Destination folder on this PC to save logs>");
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
                Console.WriteLine($"Waiting for Factory Orchestrator Service on {ip.ToString()}...");
                await Task.Delay(2000);
            }

            Console.WriteLine($"Connected to {ip.ToString()}");
        }

        /// <summary>
        /// Copies all files in target folder to the DUT.
        /// </summary>
        /// <param name="testDir">Target folder</param>
        /// <param name="destDir">Destination folder</param>
        /// <returns>Filename of any XML files found.</returns>
        private static async Task<List<string>> CopyFilesToDUT(string testDir, string destDir)
        {
            Console.WriteLine($"Copying latest binaries and TaskLists from {testDir} to {destDir} on device...");

            var bytes = await Client.SendDirectoryToDevice(testDir, destDir);

            Console.WriteLine($"Copied {bytes} bytes to the device...");

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
            Console.WriteLine($"Looking for apps in subfolders of {destDir}...");

            var dirs = await Client.EnumerateDirectories(destDir, false);
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

                var apps = files.Where(x => x.ToLowerInvariant().EndsWith(".appx") || x.ToLowerInvariant().EndsWith(".appxbundle") ||
                                            x.ToLowerInvariant().EndsWith(".msixbundle") || x.ToLowerInvariant().EndsWith(".msix"));

                if (apps.Count() > 1)
                {
                    throw new InvalidDataException($"More than one app found in {dir}!");
                }

                if (apps.Count() == 0)
                {
                    continue;
                }

                // One & only one app was found, add to list
                mainApps.Add(apps.First());
                
                // Check for a certificate
                var certs = files.Where(x => x.ToLowerInvariant().EndsWith(".cer"));

                if (certs.Count() > 1)
                {
                    throw new InvalidDataException($"More than one certificate found in {dir}!");
                }

                // Add cert or null if no cert
                certificates.Add(certs.DefaultIfEmpty(null).FirstOrDefault());

                var interiorDirs = await Client.EnumerateDirectories(dir, false);
                if (interiorDirs.Count > 0 && interiorDirs.Any(x => x.ToLowerInvariant().EndsWith(@"\dependencies")))
                {
                    // App directory has 'dependencies' folder, see if it contains apps
                    var depsForApp = await Client.EnumerateFiles(Path.Combine(dir, "dependencies"), true);
                    if (depsForApp.Count > 0)
                    {
                        depsForApp = depsForApp.Where(x => x.ToLowerInvariant().EndsWith(".appx") || x.ToLowerInvariant().EndsWith(".appxbundle") ||
                                                        x.ToLowerInvariant().EndsWith(".msixbundle") || x.ToLowerInvariant().EndsWith(".msix")).ToList();
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

                Console.WriteLine($"Installing {mainApp}... This may take a few minutes...");
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
            Console.WriteLine("Loading TaskList(s) from FactoryOrchestratorXML file(s)...");

            foreach (var xmlFilename in FOXMLs)
            {
                await Client.LoadTaskListsFromXmlFile(Path.Combine(destDir, Path.GetFileName(xmlFilename)));
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
                Console.WriteLine($"Executing TaskList {summary.Name}...");
                await Client.RunTaskList(summary.Guid);

                var taskList = await Client.QueryTaskList(summary.Guid);

                while (taskList.IsRunningOrPending)
                {
                    System.Threading.Thread.Sleep(5000);
                    taskList = await Client.QueryTaskList(summary.Guid);

                    if (taskList.IsRunningOrPending)
                    {
                        var runningTasks = taskList.Tasks.Where(x => x.IsRunningOrPending);

                        Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}---- TaskList {summary.Name} Status: {taskList.TaskListStatus} ----");
                        Console.WriteLine($"---- Running Tasks: ----");
                        foreach (var task in runningTasks)
                        {
                            Console.Write($"{task.Name}");
                            if (task.LatestTaskRunRunTime != null)
                            {
                                Console.Write($": Running for { (task.LatestTaskRunRunTime).GetValueOrDefault().TotalSeconds} seconds");
                            }
                            Console.WriteLine();
                        }
                    }
                }

                Console.WriteLine($"TaskList {taskList.Name} is finished!");
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Prints out the final result of all the executed Tasks and TaskLists.
        /// </summary>
        private static async Task PrintFinalResult()
        {
            var tasklistSummaries = (await Client.GetTaskListSummaries()).OrderBy(x => x.Name);
            var allPassed = tasklistSummaries.All(x => x.Status == TaskStatus.Passed);
            Console.WriteLine("---- TaskList Execution Summary ----");
            Console.Write($"Overall result: ");
            if (allPassed)
            {
                Console.WriteLine("Passed!");
            }
            else
            {
                Console.WriteLine("Failed!");
            }
            Console.WriteLine($"Total time to load and execute all TaskLists: {DateTime.Now - TimeStarted}");

            foreach (var summary in tasklistSummaries)
            {
                var taskList = await Client.QueryTaskList(summary.Guid);
                Console.WriteLine($"---- Results for {taskList.Name} ----");
                Console.WriteLine($"Overall status: {taskList.TaskListStatus}");
                foreach (var task in taskList.Tasks)
                {
                    Console.WriteLine($"{task.Name}: {task.LatestTaskRunStatus} with exit code {task.LatestTaskRunExitCode.GetValueOrDefault()}. Task took {task.LatestTaskRunRunTime.GetValueOrDefault().TotalSeconds} seconds.");
                }
            }
        }

        /// <summary>
        /// Copies all Factory Orchestrator logs to the local PC.
        /// </summary>
        /// <param name="destinationPath">Target folder on local PC.</param>
        private static async Task CopyLogsFromDUT(string destinationPath)
        {
            var logDir = await Client.GetLogFolder();
            var bytes = await Client.GetDirectoryFromDevice(logDir, destinationPath);

            Console.WriteLine($"Logs copied to {destinationPath}. Total size: {bytes} bytes.");
        }

        public static FactoryOrchestratorClient Client;
        public static DateTime TimeStarted;
        public static IPAddress Ip;
        public static string TestDir;
        public static string DestDir;
        public static string LogFolder;
    }
}
