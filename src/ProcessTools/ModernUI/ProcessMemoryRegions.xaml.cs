using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using MemoryInfo;
using ProcessTools.Core.ExtendedClasses;

namespace ProcessTools.ModernUI
{
    /// <summary>
    /// Interaction logic for ProcessMemoryRegions.xaml
    /// </summary>
    public partial class ProcessMemoryRegions : UserControl
    {
        public class MemoryBasicInformationView : INotifyPropertyChanged
        {
            public string BaseAddress { get; set; }

            public string AllocationBase { get; set; }

            public string AllocationProtect { get; set; }

            public string RegionSize { get; set; }

            public string State { get; set; }

            public string Protect { get; set; }

            public string Type { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private ICollectionView _memoryRegionsView;

        private ObservableCollectionEx<MemoryBasicInformationView> _memoryRegions { get; set; }

        public ICollectionView MemoryRegionsView
        {
            get { return _memoryRegionsView; }
        }

        public ProcessMemoryRegions()
        {
            var memory = new Memory();
            memory.Open(ProcessInfoMain.SelectedProcess.Id);
            var memoryRegionsDataType = memory.GetAccessableMemoryRegions(true);

            var memoryRegions = memoryRegionsDataType.Select(item => new MemoryBasicInformationView()
            {
                AllocationBase = item.AllocationBase.ToString("x"),
                AllocationProtect = item.AllocationProtect.ToString(),
                BaseAddress = item.BaseAddress.ToString("x"),
                Protect = item.Protect.ToString(),
                RegionSize = item.RegionSize.ToString("x"),
                State = item.State.ToString(),
                Type = item.Type.ToString()
            }).ToArray();
            _memoryRegions = new ObservableCollectionEx<MemoryBasicInformationView>(memoryRegions);
            _memoryRegionsView = CollectionViewSource.GetDefaultView(_memoryRegions);

            InitializeComponent();
        }
    }
}