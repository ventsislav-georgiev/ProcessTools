using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation
{
    public class ProcessWindow : INotifyPropertyChanged
    {
        public int ProcessID { get; set; }

        public IntPtr Handle { get; set; }

        public string Title { get; set; }

        public ProcessWindow(int processID, IntPtr handle, string title)
        {
            ProcessID = processID;
            Handle = handle;
            Title = title;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
