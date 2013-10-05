using System;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Research.DynamicDataDisplay.Common;
using ProcessInfo;

namespace ProcessTools.Core.DynamicDataDisplay
{
    public class PerformanceInfo
    {
        public DateTime Time { get; set; }

        public double Value { get; set; }
    }

    public class PerformanceData : RingArray<PerformanceInfo>
    {
        private readonly PerformanceCounter _counter;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(200);

        public PerformanceData(PerformanceCounter counter)
            : base(200)
        {
            if (counter == null)
                throw new ArgumentNullException("counter");

            this._counter = counter;
            _timer.Tick += OnTimerTick;
            Run();
        }

        public TimeSpan UpdateInterval
        {
            get { return _updateInterval; }
            set
            {
                _updateInterval = value;
                _timer.Interval = _updateInterval;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            var newInfo = new PerformanceInfo { Time = DateTime.Now, Value = Proc.GetCpuValue(_counter.NextValue()) };
            Add(newInfo);
        }

        private void Run()
        {
            _timer.Interval = _updateInterval;
            _timer.IsEnabled = true;
            _timer.Start();
        }

        public void Pause()
        {
            _timer.IsEnabled = false;
        }
    }
}