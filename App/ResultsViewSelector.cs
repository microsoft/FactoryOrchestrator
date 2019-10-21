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
    public class ResultsViewSelector : DataTemplateSelector
    {
        // These templates are set by TaskListExecutionPage.xaml which instantiates the ActiveTaskListSelector.
        public DataTemplate Normal { get; set; }
        public DataTemplate RetryButtonShown { get; set; }

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
                if (element != null && item != null && item is TaskBaseWithTemplate)
                {
                    var taskWithTemplate = (TaskBaseWithTemplate)item;

                    switch (taskWithTemplate.Template)
                    {
                        case TaskViewTemplate.WithRetryButton:
                            dataTemplate = RetryButtonShown;
                            break;
                        default:
                            dataTemplate = Normal;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.AllExceptionsToString());
                dataTemplate = Normal;
            }

            return dataTemplate;
        }
    }
}
