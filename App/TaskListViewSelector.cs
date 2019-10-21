using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// A UI helper class to select the correct DataTemplate to use for TaskListsView items.
    /// </summary>
    public class TaskListViewSelector : DataTemplateSelector
    {
        // These templates are set by TaskListExecutionPage.xaml which instantiates the TaskListViewSelector.
        public DataTemplate Running { get; set; }
        public DataTemplate Paused { get; set; }
        public DataTemplate Completed { get; set; }
        public DataTemplate NotRun { get; set; }

        /// <summary>
        /// Returns the template to use for a given TaskListSummaryWithTemplate.
        /// Called every time a list item in TaskListsView changes.
        /// </summary>
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            DataTemplate dataTemplate = null;
            try
            {
                if (element != null && item != null && item is TaskListSummary)
                {
                    var list = (TaskListSummary)item;
                    switch (list.Status)
                    {
                        case TaskStatus.Running:
                        case TaskStatus.RunPending:
                            dataTemplate = Running;
                            break;
                        case TaskStatus.Aborted:
                            if (list.RunInParallel)
                            {
                                dataTemplate = Completed;
                            }
                            else
                            {
                                dataTemplate = Paused;
                            }
                            break;
                        case TaskStatus.Passed:
                        case TaskStatus.Failed:
                            dataTemplate = Completed;
                            break;
                        default:
                            dataTemplate = NotRun;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.AllExceptionsToString());
                dataTemplate = NotRun;
            }

            return dataTemplate;
        }
    }
}
