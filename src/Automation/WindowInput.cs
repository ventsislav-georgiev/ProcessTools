using System;
using WinAPI;
using WindowsInput.Native;

namespace Automation
{
    using System.Collections.Generic;
    using System.Threading;
    using WindowsInput;
    using NativeMethods = WinAPI.NativeMethods;

    public class WindowInput
    {
        public ProcessWindow Window { get; set; }

        public AutomationType AutomationType { get; set; }

        public List<InputEvent> InputEvents { get; set; }

        public WindowInput(ProcessWindow window, AutomationType automationType, List<InputEvent> inputEvents)
        {
            Window = window;
            AutomationType = automationType;
            InputEvents = inputEvents;
        }

        public void AutomateInput(CancellationToken? cancelationToken = null)
        {
            Action checkCancellation = () =>
            {
                if (cancelationToken.HasValue)
                    cancelationToken.Value.ThrowIfCancellationRequested();
            };
            foreach (var inputEvent in InputEvents)
            {
                var cycleCount = inputEvent.CycleCount;
                if (cycleCount == 0)
                {
                    while (true)
                    {
                        checkCancellation();
                        SimulateInput(inputEvent);
                    }
                }
                else
                {
                    while (cycleCount > 0)
                    {
                        checkCancellation();
                        SimulateInput(inputEvent);
                        cycleCount--;
                    }
                }
            }
        }

        public void SimulateInput(InputEvent inputEvent)
        {
            if (inputEvent.Modifiers != null)
            {
                foreach (var modifier in inputEvent.Modifiers)
                {
                    SimulateInput(AutomationType, Window, modifier, inputEvent.InputType, keyState: KeyState.Down);
                }
            }

            switch (inputEvent.InputDataType)
            {
                case InputDataType.VirtualKey:
                    {
                        VirtualKeyCode vkKey = (VirtualKeyCode)inputEvent.InputData;
                        SimulateInput(AutomationType, Window, vkKey, inputEvent.InputType);
                        break;
                    }
                case InputDataType.Char:
                    {
                        char keyChar = (char)inputEvent.InputData;
                        SimulateInput(AutomationType, Window, keyChar);
                        break;
                    }
                case InputDataType.String:
                    {
                        string text = (string)inputEvent.InputData;
                        SimulateInput(AutomationType, Window, text);
                        break;
                    }
                case InputDataType.VKXY:
                    {
                        double[] data = (double[])inputEvent.InputData;
                        var vkKeyUint = (uint)data[0];
                        bool isMoveOnly = vkKeyUint == 0;

                        var xPosition = (int)data[1];
                        var yPoistion = (int)data[2];
                        var xAbsolute = (double)data[3];
                        var yAbsolute = (double)data[4];
                        switch (AutomationType)
                        {
                            case AutomationType.Foreground:
                                {
                                    if (isMoveOnly)
                                        SimulateForegroundMousePosition(Window, xAbsolute, yAbsolute);
                                    else
                                        SimulateForegroundMouseClick(Window, (VirtualKeyCode)vkKeyUint, xAbsolute, yAbsolute);
                                    break;
                                }
                            case AutomationType.Background:
                                {
                                    if (isMoveOnly)
                                        SimulateBackgroundMousePosition(Window.Handle, xPosition, yPoistion);
                                    else
                                        SimulateBackgroundMouseClick(Window.Handle, (VirtualKeyCode)vkKeyUint, xPosition, yPoistion);
                                    break;
                                }
                        }
                        break;
                    }
            }

            if (inputEvent.Modifiers != null)
            {
                foreach (var modifier in inputEvent.Modifiers)
                {
                    SimulateInput(AutomationType, Window, modifier, inputEvent.InputType, keyState: KeyState.Up);
                }
            }

            if (inputEvent.Duration > 0)
            {
                Thread.Sleep(inputEvent.Duration);
            }
        }

        #region Static

        private static InputSimulator InputSimulator = new InputSimulator();

        public static VirtualKeyCode ConvertCharToVirtualKey(char ch)
        {
            short vkey = NativeMethods.VkKeyScan(ch);
            VirtualKeyCode retval = (VirtualKeyCode)(vkey & 0xff);
            int modifiers = vkey >> 8;
            if ((modifiers & 1) != 0) retval |= VirtualKeyCode.SHIFT;
            if ((modifiers & 2) != 0) retval |= VirtualKeyCode.CONTROL;
            if ((modifiers & 4) != 0) retval |= VirtualKeyCode.MENU;
            return retval;
        }

        public static void SimulateInput(AutomationType automationType, ProcessWindow processWindow, string text)
        {
            if (automationType == Automation.AutomationType.Foreground)
            {
                NativeMethods.AllowSetForegroundWindow(processWindow.ProcessID);
                NativeMethods.SetForegroundWindow(processWindow.Handle);
                Thread.Sleep(10);
                InputSimulator.Keyboard.TextEntry(text);
            }
            else
            {
                foreach (var keyChar in text)
                {
                    SimulateInput(automationType, processWindow, keyChar);
                }
            }
        }

