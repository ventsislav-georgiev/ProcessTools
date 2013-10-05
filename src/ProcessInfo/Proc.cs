using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using WinAPI;

namespace ProcessInfo
{
    public class Proc : INotifyPropertyChanged
    {
        #region Static

        public static Process GetCurrentProcess()
        {
            return Process.GetCurrentProcess();
        }

        /// <summary>
        ///   Retrieve collection of all runing processes
        /// </summary>
        /// <returns>Runing processes collection</returns>
        public static IEnumerable<Proc> GetAllProcesses(bool filterByUser, string sid = null)
        {
            return Process.GetProcesses()
                          .Where(process =>
                          {
                              bool skipProcess = false;
                              switch (process.ProcessName)
                              {
                                  case "audiodg":
                                      {
                                          skipProcess = true;
                                          break;
                                      }
                              }

                              string processSid;
                              bool isFromSameOwner = false;
                              if (!skipProcess && filterByUser && Utilities.IsUserAvailableProcess(process.ProcessName) && GetProcessOwner.ExGetProcessInfoByPid(process.Id, out processSid))
                                  isFromSameOwner = processSid == sid;

                              return !skipProcess && (!filterByUser || isFromSameOwner);
                          }).Select(proc => new Proc(proc));
        }

        #endregion Static

        #region Fields

        private readonly PerformanceCounter _threadCountCounter;
        private readonly PerformanceCounter _handleCountCounter;
        private readonly PerformanceCounter _privateWorkingSetCounter;

        private readonly Process _process;

        public Process Process
        {
            get
            {
                return _process;
            }
        }

        private readonly PerformanceCounter _totalProcessorTimeCounter;

        private bool _hasIcon = true;
        private BitmapImage _icon;

        /// <summary>
        ///   The process icon
        /// </summary>
        public BitmapImage ProcessIcon
        {
            get
            {
                bool skipProcess = !Utilities.IsUserAvailableProcess(_process.ProcessName);

                if (_hasIcon && _icon == null && !skipProcess)
                {
                    Icon icon = Icon.ExtractAssociatedIcon(_process.MainModule.FileName);
                    if (icon == null)
                        _hasIcon = false;
                    else
                    {
                        //var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        //    // ReSharper disable PossibleNullReferenceException
                        //    icon.Handle,
                        //    // ReSharper restore PossibleNullReferenceException
                        //    new Int32Rect(0, 0, icon.Width, icon.Height),
                        //    BitmapSizeOptions.FromEmptyOptions());

                        //_icon = new BitmapImage();
                        //var encoder = new JpegBitmapEncoder();
                        //encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        //var memoryStream = new MemoryStream();
                        //encoder.Save(memoryStream);

                        //_icon.BeginInit();
                        //_icon.StreamSource = new MemoryStream(memoryStream.ToArray());
                        //_icon.EndInit();
                        //memoryStream.Close();
                        var stream = new MemoryStream();
                        icon.ToBitmap().Save(stream, ImageFormat.Png);

                        _icon = new BitmapImage();
                        _icon.BeginInit();
                        _icon.StreamSource = new MemoryStream(stream.ToArray());
                        _icon.EndInit();
                    }
                }
                return _icon;
            }
        }

        /// <summary>
        ///   The PID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///   Process File Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   Process Instance Name
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        ///   Process Instance File Path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        ///   Process Instance Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///   The process status
        /// </summary>
        public string Status { get; set; }

        private double _privateWorkingSet;

        /// <summary>
        ///   The memory private working set
        /// </summary>
        public double PrivateWorkingSet
        {
            get
            {
                return _privateWorkingSet;
            }
            set
            {
                if (_privateWorkingSet == value)
                    return;
                _privateWorkingSet = value;
                OnPropertyChanged("PrivateWorkingSet");
            }
        }

        private double _threadCount;

        /// <summary>
        ///   The threads count
        /// </summary>
        public double ThreadCount
        {
            get
            {
                return _threadCount;
            }
            set
            {
                if (_threadCount == value)
                    return;
                _threadCount = value;
                OnPropertyChanged("ThreadCount");
            }
        }

