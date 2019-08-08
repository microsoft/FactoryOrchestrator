using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.FactoryOrchestrator.UWP
{
    public class TestViewModel
    {
        public TestData TestData { get; set; }
        public TestViewModel()
        {
            TestData = new TestData
            {
                TaskListMap = new Dictionary<Guid, TaskList>(),
                TestGuidsMap = new Dictionary<int, Guid>(),
                TestNames = new ObservableCollection<String>(),
                TestStatus = new ObservableCollection<String>()
            };
        }

        public void SetTestNames(Guid guid)
        {
            SetTestStatus(guid);
            TestData.TestNames = new ObservableCollection<String>(TestData.TaskListMap[guid].Tasks.Values.Select(x => x.TestName).ToList());
        }

        private void SetTestStatus(Guid guid)
        {
            List<String> testResults = new List<String>();
            foreach (var task in TestData.TaskListMap[guid].Tasks.Values)
            {
                String str;
                switch (task.LatestTaskRunStatus)
                {
                    case TaskStatus.Passed:
                        str = "✔ Passed";
                        if (task.TimesRetried > 0)
                        {
                            str += $" (On retry {task.TimesRetried})";
                        }
                        testResults.Add(str);
                        break;
                    case TaskStatus.Failed:
                        str = "❌ Failed";
                        if (task.TimesRetried > 0)
                        {
                            str += $" (All {task.MaxNumberOfRetries} retries)";
                        }
                        testResults.Add(str);
                        break;
                    case TaskStatus.Running:
                        str = "▶ Running";
                        if (task.TimesRetried > 0)
                        {
                            str += $" (Retry {task.TimesRetried} of {task.MaxNumberOfRetries})";
                        }
                        testResults.Add(str);
                        break;
                    case TaskStatus.NotRun:
                        testResults.Add("❔ Not Run");
                        break;
                    case TaskStatus.Aborted:
                        testResults.Add("⛔ Aborted");
                        break;
                    case TaskStatus.Timeout:
                        testResults.Add("⏱ Timed-out");
                        break;
                    default:
                        testResults.Add("❔ Unknown");
                        break;
                }
            }
            //return new ObservableCollection<String>(TestData.TaskListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
            TestData.TestStatus = new ObservableCollection<String>(testResults);
        }

        public void SetTestNames(ObservableCollection<String> testNames)
        {
            TestData.TestNames = testNames;
        }

        public void AddOrUpdateTaskList(TaskList taskList)
        {
            if (TestData.TaskListMap.ContainsKey(taskList.Guid))
            {
                TestData.TaskListMap[taskList.Guid] = taskList;

            }
            else
            {
                TestData.TaskListMap.Add(taskList.Guid, taskList);
                TestData.TaskListGuids.Add(taskList.Guid);
            }

            SetTestNames(taskList.Guid);
        }

        public void SetActiveTaskList(Guid taskListGuid)
        {
            TestData.SelectedTaskListGuid = taskListGuid;
            SetTests(taskListGuid);
        }

        public void ClearActiveTaskList()
        {
            TestData.SelectedTaskListGuid = null;
            TestData.TestNames = new ObservableCollection<string>();
            TestData.TestStatus = new ObservableCollection<string>();
            TestData.TestGuidsMap = new Dictionary<int, Guid>();
        }

        public void SetTests(Guid taskListGuid)
        {
            SetTestNames(taskListGuid);
            SetTestGuidsMap(taskListGuid);
        }

        public bool PruneKnownTaskLists(List<Guid> taskListGuids)
        {
            bool guidRemoved = false;

            foreach (var guid in TestData.TaskListGuids)
            {
                if (!taskListGuids.Contains(guid))
                {
                    TestData.TaskListMap.Remove(guid);
                    guidRemoved = true;
                    if (TestData.SelectedTaskListGuid == guid)
                    {
                        ClearActiveTaskList();
                    }
                }
            }

            return guidRemoved;
        }

        private void SetTestGuidsMap(Guid taskListGuid)
        {
            Dictionary<int, Guid> testGuidsMap = new Dictionary<int, Guid>();
            int index = 0;
            foreach (var task in TestData.TaskListMap[taskListGuid].Tasks.Values)
            {
                testGuidsMap.Add(index, task.Guid);
                index++;
            }
            TestData.TestGuidsMap = testGuidsMap;
        }
    }
}
