// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Core;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
{
    class TaskStatusDataBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var paramStr = parameter as String;

            if (paramStr == null)
            {
                return ConvertStatus(value, true);
            }
            if (paramStr.Equals("TaskBase", StringComparison.InvariantCultureIgnoreCase))
            {
                return ConvertStatus(value, false);
            }
            else
            {
                throw new ArgumentException("TaskBaseQuickStatusConverter parameter is unrecognized");
            }
        }

        private string ConvertStatus(object value, bool isStatus)
        {
            String status = "";
            TaskStatus statusEnum;
            // value is the data from the source object.
            TaskBase task = value as TaskBase;

            if (isStatus)
            {
                statusEnum = (TaskStatus)value;
            }
            else
            {
                statusEnum = task.LatestTaskRunStatus;
            }

            switch (statusEnum)
            {
                case TaskStatus.Passed:
                    status += resourceLoader.GetString("Passed");
                    if ((!isStatus) && (task.TimesRetried > 0))
                    {
                        status += $" ({resourceLoader.GetString("OnRetry")} {task.TimesRetried})";
                    }
                    if ((!isStatus) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} {resourceLoader.GetString("TotalRuns")})";
                    }
                    break;
                case TaskStatus.Failed:
                    status += resourceLoader.GetString("Failed");
                    if ((!isStatus) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} {resourceLoader.GetString("TotalRuns")})";
                    }
                    break;
                case TaskStatus.Running:
                    status += resourceLoader.GetString("Running");
                    if ((!isStatus) && (task.TimesRetried > 0))
                    {
                        status += $" ({resourceLoader.GetString("Retry")} {task.TimesRetried})";
                    }
                    break;
                case TaskStatus.NotRun:
                    status += resourceLoader.GetString("NotRun");
                    break;
                case TaskStatus.Aborted:
                    status += resourceLoader.GetString("Aborted");
                    if ((!isStatus) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} {resourceLoader.GetString("TotalRuns")})";
                    }
                    break;
                case TaskStatus.Timeout:
                    status += resourceLoader.GetString("TimedOut");
                    if ((!isStatus) && (task.TaskRunGuids.Count > 1) && (task.TaskRunGuids.Count > task.TimesRetried))
                    {
                        status += $" ({task.TaskRunGuids.Count} {resourceLoader.GetString("TotalRuns")})";
                    }
                    break;
                case TaskStatus.RunPending:
                    status += resourceLoader.GetString("RunPending");
                    break;
                default:
                    status += resourceLoader.GetString("Unknown");
                    break;
            }

            return status;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();
    }
}
