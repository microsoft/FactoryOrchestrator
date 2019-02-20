using FTFTestExecution;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTFUWP
{
    public class TestData : INotifyPropertyChanged
    {
        private Dictionary<Guid, TestList> testListMap = new Dictionary<Guid, TestList>();
        public Dictionary<Guid, TestList> TestListMap
        {
            get { return testListMap; }
            set
            {
                if (value != testListMap)
                {
                    testListMap = value;
                    NotifyPropertyChanged("TestListMap");
                }
            }
        }

        public ObservableCollection<Guid> TestListGuids { get { return new ObservableCollection<Guid>(TestListMap.Keys); } }

        private ObservableCollection<String> testNames = new ObservableCollection<String>();
        public ObservableCollection<String> TestNames
        {
            get { return testNames; }
            set
            {
                if (value != testNames)
                {
                    testNames = value;
                    NotifyPropertyChanged("TestNames");
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

        private ObservableCollection<TestStatus> testStatus = new ObservableCollection<TestStatus>();

        public ObservableCollection<TestStatus> TestStatus
        {
            get { return testStatus; }
            set
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