        public static void SimulateInput(AutomationType automationType, ProcessWindow processWindow, char keyChar)
        {
            var vkKey = ConvertCharToVirtualKey(keyChar);
            SimulateInput(automationType, processWindow, vkKey, InputType.Keyboard);
        }

        public static void SimulateInput(AutomationType automationType, ProcessWindow processWindow, VirtualKeyCode vkKey, InputType inputType, KeyState keyState = KeyState.Press)
        {
            if (automationType == AutomationType.Foreground)
            {
                ActivateWindow(processWindow.ProcessID, processWindow.Handle);
            }

            if (keyState == KeyState.Press)
            {
                SimulateInput(automationType, processWindow, vkKey, inputType, keyState: KeyState.Down);
                SimulateInput(automationType, processWindow, vkKey, inputType, keyState: KeyState.Up);
            }
            else
            {
                uint windowMessage = 0;
                IntPtr lParam = IntPtr.Zero;
                switch (inputType)
                {
                    case InputType.Keyboard:
                        {
                            if (keyState == KeyState.Down)
                            {
                                windowMessage = (uint)Enums.WMessages.KEYDOWN;
                                lParam = GetLParam(1, vkKey, 0, 0, 0, 0);
                            }
                            else if (keyState == KeyState.Up)
                            {
                                windowMessage = (uint)Enums.WMessages.KEYUP;
                                lParam = GetLParam(1, vkKey, 0, 0, 1, 1);
                            }
                            break;
                        }
                    case InputType.Mouse:
                        {
                            windowMessage = (uint)(keyState == KeyState.Down ? Enums.WMessages.KEYDOWN : Enums.WMessages.KEYUP);
                            if (keyState == KeyState.Down)
                            {
                                windowMessage = (uint)Enums.WMessages.KEYDOWN;
                                lParam = GetLParam(1, vkKey, 0, 0, 0, 0);
                            }
                            else if (keyState == KeyState.Up)
                            {
                                windowMessage = (uint)Enums.WMessages.KEYUP;
                                lParam = GetLParam(1, vkKey, 0, 0, 1, 1);
                            }
                            break;
                        }
                }

                if (automationType == Automation.AutomationType.Background)
                {
                    var result = NativeMethods.SendMessage(processWindow.Handle, windowMessage, (IntPtr)vkKey, lParam);
                    if (result != IntPtr.Zero)
                    {
                        if (keyState == KeyState.Down)
                            NativeMethods.SendMessage(processWindow.Handle, (uint)Enums.WMessages.CHAR, (IntPtr)vkKey, lParam);
                    }
                }
                else
                {
                    switch (keyState)
                    {
                        case KeyState.Down:
                            {
                                InputSimulator.Keyboard.KeyDown(vkKey);
                                break;
                            }
                        case KeyState.Up:
                            {
                                InputSimulator.Keyboard.KeyUp(vkKey);
                                break;
                            }
                    }
                }
            }
        }

        public static void SimulateForegroundMousePosition(ProcessWindow processWindow, double x, double y)
        {
            ActivateWindow(processWindow.ProcessID, processWindow.Handle);
            InputSimulator.Mouse.MoveMouseTo(x, y);
            Thread.Sleep(10);
        }