        private double _handleCount;

        /// <summary>
        ///   The handles count
        /// </summary>
        public double HandleCount
        {
            get
            {
                return _handleCount;
            }
            set
            {
                if (_handleCount == value)
                    return;
                _handleCount = value;
                OnPropertyChanged("HandleCount");
            }
        }

        private double _totalProcessorTime;

        /// <summary>
        ///   The total processor time used by the process
        /// </summary>
        public double TotalProcessorTime
        {
            get
            {
                return _totalProcessorTime;
            }
            set
            {
                if (_totalProcessorTime == value)
                    return;
                _totalProcessorTime = value;
                OnPropertyChanged("TotalProcessorTime");
            }
        }

        public bool IsSystem
        {
            get
            {
                return !Utilities.IsUserAvailableProcess(_process.ProcessName);
            }
        }

        public bool HasExited
        {
            get
            {
                return !IsSystem && _process.HasExited;
            }
        }

        #endregion Fields

        #region Methods

        /// <summary>
        ///   Initialize new instance of Proc class
        /// </summary>
        public Proc()
        {
        }

        /// <summary>
        ///   Initialize new instance of Proc class
        /// </summary>
        /// <param name="process">The process from which to import values</param>
        public Proc(Process process)
        {
            _process = process;
            Id = process.Id;
            ProcessName = _process.ProcessName;
            bool isUserAvailableProcess = Utilities.IsUserAvailableProcess(_process.ProcessName);
            if (isUserAvailableProcess)
            {
                FilePath = _process.MainModule.FileName;
                Name = Path.GetFileName(FilePath);
            }
            else
            {
                Name = ProcessName;
            }
            //Description = FileVersionInfo.GetVersionInfo(FilePath).FileDescription;

            _threadCountCounter = new PerformanceCounter("Process", "Thread Count", ProcessName, true);
            _handleCountCounter = new PerformanceCounter("Process", "Handle Count", ProcessName, true);

            ////Memory
            _privateWorkingSetCounter = new PerformanceCounter("Process", "Working Set - Private", ProcessName, true);

            ////Processor time
            _totalProcessorTimeCounter = new PerformanceCounter("Process", "% Processor Time", ProcessName, true);
        }

        public static double GetMemoryValue(PerformanceCounter counter)
        {
            return Math.Round(counter.NextValue() / 1048576, 1); //(1024 * 1024);
        }

        public static double GetCpuValue(PerformanceCounter counter)
        {
            return GetCpuValue(counter.NextValue());
        }

        public static double GetCpuValue(double value)
        {
            return Math.Round(value / Environment.ProcessorCount, 1);
        }

        public void UpdateCounters()
        {
            if (_process == null || HasExited) return;
            //if (_process.Responding)
            //    Status = "Running";
            //else
            //{
            //    uint processUiThreadId = Memory.GetWindowThreadProcessId(_process.MainWindowHandle);
            //    var uiThread = _process.Threads.OfType<ProcessThread>()
            //                           .SingleOrDefault(thread => thread.Id == processUiThreadId);

            //    // ReSharper disable PossibleNullReferenceException
            //    Status = uiThread.HasValue() && uiThread.ThreadState == ThreadState.Wait
            //        // ReSharper restore PossibleNullReferenceException
            //                 ? "Suspended"
            //                 : "Not responding";
            //}

            ThreadCount = _threadCountCounter.NextValue();
            HandleCount = _handleCountCounter.NextValue();

            //Memory
            PrivateWorkingSet = GetMemoryValue(_privateWorkingSetCounter);
            //Processor time
            TotalProcessorTime = GetCpuValue(_totalProcessorTimeCounter);
        }

        public void EndTask()
        {
            _process.Kill();
        }

        public void ResumeTask()
        {
            Managed.Resume(_process);
        }

        public void SuspendTask()
        {
            Managed.Suspend(_process);
        }

        #endregion Methods

        #region PropertyChangedNotification

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string value)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(value));
            }
        }

        #endregion PropertyChangedNotification
    }
}