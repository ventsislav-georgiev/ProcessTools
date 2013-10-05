using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using ProcessTools.Core.ExtendedClasses;
using WinAPI;

namespace ProcessTools.ModernUI
{
    /// <summary>
    /// Interaction logic for ProcessHandles.xaml
    /// </summary>
    public partial class ProcessHandles : UserControl
    {
        public class WindowInfoView : INotifyPropertyChanged
        {
            public string Title { get; set; }

            public string Class { get; set; }

            public string StyleEx { get; set; }

            public string Style { get; set; }

            public string Status { get; set; }

            public string WindowType { get; set; }

            public string CreatorVersion { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private ICollectionView _windowInfosView;

        private ObservableCollectionEx<WindowInfoView> _windowInfos { get; set; }

        public ICollectionView WindowInfosView
        {
            get { return _windowInfosView; }
        }

        public ProcessHandles()
        {
            var windowHandles = Managed.GetAllWindows(ProcessInfoMain.SelectedProcess);
            var windowInfos = windowHandles.ToArray().Select(item => Managed.GetWindowInfo((IntPtr)item));

            int windowIndex = 0;
            var windowInfosView = windowInfos.Select(item => new WindowInfoView()
            {
                Title = Managed.GetWindowText((IntPtr)windowHandles[windowIndex]),
                Class = Managed.GetClassName((IntPtr)windowHandles[windowIndex++]),
                WindowType = item.atomWindowType.ToString(),
                StyleEx = item.dwExStyle.ToString(),
                Style = item.dwStyle.ToString(),
                Status = item.dwWindowStatus.ToString(),
                CreatorVersion = item.wCreatorVersion.ToString()
            }).ToArray();
            _windowInfos = new ObservableCollectionEx<WindowInfoView>(windowInfosView);
            _windowInfosView = CollectionViewSource.GetDefaultView(_windowInfos);

            InitializeComponent();
        }
    }
}