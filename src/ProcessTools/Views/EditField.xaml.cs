using System;
using System.Windows;

namespace ProcessTools.Views
{
    /// <summary>
    /// Interaction logic for EditField.xaml
    /// </summary>
    public partial class EditField : Window
    {
        private Action<string> _onAccept;

        public EditField(string title, string currentValue, Action<string> onAccept)
        {
            InitializeComponent();
            _onAccept = onAccept;
            lblTitle.Content = title; 
            txtValue.Text = currentValue;
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            _onAccept(txtValue.Text);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}