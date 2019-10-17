using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
{
    class TaskStatusDataBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var paramStr = parameter as String;
            
            if (paramStr.Equals("task", StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertStatus(value, false);
            }
            else if (paramStr.Equals("list", StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertStatus(value, true);
            }
            else
            {
                throw new ArgumentException("TaskBaseQuickStatusConverter parameter is unrecognized");
            }
        }

        private string ConvertStatus(object value, bool isList)
        {
            String status = "";
            TaskStatus statusEnum;
            // value is the data from the source object.
            TaskBase task = value as TaskBase;
            TaskListSummaryWithTemplate list = value as TaskListSummaryWithTemplate;

            if (isList)
            {
                statusEnum = list.Summary.Status;
            }
            else
            {
                statusEnum = task.LatestTaskRunStatus;
            }

            switch (statusEnum)
            {
                case TaskStatus.Passed:
                    status += "✔ Passed";
                    if ((!isList) && (task.TimesRetried > 0))
                    {
                        status += $" (On retry {task.TimesRetried})";
                    }
                    if ((!isList) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} total runs)";
                    }
                    break;
                case TaskStatus.Failed:
                    status += "❌ Failed";
                    if ((!isList) && (task.TimesRetried > 0))
                    {
                        status += $" (All {task.TimesRetried} retries)";
                    }
                    if ((!isList) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} total runs)";
                    }
                    break;
                case TaskStatus.Running:
                    status += "▶ Running";
                    if ((!isList) && (task.TimesRetried > 0))
                    {
                        status += $" (Retry {task.TimesRetried})";
                    }
                    break;
                case TaskStatus.NotRun:
                    status += "❔ Not Run";
                    break;
                case TaskStatus.Aborted:
                    status += "⛔ Aborted";
                    if ((!isList) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} total runs)";
                    }
                    break;
                case TaskStatus.Timeout:
                    status += "⏱ Timed-out";
                    if ((!isList) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} total runs)";
                    }
                    break;
                case TaskStatus.RunPending:
                    status += "❔ Run Pending";
                    break;
                default:
                    status += "❔ Unknown";
                    break;
            }

            return status;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
