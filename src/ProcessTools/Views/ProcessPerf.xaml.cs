using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using ProcessInfo;
using ProcessTools.Core.DynamicDataDisplay;

namespace ProcessTools.Views
{
    /// <summary>
    ///   Interaction logic for ProcessPerf.xaml
    /// </summary>
    public partial class ProcessPerf
    {
        #region Fields

        private readonly string _counterName;
        private readonly string _instanceName;
        private double _currentTotal;

        #endregion Fields

        public ProcessPerf(string instanceName, string counterName)
        {
            _instanceName = instanceName;
            _counterName = counterName;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GraphTrayIcon.ToolTipText = _instanceName;

            //Start Graph
            CreatePerformanceGraph(_instanceName, _counterName);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CreatePerformanceGraph(string instanceName, string counterName)
        {
            var perfData = new PerformanceData(new PerformanceCounter("Process", counterName, instanceName));
            var filteredData = new FilteringDataSource<PerformanceInfo>(perfData, new MaxSizeFilter());
            var dataSource = new EnumerableDataSource<PerformanceInfo>(filteredData);
            dataSource.SetXMapping(pi => (pi.Time.TimeOfDay.TotalSeconds - (_currentTotal == 0 ? _currentTotal = pi.Time.TimeOfDay.TotalSeconds : _currentTotal)));
            dataSource.SetYMapping(pi => Proc.GetCpuValue(pi.Value));
            Plotter.AddLineGraph(dataSource, 0.8, string.Format("{0} - {1}", counterName, instanceName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            GraphTrayIcon.Dispose();
        }
    }
}