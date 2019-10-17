using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace FactoryOrchestratorClientSample
{
    /// <summary>
    /// 
    /// </summary>
    class FactoryOrchestratorNETCoreClientSample
    {
        static void Main(string[] args)
        {
            var t = Task.Run(() => RunAsync(args));
            t.Wait();
        }

        /// <summary>
        /// Main method for Client Sample. Factory Orchestrator service methods are all async.
        /// 1) Connect to DUT
        /// 2) Copy files and FactoryOrchestratorXML to DUT
        /// 3) Load TaskLists from FactoryOrchestratorXML
        /// 4) Execute loaded TaskLists
        /// 5) Print results
        /// 6) Copy logs to host PC
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

                while (taskList.IsRunning)
                {
                    System.Threading.Thread.Sleep(5000);
                    taskList = await Client.QueryTaskList(summary.Guid);

                    if (taskList.IsRunning)
                    {
                        var runningTasks = taskList.Tasks.Where(x => x.IsRunning);

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
