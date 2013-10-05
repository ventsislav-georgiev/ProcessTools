using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WinAPI;
using WindowsInput.Native;

namespace Automation
{
    public class InputEvent : INotifyPropertyChanged
    {
        public object InputData { get; set; }

        public string InputDataStringRepresentation { get; set; }

        public List<VirtualKeyCode> Modifiers { get; set; }

        public InputDataType InputDataType { get; set; }

        public InputType InputType { get; set; }

        public int CycleCount { get; set; }

        public int Duration { get; set; }

        public InputEvent(object inputData, List<Enums.VirtualKeyCode> modifiers, string inputDataStringRepresentation, InputDataType inputDataType, InputType inputType, int cycleCount = 1, int duration = 0)
        {
            if (inputDataType == InputDataType.VirtualKey)
                InputData = (VirtualKeyCode)(Enums.VirtualKeyCode)inputData;
            else
                InputData = inputData;
            Modifiers = modifiers != null ? modifiers.Select(item => (VirtualKeyCode)item).ToList() : null;
            InputDataStringRepresentation = inputDataStringRepresentation;
            InputDataType = inputDataType;
            InputType = inputType;
            CycleCount = cycleCount;
            Duration = duration;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}