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
    class TaskBaseDataBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // value is the data from the source object.
            TaskBase task = (TaskBase)value;

            var paramStr = parameter as String;
            
            if (paramStr.Equals("name", StringComparison.InvariantCultureIgnoreCase))
            {
                return task.Name;
            }
            else if (paramStr.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            {
                String status = "";
                switch (task.LatestTaskRunStatus)
                {
                    case TaskStatus.Passed:
                        status += "✔ Passed";
                        if (task.TimesRetried > 0)
                        {
                            status += $" (On retry {task.TimesRetried})";
                        }
                        break;
                    case TaskStatus.Failed:
                        status += "❌ Failed";
                        if (task.TimesRetried > 0)
                        {
                            status += $" (All {task.MaxNumberOfRetries} retries)";
                        }
                        break;
                    case TaskStatus.Running:
                        status += "▶ Running";
                        if (task.TimesRetried > 0)
                        {
                            status += $" (Retry {task.TimesRetried} of {task.MaxNumberOfRetries})";
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
                    default:
                        status += "❔ Unknown";
                        break;
                }

                return status;
            }
            else
            {
                throw new ArgumentException("TaskBaseQuickStatusConverter parameter is unrecognized");
            }
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
