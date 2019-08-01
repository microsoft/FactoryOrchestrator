using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.FactoryOrchestrator.UWP
{
    public class TestData : INotifyPropertyChanged
    {
        private object testlock = new object();
        private Dictionary<Guid, TaskList> taskListMap = new Dictionary<Guid, TaskList>();
        public Dictionary<Guid, TaskList> TaskListMap
        {
            get { return taskListMap; }
            set
            {
                lock (testlock)
                {
                    if (value != taskListMap)
                    {
                        taskListMap = value;
                        NotifyPropertyChanged("TaskListMap");
                    }
                }
            }
        }

        public ObservableCollection<Guid> TaskListGuids
        {
            get { return new ObservableCollection<Guid>(TaskListMap.Keys); }
        }

        private Dictionary<int, Guid> testGuidsMap = new Dictionary<int, Guid>();

        public Dictionary<int, Guid> TestGuidsMap
        {
            get { return testGuidsMap; }
            set
            {

                lock (testlock)
                {
                    if (value != testGuidsMap)
                    {
                        testGuidsMap = value;
                        NotifyPropertyChanged("TestGuidsMap");
                    }
                }
            }
        }

        private ObservableCollection<String> testNames = new ObservableCollection<String>();
        public ObservableCollection<String> TestNames
        {
            get { return testNames; }
            set
            {
                lock (testlock)
                {
                    if (value != testNames)
                    {
                        testNames = value;
                        NotifyPropertyChanged("TestNames");
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }

        private Guid? selectedTaskListGuid;

        public Guid? SelectedTaskListGuid
        {
            get { return selectedTaskListGuid; }
            set
            {

                lock (testlock)
                {
                    if (value != selectedTaskListGuid)
                    {
                        selectedTaskListGuid = value;
                        NotifyPropertyChanged("TaskListGuid");
                    }
                }
            }
        }

        private ObservableCollection<String> testStatus = new ObservableCollection<String>();

        public ObservableCollection<String> TestStatus
        {
            get { return testStatus; }
            set
            {

                lock (testlock)
                {
                    if (value != testStatus)
                    {
                        testStatus = value;
                        NotifyPropertyChanged("TestStatus");
                    }
                }
            }
        }
    }
}
