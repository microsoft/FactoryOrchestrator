using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTFUWP
{
    class TestViewModel : INotifyPropertyChanged
    {
        public TestViewModel()
        {

        }
        public ObservableCollection<String> testNames
        {
            get
            {
                return testNames;
            }
            set
            {
                testNames = value;
                NotifyPropertyChanged("TestNames");
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
    }
}
