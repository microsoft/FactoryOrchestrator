using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.FactoryTestFramework.UWP
{
    public class TestData : INotifyPropertyChanged
    {
        private object testlock = new object();
        private Dictionary<Guid, TestList> testListMap = new Dictionary<Guid, TestList>();
        public Dictionary<Guid, TestList> TestListMap
        {
            get { return testListMap; }
            set
            {
                lock (testlock)
                {
                    if (value != testListMap)
                    {
                        testListMap = value;
                        NotifyPropertyChanged("TestListMap");
                    }
                }
            }
        }

        public ObservableCollection<Guid> TestListGuids
        {
            get { return new ObservableCollection<Guid>(TestListMap.Keys); }
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

        private Guid? selectedTestListGuid;

        public Guid? SelectedTestListGuid
        {
            get { return selectedTestListGuid; }
            set
            {

                lock (testlock)
                {
                    if (value != selectedTestListGuid)
                    {
                        selectedTestListGuid = value;
                        NotifyPropertyChanged("TestListGuid");
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
