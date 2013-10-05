using System;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using Automation;
using ProcessInfo;
using WinAPI;

namespace ProcessTools.Views
{
    /// <summary>
    /// Interaction logic for MouseInput.xaml
    /// </summary>
    public partial class MouseInput : Window
    {
        private Action<double[], string> _onAccept;
        private Action _onCancel;
        private bool _isAccepted;

        private static Tuple<IntPtr, HotKey> _currentProcessHotKey;
        private Timer _timer;

        private bool scanningMouseCoordinates;

        public MouseInput(Action<double[], string> onAccept, Action onCancel)
        {
            _onAccept = onAccept;
            _onCancel = onCancel;

            _timer = new Timer(10);
            _timer.Elapsed += TimerUpdateCoordinates;

            var hotkey = new HotKey(Enums.VirtualKeyCode.ACCEPT, Enums.KeyModifier.None);
            _currentProcessHotKey = new Tuple<IntPtr, HotKey>(Proc.GetCurrentProcess().Handle, hotkey);
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);

            InitializeComponent();
        }

        private void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (!handled)
            {
                if (msg.message == (int)Enums.WMessages.HOTKEY)
                {
                    if (_currentProcessHotKey.Item1.Equals(msg.wParam))
                    {
                        ToggleScanMousePosition();
                        handled = true;
                    }
                }
            }
        }

        private void ToggleScanMousePosition()
        {
            if (scanningMouseCoordinates)
            {
                btnSetPosition.Content = "Set position";
                scanningMouseCoordinates = false;
                lblNotice.Visibility = System.Windows.Visibility.Hidden;

                _timer.Stop();
            }
            else
            {
                btnSetPosition.Content = "Stop following";
                scanningMouseCoordinates = true;
                lblNotice.Visibility = System.Windows.Visibility.Visible;

                _timer.Start();
            }
        }

        private void chClick_Click(object sender, RoutedEventArgs e)
        {
            Action<bool> changeRadioButtonsState = (state) =>
            {
                rdLeft.IsEnabled = state;
                rdRight.IsEnabled = state;
                //rdMiddle.IsEnabled = state;
            };

            if (!chClick.IsChecked.Value)
            {
                changeRadioButtonsState(false);
            }
            else
            {
                changeRadioButtonsState(true);
            }
        }

        private void btnSetPosition_Click(object sender, RoutedEventArgs e)
        {
            ToggleScanMousePosition();
        }

        private void TimerUpdateCoordinates(object sender, EventArgs e)
        {
            if (scanningMouseCoordinates)
            {
                var position = Managed.GetCursorPosition();
                this.Dispatcher.Invoke(() =>
                {
                    txtX.Text = position.X.ToString();
                    txtY.Text = position.Y.ToString();
                });
            }
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            Enums.VirtualKeyCode vkKey = Enums.VirtualKeyCode.VK_0;
            string button = null;
            if (rdLeft.IsChecked.Value)
            {
                vkKey = Enums.VirtualKeyCode.LBUTTON;
                button = "Left";
            }
            if (rdRight.IsChecked.Value)
            {
                vkKey = Enums.VirtualKeyCode.RBUTTON;
                button = "Right";
            }
            if (rdMiddle.IsChecked.Value)
            {
                vkKey = Enums.VirtualKeyCode.MBUTTON;
                button = "Middle";
            }

            int x = 0;
            int y = 0;
            double xAbsolute = 0;
            double yAbsolute = 0;
            if (int.TryParse(txtX.Text, out x))
                xAbsolute = ConvertXToAbsolute(x);
            if(int.TryParse(txtY.Text, out y))
                yAbsolute = ConvertYToAbsolute(y);

            string representation = String.Join("; ", "Mouse Button: " + button, "X: " + x, "Y: " + y);
            _isAccepted = true;
            _onAccept(new double[] { (uint)vkKey, x, y, xAbsolute, yAbsolute }, representation);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!_isAccepted)
                _onCancel();

            _currentProcessHotKey.Item2.Unregister();
            ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcherThreadFilterMessage;
        }


        private double ConvertXToAbsolute(int x)
        {
            double width = System.Windows.SystemParameters.PrimaryScreenWidth;

            return ((double)65535 * x) / width;
        }

        private double ConvertYToAbsolute(int y)
        {
            double height = System.Windows.SystemParameters.PrimaryScreenHeight;
            return ((double)65535 * y) / height;
        }
    }
}