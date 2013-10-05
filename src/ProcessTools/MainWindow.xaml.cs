using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Automation;
using MemoryInfo;
using Microsoft.Win32;
using ProcessInfo;
using ProcessTools.Core;
using ProcessTools.Core.ExtendedClasses;
using ProcessTools.Core.Extensions;
using ProcessTools.ModernUI;
using ProcessTools.Views;
using WinAPI;

namespace ProcessTools
{
    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Constant

        /// <summary>
        ///   RefreshAllProcessesInterval in miliseconds
        /// </summary>
        private const int RefreshAllProcessesInterval = 1400;

        /// <summary>
        /// If only processes for the current user are shown
        /// </summary>
        private bool _showOnlyCurrentUserProcesses = true;

        private bool _isProcessesViewUpdateForced;

        #endregion Constant

        #region Properties

        public static Window Window;
        private static BitmapImage _play48Image = new BitmapImage(new Uri(@"pack://application:,,,/ProcessTools;component/Content/Images/Resume_48.png"));
        private static BitmapImage _suspend48Image = new BitmapImage(new Uri(@"pack://application:,,,/ProcessTools;component/Content/Images/Suspend_48.png"));

        #region Processes

        private static readonly WindowsIdentity WindowsIdentity = WindowsIdentity.GetCurrent();

        private readonly List<ProcessPerf> _processPerfCollection = new List<ProcessPerf>();

        private ICollectionView _processesView;

        private readonly object _processesLocker = new object();

        private ObservableCollectionEx<Proc> _processes { get; set; }

        public ICollectionView ProcessesView
        {
            get { return _processesView; }
        }

        #endregion Processes

        #region MemoryWalker

        private ICollectionView _foundAddressesView;

        private List<MemoryAddress> _foundAddressesShadow { get; set; }

        private const int _foundAddresesMaxVisibleCount = 100;

        private ObservableCollectionEx<MemoryAddress> _foundAddresses { get; set; }

        public ICollectionView FoundAddressesView
        {
            get { return _foundAddressesView; }
        }

        private static DispatcherTimer _dispatcherTimer;

        #endregion MemoryWalker

        #region Automation

        private ICollectionView _automationWindowsView;

        private ObservableCollectionEx<ProcessWindow> _automationWindows { get; set; }

        public ICollectionView AutomationWindowsView
        {
            get { return _automationWindowsView; }
        }

        private ICollectionView _automationWindowInputsView;

        private Dictionary<IntPtr, WindowInput> _automationWindowInputsDictionary { get; set; }

        private ObservableCollectionEx<InputEvent> _automationWindowInputsViewItems { get; set; }

        public ICollectionView AutomationWindowInputsView
        {
            get { return _automationWindowInputsView; }
        }

        #endregion Automation

        #endregion Properties

        #region Static

        private static bool _isProcessesRefreshCycleActive = true;

        #endregion Static

        #region Constructors

        public MainWindow()
        {
            // Processes
            // Initial data loading
            _processes = new ObservableCollectionEx<Proc>(Proc.GetAllProcesses(_showOnlyCurrentUserProcesses, WindowsIdentity.User.Value));
            //foreach (Proc process in _processes)
            //    process.UpdateCounters();

            // Enable collection synchronization
            BindingOperations.EnableCollectionSynchronization(_processes, _processesLocker);
            _processesView = CollectionViewSource.GetDefaultView(_processes);

            // MemoryWalker
            _foundAddresses = new ObservableCollectionEx<MemoryAddress>();
            _foundAddressesView = CollectionViewSource.GetDefaultView(_foundAddresses);

            // Automation
            _automationWindows = new ObservableCollectionEx<ProcessWindow>();
            _automationWindowsView = CollectionViewSource.GetDefaultView(_automationWindows);
            _automationWindowInputsDictionary = new Dictionary<IntPtr, WindowInput>();
            _automationWindowInputsViewItems = new ObservableCollectionEx<InputEvent>();
            _automationWindowInputsView = CollectionViewSource.GetDefaultView(_automationWindowInputsViewItems);

            // Init GUI
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            InitializeComponent();
        }

