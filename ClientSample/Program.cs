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

namespace NETCoreClientSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = Task.Run(() => RunAsync(args));
            t.Wait();
        }

        private static async Task RunAsync(string[] args)
        {
            TimeStarted = DateTime.Now;

            ValidateArgs(args);
            await ConnectToFactoryOrchestrator(Ip, 45684);
            var FOXMLs = await CopyFilesToDUT(TestDir, DestDir);
            var taskListSummaries = await LoadFactoryOrchestratorXMLs(DestDir, FOXMLs);
            await ExecuteTaskLists(taskListSummaries);
            await PrintFinalResult();
            await CopyLogsFromDUT(LogFolder);
        }

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

        private static void PrintUsage(ArgumentException e)
        {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("dotnet ClientSample.dll <IP Address of DUT> <Folder on this PC with test content AND FactoryOrchestratorXML files> <Destination folder on DUT> <Destination folder on this PC to save logs>");
            Console.WriteLine();
            throw e;
        }

        private static async Task ConnectToFactoryOrchestrator(IPAddress ip, int port)
        {
            Client = new FactoryOrchestratorClient(ip, port);
            await Client.Connect();

            Console.WriteLine($"Connected to {ip.ToString()}");
        }

        private static async Task<List<string>> CopyFilesToDUT(string testDir, string destDir)
        {
            Console.WriteLine($"Copying latest binaries and TaskLists from {testDir} to {destDir} on device...");

            await Client.SendDirectoryToDevice(testDir, destDir);
            return Directory.EnumerateFiles(testDir, "*.xml").ToList();
        }

        private static async Task<List<TaskListSummary>> LoadFactoryOrchestratorXMLs(string destDir, List<string> FOXMLs)
        {
            Console.WriteLine("Loading TaskList(s) from FactoryOrchestratorXML file(s)...");

            foreach (var xmlFilename in FOXMLs)
            {
                await Client.LoadTaskListsFromXmlFile(Path.Combine(destDir, xmlFilename));
            }

            Console.WriteLine($"{Environment.NewLine}TaskLists:");

            var tasklistSummaries = (await Client.GetTaskListSummaries()).OrderBy(x => x.Name);
            foreach (var summary in tasklistSummaries)
            {
                Console.WriteLine(summary.ToString());
            }

            return tasklistSummaries.ToList();
        }

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
                    var runningTasks = taskList.Tasks.Values.Where(x => x.IsRunning);

                    if (taskList.IsRunning)
                    {
                        Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}---- TaskList {summary.Name} Status: {taskList.TaskListStatus} ----");
                        Console.WriteLine($"---- Running Tasks: ----");
                        foreach (var task in runningTasks)
                        {
                            Console.WriteLine($"{task.Name}: Running for {(DateTime.Now - task.LatestTaskRunTimeStarted).Value.TotalSeconds} seconds");
                        }
                    }
                }

                Console.WriteLine($"TaskList {taskList.Name} is finished!");
                Console.WriteLine();
                Console.WriteLine();
            }
        }

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
                foreach (var task in taskList.Tasks.Values)
                {
                    Console.WriteLine($"{task.Name}: {task.LatestTaskRunStatus} with exit code {task.LatestTaskRunExitCode}. Task took {task.LatestTaskRunRunTime.Value.TotalSeconds} seconds.");
                }
            }
        }

        private static async Task CopyLogsFromDUT(string destinationPath)
        {
            var logDir = await Client.GetLogFolder();
            await Client.GetDirectoryFromDevice(logDir, destinationPath);

            Console.WriteLine($"Logs copied to {destinationPath}");
        }

        public static FactoryOrchestratorClient Client;
        public static DateTime TimeStarted;
        public static IPAddress Ip;
        public static string TestDir;
        public static string DestDir;
        public static string LogFolder;
    }
}
