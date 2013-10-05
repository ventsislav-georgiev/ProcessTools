using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using Be.Windows.Forms;
using MemoryInfo;
using ProcessTools.Core;
using WinAPI;

namespace ProcessTools.Views
{
    /// <summary>
    /// Interaction logic for ProcessMemory.xaml
    /// </summary>
    public partial class ProcessMemory : Window
    {
        private List<Structs.MemoryBasicInformation> _memoryRegions;
        private Memory _memory;
        private HexBox _hexBox;
        private BitControl _bitControl;
        private ulong _currentBaseAddress;

        public ProcessMemory(Memory memory, MemoryAddress memoryAddress = null)
        {
            InitializeComponent();

            InitHexBox(wfHB.Child as HexBox);
            InitBitControl(wfBC.Child as BitControl);

            _memory = memory;
            _memoryRegions = memory.GetAccessableMemoryRegions();

            ulong baseAddress = 0;
            if (memoryAddress != null)
            {
                baseAddress = memoryAddress.Offset;
            }
            else
            {
                baseAddress = (ulong)memory.Process.MainModule.BaseAddress;
            }
            GoToAddress(baseAddress);
        }

        private void GoToAddress(ulong address)
        {
            _currentBaseAddress = address;
            var memoryRegion = FindAddressMemoryRegion(address);
            if (memoryRegion.HasValue)
            {
                var regionBaseAddress = (ulong)memoryRegion.Value.BaseAddress;
                var regionSize = (ulong)memoryRegion.Value.RegionSize;
                var regionEndAddress = regionBaseAddress + regionSize;

                var startAddress = address;
                if (regionBaseAddress > startAddress)
                    startAddress = regionBaseAddress;

                var endAddress = address + 1000;
                if (regionEndAddress > endAddress)
                    endAddress = regionEndAddress;

                _hexBox.ByteProvider = new DynamicByteProvider(GrabMemory(startAddress, endAddress));
            }
            else
            {
                Utilities.Message("Unaccessable memory.");
            }
        }

        private byte[] GrabMemory(ulong startAddress, ulong endAddress)
        {
            return _memory.Read(startAddress, (uint)(endAddress - startAddress));
        }

        private Structs.MemoryBasicInformation? FindAddressMemoryRegion(ulong address)
        {
            foreach (var region in _memoryRegions)
            {
                if (address >= (ulong)region.BaseAddress && address <= ((ulong)region.BaseAddress + (ulong)region.RegionSize))
                {
                    return region;
                }
            }

            return null;
        }

        private void InitHexBox(HexBox hexBox)
        {
            hexBox.BuiltInContextMenu.CopyMenuItemImage = null;
            hexBox.BuiltInContextMenu.CopyMenuItemText = "Copy";
            hexBox.BuiltInContextMenu.CutMenuItemImage = null;
            hexBox.BuiltInContextMenu.CutMenuItemText = "Cut";
            hexBox.BuiltInContextMenu.PasteMenuItemImage = null;
            hexBox.BuiltInContextMenu.PasteMenuItemText = "Paste";
            hexBox.BuiltInContextMenu.SelectAllMenuItemText = "Select All";
            hexBox.HexCasing = Be.Windows.Forms.HexCasing.Lower;
            hexBox.InfoForeColor = Color.Gray;
            hexBox.LineInfoVisible = true;
            hexBox.Name = "hexBox";
            hexBox.SelectionBackColor = Color.Gray;
            hexBox.SelectionForeColor = Color.White;
            hexBox.ShadowSelectionColor = Color.Gray;
            hexBox.StringViewVisible = true;
            hexBox.UseFixedBytesPerLine = true;
            hexBox.VScrollBarVisible = true;
            hexBox.ColumnInfoVisible = true;
            hexBox.ReadOnly = true;
            hexBox.CurrentLineChanged += new System.EventHandler(this.Position_Changed);
            hexBox.CurrentPositionInLineChanged += new System.EventHandler(this.Position_Changed);
            _hexBox = hexBox;
        }

        private void InitBitControl(BitControl bitControl)
        {
            bitControl.Name = "bitControl";
            bitControl.Editable = false;
            _bitControl = bitControl;
        }

        private void Position_Changed(object sender, EventArgs e)
        {
            string bitPresentation = string.Empty;

            byte? currentByte = _hexBox.ByteProvider != null && _hexBox.ByteProvider.Length > _hexBox.SelectionStart
                ? _hexBox.ByteProvider.ReadByte(_hexBox.SelectionStart)
                : (byte?)null;

            BitInfo bitInfo = currentByte != null ? new BitInfo((byte)currentByte, _hexBox.SelectionStart) : null;
            _bitControl.BitInfo = bitInfo;
        }
    }
}