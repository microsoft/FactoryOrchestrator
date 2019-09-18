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
            TaskListSummary list = value as TaskListSummary;

            if (isList)
            {
                statusEnum = list.Status;
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
                    break;
                case TaskStatus.Failed:
                    status += "❌ Failed";
                    if ((!isList) && (task.TimesRetried > 0))
                    {
                        status += $" (All {task.MaxNumberOfRetries} retries)";
                    }
                    break;
                case TaskStatus.Running:
                    status += "▶ Running";
                    if ((!isList) && (task.TimesRetried > 0))
                    {
                        if (task.MaxNumberOfRetries == 0)
                        {
                            status += $" (Retry {task.TimesRetried})";
                        }
                        else
                        {
                            status += $" (Retry {task.TimesRetried} of {task.MaxNumberOfRetries})";
                        }
                    }
                    break;
                case TaskStatus.NotRun:
                    status += "❔ Not Run";
                    break;
                case TaskStatus.Aborted:
                    status += "⛔ Aborted";
                    break;
                case TaskStatus.Timeout:
                    status += "⏱ Timed-out";
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