        public static void SimulateForegroundMouseClick(ProcessWindow processWindow, VirtualKeyCode vkKey, double x, double y, KeyState keyState = KeyState.Press)
        {
            SimulateForegroundMousePosition(processWindow, x, y);
            switch (keyState)
            {
                case KeyState.Press:
                    {
                        switch (vkKey)
                        {
                            case VirtualKeyCode.MBUTTON:
                                {
                                    break;
                                }
                            case VirtualKeyCode.LBUTTON:
                                {
                                    InputSimulator.Mouse.LeftButtonClick();
                                    break;
                                }
                            case VirtualKeyCode.RBUTTON:
                                {
                                    InputSimulator.Mouse.RightButtonClick();
                                    break;
                                }
                        }
                        break;
                    }
                case KeyState.Down:
                    {
                        switch (vkKey)
                        {
                            case VirtualKeyCode.MBUTTON:
                                {
                                    break;
                                }
                            case VirtualKeyCode.LBUTTON:
                                {
                                    InputSimulator.Mouse.LeftButtonDown();
                                    break;
                                }
                            case VirtualKeyCode.RBUTTON:
                                {
                                    InputSimulator.Mouse.RightButtonDown();
                                    break;
                                }
                        }
                        break;
                    }
                case KeyState.Up:
                    {
                        switch (vkKey)
                        {
                            case VirtualKeyCode.MBUTTON:
                                {
                                    break;
                                }
                            case VirtualKeyCode.LBUTTON:
                                {
                                    InputSimulator.Mouse.LeftButtonUp();
                                    break;
                                }
                            case VirtualKeyCode.RBUTTON:
                                {
                                    InputSimulator.Mouse.RightButtonUp();
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        public static void SimulateBackgroundMousePosition(IntPtr windowHandle, int x, int y)
        {
            NativeMethods.SendMessage(windowHandle, (uint)Enums.WMessages.MOUSEMOVE, IntPtr.Zero, GetLParam(x, y));
        }

        public static void SimulateBackgroundMouseClick(IntPtr windowHandle, VirtualKeyCode vkKey, int x, int y, KeyState keyState = KeyState.Press)
        {
            if (keyState == KeyState.Press)
            {
                SimulateBackgroundMouseClick(windowHandle, vkKey, x, y, KeyState.Down);
                SimulateBackgroundMouseClick(windowHandle, vkKey, x, y, KeyState.Up);
            }
            else
            {
                uint windowMessage = 0;
                switch (keyState)
                {
                    case KeyState.Down:
                        {
                            switch (vkKey)
                            {
                                case VirtualKeyCode.MBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.MBUTTONDOWN;
                                        break;
                                    }
                                case VirtualKeyCode.LBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.LBUTTONDOWN;
                                        break;
                                    }
                                case VirtualKeyCode.RBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.RBUTTONDOWN;
                                        break;
                                    }
                            }
                            break;
                        }
                    case KeyState.Up:
                        {
                            switch (vkKey)
                            {
                                case VirtualKeyCode.MBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.MBUTTONUP;
                                        break;
                                    }
                                case VirtualKeyCode.LBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.LBUTTONUP;
                                        break;
                                    }
                                case VirtualKeyCode.RBUTTON:
                                    {
                                        windowMessage = (uint)Enums.WMessages.RBUTTONUP;
                                        break;
                                    }
                            }
                            break;
                        }
                }
                NativeMethods.SendMessage(windowHandle, windowMessage, (IntPtr)vkKey, GetLParam(x, y));
            }
        }

        public static void ActivateWindow(int processID, IntPtr windowHandle)
        {
            NativeMethods.AllowSetForegroundWindow(processID);
            while (!NativeMethods.SetForegroundWindow(windowHandle)) ;
            Thread.Sleep(10);
        }

        #endregion Static

        #region Private

        public static void CheckKeyShiftState()
        {
            /// <summary>Code if the key is toggled.</summary>
            const ushort KEY_TOGGLED = 0x1;
            /// <summary>Code for if the key is pressed.</summary>
            const ushort KEY_PRESSED = 0xF000;
            // Wait for all modifier keys to be released
            while ((NativeMethods.GetKeyState((int)VirtualKeyCode.MENU) & KEY_PRESSED) == KEY_PRESSED ||
                (NativeMethods.GetKeyState((int)VirtualKeyCode.CONTROL) & KEY_PRESSED) == KEY_PRESSED ||
                (NativeMethods.GetKeyState((int)VirtualKeyCode.SHIFT) & KEY_PRESSED) == KEY_PRESSED)
            {
                Thread.Sleep(1);
            }
        }

        private static uint GetScanCode(VirtualKeyCode key)
        {
            var keyboardLayout = NativeMethods.GetKeyboardLayout(0);
            return NativeMethods.MapVirtualKeyEx((uint)key, Enums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC_EX, keyboardLayout);
        }

        private static uint GetDwExtraInfo(Int16 repeatCount, VirtualKeyCode vkKey, byte extended, byte contextCode, byte previousState, byte transitionState)
        {
            uint lParam = (uint)repeatCount;
            uint scanCode = GetScanCode(vkKey) + 0x80;
            lParam += (uint)(scanCode * 0x10000);
            lParam += (uint)((extended) * 0x1000000);
            lParam += (uint)((contextCode * 2) * 0x10000000);
            lParam += (uint)((previousState * 4) * 0x10000000);
            lParam += (uint)((transitionState * 8) * 0x10000000);
            return lParam;
        }

        private static IntPtr GetLParam(int x, int y)
        {
            return (IntPtr)((y << 16) | (x & 0xFFFF));
        }

        private static IntPtr GetLParam(Int16 repeatCount, VirtualKeyCode vkKey, byte extended, byte contextCode, byte previousState, byte transitionState)
        {
            uint lParam = (uint)repeatCount;
            uint scanCode = GetScanCode(vkKey);
            lParam += (uint)(scanCode * 0x10000);
            lParam += (uint)((extended) * 0x1000000);
            lParam += (uint)((contextCode * 2) * 0x10000000);
            lParam += (uint)((previousState * 4) * 0x10000000);
            lParam += (uint)((transitionState * 8) * 0x10000000);
            return (IntPtr)lParam;
        }

        private static uint RemoveLeadingDigit(uint number)
        {
            return (number - ((number % (0x10000000)) * (0x10000000)));
        }

        #endregion Private
    }
}