        private bool _isTickInAction = false;

        private void DispatcherTimer_Tick(Object sender, EventArgs e)
        {
            if (_isTickInAction)
                return;

            try
            {
                _isTickInAction = true;
                if (_foundAddresses.Count < 30)
                {
                    for (int itemIndex = 0; itemIndex < _foundAddresses.Count; itemIndex++)
                    {
                        var item = _foundAddresses[itemIndex];
                        item.Value = string.Empty;
                    }
                }
            }
            catch
            {
                _foundAddresses.Clear();
            }
            finally
            {
                _isTickInAction = false;
            }
        }

        #endregion Constructors

        #region FormEvents

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Window = this;

            // MemoryWalker
            _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            _dispatcherTimer.Start();

            // Automation
            imgAutomationStart.Source = _play48Image;

            //Set the sorting
            ListViewEx.SetUpResources(Window.Resources);

            //ListViewEx.Sort(ListViewProcesses, "TotalProcessorTime", ListSortDirection.Descending);
            ListViewEx.Sort(ListViewProcesses, "Name", ListSortDirection.Ascending);

            UpdateProcessesTask();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (ProcessPerf processPerf in _processPerfCollection)
            {
                if (processPerf.HasValue() && processPerf.IsVisible)
                    processPerf.Close();
                TrayIcon.Dispose();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    {
                        ShowInTaskbar = false;
                        break;
                    }
                default:
                    {
                        ShowInTaskbar = true;
                        break;
                    }
            }
        }

        #region ListView

        private void ListViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            ListViewEx.GridViewColumnHeaderClicked((ListView)sender, e.OriginalSource as GridViewColumnHeader);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EndTaskButton.HasValue())
                EndTaskButton.IsEnabled = e.AddedItems.Count > 0;
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem == null) return;
            var process = listViewItem.Content as Proc;
            if (!process.HasValue()) return;
            // ReSharper disable PossibleNullReferenceException
            var processPerf = new ProcessPerf(process.ProcessName, "% Processor Time");
            // ReSharper restore PossibleNullReferenceException
            processPerf.Show();
            _processPerfCollection.Add(processPerf);
        }

        #region ContextMenu

        private void EndTask_Click(object sender, RoutedEventArgs e)
        {
            var proc = ListViewProcesses.SelectedItem as Proc;
            if (proc != null) proc.EndTask();
        }

        private void ResumeTask_Click(object sender, RoutedEventArgs e)
        {
            var proc = ListViewProcesses.SelectedItem as Proc;
            if (proc != null) proc.ResumeTask();
        }

        private void SuspendTask_Click(object sender, RoutedEventArgs e)
        {
            var proc = ListViewProcesses.SelectedItem as Proc;
            if (proc != null) proc.SuspendTask();
        }

        private void DetailedInfo_Click(object sender, RoutedEventArgs e)
        {
            var proc = ListViewProcesses.SelectedItem as Proc;
            if (proc != null)
            {
                var processInfo = new ProcessInfoMain(proc);
                processInfo.Show();
            }
        }

        #endregion ContextMenu

        #endregion ListView

        #region Tray

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion Tray

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.Source is TabControl)) return;
            var tabControl = e.Source as TabControl;
            if (tabControl != null)
            {
                var isProcessesTab = tabControl.SelectedIndex == 0;
                _isProcessesRefreshCycleActive = isProcessesTab;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _showOnlyCurrentUserProcesses = false;
            _isProcessesViewUpdateForced = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _showOnlyCurrentUserProcesses = _isProcessesViewUpdateForced = true;
        }

        #endregion FormEvents

        #region Processes

        /// <summary>
        ///   Start new task to refresh the running processes in the list view
        /// </summary>
        private void UpdateProcessesTask()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        int currentCycle = 0;
                        while (true)
                        {
                            if (!_isProcessesRefreshCycleActive)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            currentCycle++;

                            if (_isProcessesViewUpdateForced || currentCycle == 5)
                            {
                                _isProcessesViewUpdateForced = false;
                                currentCycle = 0;

                                Proc[] currentProcesses = Proc.GetAllProcesses(_showOnlyCurrentUserProcesses, WindowsIdentity.User.Value).ToArray();
                                var visibleProcesses = _processes.ToArray();

                                var processesToAdd = new List<Proc>();
                                var processesToRemove = new List<Proc>();

                                //Add new processes to the list view
                                foreach (var process in currentProcesses)
                                {
                                    if (!visibleProcesses.Any(item => item.Id.Equals(process.Id)))
                                        processesToAdd.Add(process);
                                }

                                //Remove the closed processes from the list view
                                foreach (Proc proc in visibleProcesses)
                                {
                                    var process = proc;
                                    if (!process.HasValue() || process.HasExited || !currentProcesses.Any(item => item.Id.Equals(process.Id)))
                                        processesToRemove.Add(proc);
                                }

                                _processes.SuspendCollectionChangeNotification();
                                _processes.AddRange(processesToAdd);
                                _processes.RemoveRange(processesToRemove);
                                _processes.ResumeCollectionChangeNotification();
                                _processes.RaiseOnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                            }

                            //Updates the properties values, by calling .NextValue for each counter
                            foreach (Proc process in _processes)
                            {
                                process.UpdateCounters();
                            }

                            Thread.Sleep(RefreshAllProcessesInterval);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Message(ex.Message);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        #endregion Processes

        #region MemoryWalker

        private Proc _memoryWalkerChosenProc = null;

        private bool _isSearching { get; set; }

        private void ChooseProcess_Click(object sender, RoutedEventArgs e)
        {
            var chooseProcess = new ItemChoose((processID) =>
            {
                _memoryWalkerChosenProc = new Proc(Process.GetProcessById(processID));
                var content = string.Concat("Chosen Process: ", _memoryWalkerChosenProc.Id);
                lblMemoryWalkerChosenProcess.Content = content;
            }, Proc.GetAllProcesses(false).ToDictionary(item => item.Id, item => item.Name), isOrderByKey: false);
            chooseProcess.ShowDialog();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (_isSearching)
                return;

            if (_memoryWalkerChosenProc == null || _memoryWalkerChosenProc.HasExited || string.IsNullOrWhiteSpace(txtSearchValue.Text))
            {
                Utilities.Message("Please choose running process first and input valid search value.");
                return;
            }

            if (txtSearchValue.Text == "0")
            {
                Utilities.Message("Please don't search values that are common.\n (System.OutOfMemory is possible if the results are alot.)");
                return;
            }

            if (btnNextSearch.IsEnabled)
            {
                btnNextSearch.IsEnabled = false;
                btnFirstSearch.Content = "First Search";
                lblFoundAddresses.Content = "Found addresses: ";

                _foundAddresses.Clear();
                return;
            }

            var memory = new Memory();
            memory.Open(_memoryWalkerChosenProc.Id);
            var sigScanner = new SigScanner(memory, isMultipleOffsetsSearch: true);

            byte[] searchValueBytes = null;
            MemoryInfo.ValueType valueType;

            try
            {
                ValueTypesUtil.GetBytesFromStringValueByType(txtSearchValue.Text, cbSearchValueType.Text, IsUnicode.IsChecked.Value, out searchValueBytes, out valueType);
            }
            catch
            {
                Utilities.Message("Please input valid search value.");
                return;
            }

            var searchValueLength = (uint)searchValueBytes.Length;
            var isUnicode = IsUnicode.IsChecked.Value;

            SearchStarted();

            Task.Run(() =>
            {
                try
                {
                    sigScanner.ScanModule(searchValueBytes);
                }
                catch (Exception ex)
                {
                    Utilities.Message(ex.Message);
                }
            }).ContinueWith((task) =>
            {
                try
                {
                    _foundAddressesShadow = new List<MemoryAddress>();
                    foreach (var address in sigScanner.FoundAddresses)
                    {
                        _foundAddressesShadow.Add(new MemoryAddress(memory, address.Value, address.Key, valueType, searchValueLength, isUnicode));
                    }

                    Window.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        SearchFinished(true);
                    }));
                }
                catch (Exception ex)
                {
                    Utilities.Message(ex.Message);
                }
            });
        }

        private void btnNextSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchValueText = txtSearchValue.Text;
            SearchStarted();

            Task.Run(() =>
            {
                try
                {
                    var addressesToRemove = new List<MemoryAddress>();
                    for (int addressIndex = 0; addressIndex < _foundAddressesShadow.Count; addressIndex++)
                    {
                        var address = _foundAddressesShadow[addressIndex];
                        if (address.ReadValueFromProcessMemory() != searchValueText)
                        {
                            addressesToRemove.Add(address);
                        }
                    }

                    foreach (var address in addressesToRemove)
                        _foundAddressesShadow.Remove(address);

                    Window.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        SearchFinished();
                    }));
                }
                catch (Exception ex)
                {
                    Utilities.Message(ex.Message);
                }
            });
        }

        private void SearchStarted()
        {
            btnFirstSearch.IsEnabled = false;
            btnNextSearch.IsEnabled = false;
            btnMemoryWalkerChooseProcess.IsEnabled = false;
            pbSearch.IsIndeterminate = true;
            pbSearch.BorderThickness = new Thickness(0);

            _isSearching = true;
        }

        private void SearchFinished(bool isFirstSearch = false)
        {
            if (isFirstSearch)
                btnFirstSearch.Content = "New search";

            lblFoundAddresses.Content = "Found addresses: " + _foundAddressesShadow.Count;
            if (_foundAddressesShadow.Count > _foundAddresesMaxVisibleCount)
                lblFoundAddresses.Content += " (Only first " + _foundAddresesMaxVisibleCount + " shown)";

            btnFirstSearch.IsEnabled = true;
            btnNextSearch.IsEnabled = true;
            btnMemoryWalkerChooseProcess.IsEnabled = true;
            pbSearch.IsIndeterminate = false;
            pbSearch.BorderThickness = new Thickness(10);

            _foundAddresses.Clear();
            if (_foundAddressesShadow.Count > _foundAddresesMaxVisibleCount)
                _foundAddresses.AddRange(_foundAddressesShadow.Take(_foundAddresesMaxVisibleCount));
            else
                _foundAddresses.AddRange(_foundAddressesShadow);

            _isSearching = false;
        }

        private string GenerateMask(int patternLength, byte[] array1, byte[] array2, int valuesDifference)
        {
            var mask = new StringBuilder(patternLength);
            for (int i = 0; i < array1.Length; i++)
                mask.Append("x");

            for (int i = array1.Length; i < valuesDifference; i++)
                mask.Append("?");

            for (int i = valuesDifference; i < valuesDifference + array2.Length; i++)
                mask.Append("x");

            return mask.ToString();
        }

        private void FoundAddressGrid_Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGridRow = sender as DataGridRow;
            if (dataGridRow != null)
            {
                var memoryAddress = dataGridRow.Item as MemoryAddress;
                if (memoryAddress != null)
                {
                    var editField = new EditField("Edit value on " + memoryAddress.OffsetString, memoryAddress.Value, (newValue) =>
                    {
                        memoryAddress.ValueSetter = newValue;
                    });
                    editField.ShowDialog();
                }
            }
        }

        private void btnBrowseMemory_Click(object sender, RoutedEventArgs e)
        {
            if (_memoryWalkerChosenProc != null && !_memoryWalkerChosenProc.HasExited)
            {
                var memory = new Memory();
                memory.Open(_memoryWalkerChosenProc.Id);
                var processMemory = new ProcessMemory(memory);
                processMemory.ShowDialog();
            }
            else
            {
                Utilities.Message("Choose valid process.");
            }
        }

        private void BrowseSelectedMemory_Click(object sender, RoutedEventArgs e)
        {
            var memoryAddress = FoundAddressGrid.SelectedItem as MemoryAddress;
            if (memoryAddress != null)
            {
                var processMemory = new ProcessMemory(memoryAddress.Memory, memoryAddress);
                processMemory.ShowDialog();
            }
            else
            {
                Utilities.Message("Choose valid process.");
            }
        }
        

        #endregion MemoryWalker

        #region Automation

        private Proc _automationChosenProc;
        private ProcessWindow _automationChosenWindow;
        private ProcessWindow _automationSelectedWindow;
        private InputEvent _automationCurrentInputEvent;

        private void btnAutomationChooseProcess_Click(object sender, RoutedEventArgs e)
        {
            _automationChosenWindow = null;
            lblAutomationChosenWindow.Content = "No Window Chosen";

            var chooseProcess = new ItemChoose((processID) =>
            {
                _automationChosenProc = new Proc(Process.GetProcessById(processID));
                var content = string.Concat("PID: " + _automationChosenProc.Id, " Name: " + _automationChosenProc.Name);
                lblAutomationChosenProcess.Content = content;
            },
            Proc.GetAllProcesses(false).ToDictionary(item => item.Id, item => item.Name),
            isOrderByKey: false,
            onCancel: () =>
            {
                _automationChosenProc = null;
                lblAutomationChosenProcess.Content = "No Process Chosen";
            });
            chooseProcess.ShowDialog();
        }

        private void btnAutomationChooseWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_automationChosenProc == null)
            {
                Utilities.Message("Please choose process first.");
                return;
            }

            //Structs.WindowInfo windowInfo;
            var allWindowHandles = Managed.GetAllWindows(_automationChosenProc.Process)
                .OfType<IntPtr>()
                .Distinct();
            //.Where(item => ((windowInfo = Managed.GetWindowInfo(item)).dwStyle.HasFlag(Enums.WindowStyles.WS_CAPTION) &&
            //    windowInfo.dwStyle.HasFlag(Enums.WindowStyles.WS_VISIBLE)) ||
            //    windowInfo.dwExStyle.HasFlag(Enums.WindowStylesEx.WS_EX_APPWINDOW)).ToList();

            string title = null;
            string className = null;
            Structs.WindowInfo windowInfo;
            var allWindowHandlesDictionary = allWindowHandles
                .ToDictionary(
                    item => (int)item,
                    item => (
                        (!string.IsNullOrWhiteSpace((title = Managed.GetWindowText(item))) ? title : string.Empty) + ";" +
                        (!string.IsNullOrWhiteSpace(className = Managed.GetClassName(item)) ? "ClassName: " + className : string.Empty) + ";Visible: " +
                        ((windowInfo = Managed.GetWindowInfo(item)).dwStyle.HasFlag(Enums.WindowStyles.WS_CAPTION) ? "True" : "False")
                    )
                );

            var chooseWindow = new ItemChoose((windowHandle) =>
            {
                var ptrWindowHandle = (IntPtr)windowHandle;
                _automationChosenWindow = new ProcessWindow(_automationChosenProc.Id, ptrWindowHandle, Managed.GetWindowText(ptrWindowHandle));
                var content = string.Concat("WindowHandle: " + windowHandle, " Name: " + _automationChosenWindow.Title);
                lblAutomationChosenWindow.Content = content;
            },
            allWindowHandlesDictionary,
            isOrderByKey: false,
            onCancel: () =>
            {
                _automationChosenWindow = null;
                lblAutomationChosenWindow.Content = "No Window Chosen";
            });
            chooseWindow.ShowDialog();
        }

        private void btnAutomationAddWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_automationChosenWindow != null && _automationWindows.All(window => window.Handle != _automationChosenWindow.Handle))
            {
                _automationWindows.Add(_automationChosenWindow);
                _automationWindowInputsDictionary.Add(_automationChosenWindow.Handle, null);
            }
        }

        private void btnAutomationRemoveWindow_Click(object sender, RoutedEventArgs e)
        {
            var processWindow = lbAutomationWindows.SelectedItem as ProcessWindow;
            if (processWindow != null)
            {
                _automationWindows.Remove(processWindow);
                _automationWindowInputsViewItems.Clear();
                _automationWindowInputsDictionary.Remove(processWindow.Handle);
                lbAutomationWindows.SelectedIndex = 0;
            }
        }

        private void btnAutomationKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (_automationSelectedWindow == null)
            {
                Utilities.Message("Please first select window for input automation.");
                return;
            }

            var keyboardInput = new KeyboardInput((key, modifiers, text, stringRepresentation) =>
            {
                object inputData = null;
                Automation.InputDataType dataType;
                if (text != null)
                {
                    inputData = text;
                    dataType = Automation.InputDataType.String;
                }
                else
                {
                    inputData = key.Value;
                    dataType = Automation.InputDataType.VirtualKey;
                }

                _automationCurrentInputEvent = new InputEvent(inputData, modifiers, stringRepresentation, dataType, Automation.InputType.Keyboard);
                txtAutomation.Text = stringRepresentation;
            }, () =>
            {
                _automationCurrentInputEvent = null;
                txtAutomation.Text = "";
            });
            keyboardInput.ShowDialog();
        }

        private void btnAutomationMouse_Click(object sender, RoutedEventArgs e)
        {
            if (_automationSelectedWindow == null)
            {
                Utilities.Message("Please first select window for input automation.");
                return;
            }

            var mouseInput = new MouseInput((data, representation) =>
            {
                object inputData = data;
                Automation.InputDataType dataType = InputDataType.VKXY;

                _automationCurrentInputEvent = new InputEvent(inputData, null, representation, dataType, Automation.InputType.Mouse);
                txtAutomation.Text = representation;
            }, () =>
            {
                _automationCurrentInputEvent = null;
                txtAutomation.Text = "";
            });
            mouseInput.ShowDialog();
        }

        private void btnAutomationAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_automationSelectedWindow != null && _automationCurrentInputEvent != null)
            {
                int cycle = 0;
                int duration = 0;
                int.TryParse(txtCycle.Text, out cycle);
                int.TryParse(txtDuration.Text, out duration);
                _automationCurrentInputEvent.CycleCount = cycle;
                _automationCurrentInputEvent.Duration = duration;

                var automationWindowInputs = _automationWindowInputsDictionary[_automationSelectedWindow.Handle];
                bool isCreate = automationWindowInputs == null;
                if (isCreate)
                    automationWindowInputs = new WindowInput(_automationSelectedWindow, (AutomationType)Enum.Parse(typeof(AutomationType), cbAutomationType.Text), new List<InputEvent>());

                automationWindowInputs.InputEvents.Add(_automationCurrentInputEvent);
                if (isCreate)
                    _automationWindowInputsDictionary[_automationSelectedWindow.Handle] = automationWindowInputs;
                AutomationUpdateInputEvents(_automationCurrentInputEvent);
                _automationCurrentInputEvent = null;
                txtAutomation.Text = "";
            }
        }

        private void btnAutomationRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedAutomation = AutomationWindowInputsDataGrid.SelectedItem as InputEvent;
            if (selectedAutomation != null)
            {
                var automationWindowInputs = _automationWindowInputsDictionary[_automationSelectedWindow.Handle];
                if (automationWindowInputs != null)
                {
                    automationWindowInputs.InputEvents.Remove(selectedAutomation);
                    AutomationUpdateInputEvents(selectedAutomation, false);
                }
            }
        }

        private void ListBoxAutomationWindows_Item_Selected(object sender, RoutedEventArgs e)
        {
            _automationSelectedWindow = (sender as ListBoxItem).Content as ProcessWindow;
            if (_automationSelectedWindow != null)
            {
                lblAutomationSelectedWindow.Content = _automationSelectedWindow.Title;
                AutomationUpdateInputEvents();
            }
        }

        private void AutomationUpdateInputEvents(InputEvent inputEvent = null, bool isAdd = true)
        {
            if (inputEvent == null)
            {
                _automationWindowInputsViewItems.Clear();
                WindowInput windowInput = null;
                if (_automationWindowInputsDictionary.TryGetValue(_automationSelectedWindow.Handle, out windowInput))
                {
                    if (windowInput != null)
                    {
                        _automationWindowInputsViewItems.AddRange(windowInput.InputEvents);
                    }
                }
            }
            else
            {
                object selectedItem = inputEvent;
                if (isAdd)
                    _automationWindowInputsViewItems.Add(inputEvent);
                else
                {
                    _automationWindowInputsViewItems.Remove(inputEvent);
                    if (AutomationWindowInputsDataGrid.Items.Count > 0)
                        selectedItem = AutomationWindowInputsDataGrid.Items[0];
                    else
                        selectedItem = null;
                }
                AutomationWindowInputsDataGrid.SelectedItem = selectedItem;
            }
        }

        private bool _automationIsInAction = false;
        private bool _automationIsAsync = false;
        private List<Task> _taskPool;
        private CancellationTokenSource _cancellationTokenSource;

        private void imgAutomationStart_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Action stop = () =>
            {
                if (_taskPool != null && _cancellationTokenSource != null)
                {
                    if (_taskPool.Any(task => task.Status != TaskStatus.RanToCompletion &&
                        task.Status != TaskStatus.Faulted &&
                        task.Status != TaskStatus.Canceled))
                        _cancellationTokenSource.Cancel(true);

                    foreach (var task in _taskPool)
                    {
                        try
                        {
                            if (_taskPool != null && _cancellationTokenSource != null)
                                task.Wait(_cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException) { }
                    }
                    _taskPool = null;
                    _cancellationTokenSource = null;
                }

                if (_automationIsInAction)
                {
                    var stopAction = new Action(() =>
                    {
                        imgAutomationStart.Source = _play48Image;
                        foreach (Button button in Utilities.FindVisualChildren<Button>(gAutomation))
                        {
                            button.IsEnabled = true;
                        }

                        lblAutomationStatus.Content = "Status: Stopped";
                        lblAutomationCount.Content = (int.Parse(lblAutomationCount.Content as string) + 1).ToString();
                        _automationIsInAction = false;
                        _automationIsAsync = false;
                    });

                    Window.Dispatcher.Invoke(DispatcherPriority.Normal, stopAction);
                }
            };

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (imgAutomationStart.Source == _play48Image)
                {
                    if (_automationIsInAction)
                    {
                        Utilities.Message("There is automation already starting up.");
                        return;
                    }

                    _automationIsInAction = true;
                    _automationIsAsync = chAutomationIsAsync.IsChecked.Value;

                    _taskPool = new List<Task>();
                    _cancellationTokenSource = new CancellationTokenSource();

                    imgAutomationStart.Source = _suspend48Image;
                    foreach (Button button in Utilities.FindVisualChildren<Button>(gAutomation))
                    {
                        button.IsEnabled = false;
                    }

                    lblAutomationStatus.Content = "Status: Running";
                    Action<Task> endingTask = (task) =>
                    {
                        try
                        {
                            if (task.Status != TaskStatus.Canceled)
                                stop();
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is OperationCanceledException))
                                Utilities.Message(ex.Message);
                        }
                    };

                    var mainTask = Task.Run(() =>
                    {
                        try
                        {
                            if (_automationIsAsync)
                            {
                                var unfinishedCount = _automationWindowInputsDictionary.Count;
                                foreach (var windowInput in _automationWindowInputsDictionary)
                                {
                                    _taskPool.Add(Task.Run(() =>
                                    {
                                        windowInput.Value.AutomateInput(_cancellationTokenSource.Token);
                                    }, _cancellationTokenSource.Token).ContinueWith((task) =>
                                    {
                                        unfinishedCount--;
                                    }, _cancellationTokenSource.Token));
                                }

                                var waitTask = Task.Run(() =>
                                {
                                    try
                                    {
                                        while (unfinishedCount > 0)
                                        {
                                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                            Thread.Sleep(1000);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!(ex is OperationCanceledException))
                                            Utilities.Message(ex.Message);
                                    }
                                }, _cancellationTokenSource.Token);
                                waitTask.ContinueWith(endingTask);

                                _taskPool.Add(waitTask);
                            }
                            else
                            {
                                foreach (var windowInput in _automationWindowInputsDictionary)
                                {
                                    windowInput.Value.AutomateInput(_cancellationTokenSource.Token);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is OperationCanceledException))
                                Utilities.Message(ex.Message);
                        }
                    }, _cancellationTokenSource.Token);

                    if (!_automationIsAsync && _taskPool != null)
                    {
                        mainTask.ContinueWith(endingTask);
                        _taskPool.Add(mainTask);
                    }
                }
                else
                {
                    stop();
                }
            }
        }

        private void cbAutomationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_automationSelectedWindow != null)
            {
                WindowInput windowInput = null;
                if (_automationWindowInputsDictionary.TryGetValue(_automationSelectedWindow.Handle, out windowInput))
                {
                    if (windowInput != null)
                    {
                        var selectedOption = (cbAutomationType.SelectedItem as ComboBoxItem).Content as string;
                        windowInput.AutomationType = (AutomationType)Enum.Parse(typeof(AutomationType), selectedOption);
                    }
                }
            }
        }

        #endregion Automation

        #region Miscellaneous

        private static readonly Task GetProcessesUsingFileTask = null;

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            if (GetProcessesUsingFileTask.HasValue())
                return;

            var dlg = new OpenFileDialog();
            bool? result = dlg.ShowDialog();
            if (result != true) return;
            txtChoosenFile.Text = string.Empty;
            ListBoxProcessesUsingTheFile.Items.Clear();

            var processWrapper = new ProcessWrapper(@"handle.exe",
                                                    string.Concat(dlg.FileName, " /accepteula"),
                                                    redirectStandardOutput: true);
            const string matchPattern = @"(?<=\s+pid:\s+)\b(\d+)\b(?=\s+)";
            string output = processWrapper.Execute();
            foreach (Match match in Regex.Matches(output, matchPattern))
            {
                int processId = int.Parse(match.Value);
                Process process = Process.GetProcessById(processId);
                string processName;
                try
                {
                    processName = Path.GetFileName(process.MainModule.FileName);
                }
                catch
                {
                    processName = process.ProcessName;
                }
                string processListBoxItem = string.Concat(process.Id, " ", processName);
                if (!ListBoxProcessesUsingTheFile.Items.Contains(processListBoxItem))
                    ListBoxProcessesUsingTheFile.Items.Add(processListBoxItem);
            }
            txtChoosenFile.Text = dlg.FileName;
            //GetProcessesUsingFileTask = Task.Run(() =>
            //{
            //    var processListBoxItems = new List<string>();
            //    foreach (var process in DetectOpenFiles.GetProcessesUsingFile(dlg.FileName))
            //    {
            //        string processName = null;
            //        try { processName = Path.GetFileName(process.MainModule.FileName); }
            //        catch { processName = process.ProcessName; }
            //        var processListBoxItem = string.Concat(process.Id, " ", processName);
            //        if (!processListBoxItems.Contains(processListBoxItem))
            //            processListBoxItems.Add(processListBoxItem);
            //    }
            //    return processListBoxItems;
            //}).ContinueWith((task) =>
            //{
            //    foreach (var item in task.Result)
            //    {
            //        ListBoxProcessesUsingTheFile.Items.Add(item);
            //    }
            //    txtChoosenFile.Text = dlg.FileName;
            //    GetProcessesUsingFileTask = null;
            //}, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion Miscellaneous
    }
}