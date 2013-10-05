using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProcessInfo;

namespace ProcessTools.ModernUI
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class ProcessProperties : UserControl
    {
        private Timer _timer; 

        public ProcessProperties()
        {
            InitializeComponent();

            propertyGrid.SelectedObject = ProcessInfoMain.SelectedProcess;
            _timer = new Timer(5000);
            _timer.Start();
            _timer.Elapsed += (s, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    propertyGrid.Update();
                });
            };
        }
    }
}
