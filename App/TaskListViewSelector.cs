using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                if (element != null && item != null && item is TaskListSummaryWithTemplate)
                {
                    var list = item as TaskListSummaryWithTemplate;
                    if (list != null)
                    {
                        switch (list.Template)
                        {
                            case TaskListViewTemplate.Completed:
                                dataTemplate = Completed;
                                break;
                            case TaskListViewTemplate.Running:
                                dataTemplate = Running;
                                break;
                            case TaskListViewTemplate.Paused:
                                dataTemplate = Paused;
                                break;
                            case TaskListViewTemplate.NotRun:
                                dataTemplate = NotRun;
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.AllExceptionsToString());
            }

            return dataTemplate;
        }
    }
}
