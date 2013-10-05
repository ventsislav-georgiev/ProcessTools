using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using WinAPI;

namespace ProcessTools.Views
{
    /// <summary>
    /// Interaction logic for KeyboardInput.xaml
    /// </summary>
    public partial class KeyboardInput : Window
    {
        private List<Enums.VirtualKeyCode> _modifiers;
        private Enums.VirtualKeyCode _key;
        private string _stringRepresentation;
        private bool _isAccepted;

        private Action<Enums.VirtualKeyCode?, List<Enums.VirtualKeyCode>, string, string> _onAccept;
        private Action _onCancel;

        public KeyboardInput(Action<Enums.VirtualKeyCode?, List<Enums.VirtualKeyCode>, string, string> onAccept, Action onCancel)
        {
            _onAccept = onAccept;
            _onCancel = onCancel;

            InitializeComponent();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == rdKey)
            {
                SetKeyInputs(true);
            }
            else
            {
                SetKeyInputs(false);
            }
        }

        private void SetTextInputs(bool enabled)
        {
            rtxtText.IsEnabled = enabled;
        }

        private void SetKeyInputs(bool enabled)
        {
            chAlt.IsEnabled = enabled;
            chCtrl.IsEnabled = enabled;
            chShift.IsEnabled = enabled;
            txtKey.IsEnabled = enabled;
            SetTextInputs(!enabled);
        }

        private void txtKey_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            _modifiers = new List<Enums.VirtualKeyCode>();
            var modifiersString = string.Empty;

            if (chAlt.IsChecked.Value)
            {
                _modifiers.Add(Enums.VirtualKeyCode.MENU);
                modifiersString += "[Alt]+";
            }
            if (chCtrl.IsChecked.Value)
            {
                _modifiers.Add(Enums.VirtualKeyCode.CONTROL);
                modifiersString += "[Ctrl]+";
            }
            if (chShift.IsChecked.Value)
            {
                _modifiers.Add(Enums.VirtualKeyCode.SHIFT);
                modifiersString += "[Shift]+";
            }

            _key = (Enums.VirtualKeyCode)KeyInterop.VirtualKeyFromKey(e.Key);
            _stringRepresentation = string.Concat(modifiersString, e.Key);
            txtKey.Text = _stringRepresentation;
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            _isAccepted = true;
            string text = null;
            if (rdText.IsChecked.Value)
            {
                text = GetTextFromRichTextBox(rtxtText);
                _stringRepresentation = new string(text.ToArray().Take(50).ToArray());
            }
            _onAccept(_key, _modifiers, text, _stringRepresentation);
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
        }

        #region Helper methods

        /// <summary>
        /// Returns the text that a <see cref="RichTextBox"/> contains.
        /// </summary>
        /// <param name="rtb"><see cref="RichTextBox"/> that contains the text.</param>
        /// <returns>String containing the text of the <see cref="RichTextBox"/> control.</returns>
        private string GetTextFromRichTextBox(RichTextBox rtb)
        {
            // Get textrange
            TextRange textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            return GetTextFromTextRange(textRange);
        }

        /// <summary>
        /// Extracts the text from a text range.
        /// </summary>
        /// <param name="range"><see cref="TextRange"/> to extract the text from.</param>
        /// <returns>Text in the text range.</returns>
        private string GetTextFromTextRange(TextRange range)
        {
            // Declare variables
            string result = string.Empty;

            if (range != null)
            {
                using (MemoryStream contentStream = new MemoryStream())
                {
                    // save to memorystream
                    range.Save(contentStream, DataFormats.Text);

                    // reset position
                    contentStream.Position = 0L;

                    // read content
                    StreamReader reader = new StreamReader(contentStream);
                    result = reader.ReadToEnd();
                }
            }

            // Return result
            return result;
        }

        #endregion Helper methods
    }
}