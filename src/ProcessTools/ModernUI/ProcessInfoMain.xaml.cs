using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FirstFloor.ModernUI.Windows.Controls;
using ProcessInfo;

namespace ProcessTools.ModernUI
{
    /// <summary>
    /// Interaction logic for ProcessInfoMain.xaml
    /// </summary>
    public partial class ProcessInfoMain : ModernWindow
    {
        public static Process SelectedProcess;
        public ProcessInfoMain(Proc process)
        {
            SelectedProcess = process.Process;
            InitializeComponent();
        }
    }
}
