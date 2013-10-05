#region

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

#endregion

namespace ProcessTools.Core
{
    internal static class Utilities
    {
        /// <summary>
        ///   Display message by MessageBox class
        /// </summary>
        /// <param name="message">The message</param>
        public static MessageBoxResult Message(string message, MessageBoxButton messageBoxButton = MessageBoxButton.OK,
                                               MessageBoxImage messageBoxImage = MessageBoxImage.Error)
        {
            return MessageBox.Show(message, "Attention!", messageBoxButton, messageBoxImage);
        }

        /// <summary>
        ///   Converts string value to integer using classic C operation
        /// </summary>
        /// <param name="value">The string value</param>
        /// <returns>The integer</returns>
        public static int IntParseFast(string value)
        {
            int result = 0;
            for (int i = 0; i < value.Length; i++)
            {
                result = 10 * result + (value[i] - 48);
            }
            return result;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}