using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ProcessTools.Core.ExtendedClasses;

namespace ProcessTools.Views
{
    /// <summary>
    /// Interaction logic for ProcessChoose.xaml
    /// </summary>
    public partial class ItemChoose : Window
    {
        public class Pair : INotifyPropertyChanged
        {
            public int Key { get; set; }

            public string Value { get; set; }

            public string Value2 { get; set; }

            public string Value3 { get; set; }

            public string Value4 { get; set; }

            public string Value5 { get; set; }

            public int ValuesCount { get; set; }

            public Pair(int key, string value)
            {
                Key = key;
                var valuesArray = value.Split(';');
                ValuesCount = valuesArray.Length;

                for (int valueIndex = 0; valueIndex < valuesArray.Length; valueIndex++)
                {
                    var currentValue = valuesArray[valueIndex];
                    switch (valueIndex + 1)
                    {
                        case 1:
                            Value = currentValue;
                            break;

                        case 2:
                            Value2 = currentValue;
                            break;

                        case 3:
                            Value3 = currentValue;
                            break;

                        case 4:
                            Value4 = currentValue;
                            break;

                        case 5:
                            Value5 = currentValue;
                            break;
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private Action<int> _onChoose;

        private bool _itemChoosen;

        private Action _onCancel;

        public ObservableCollectionEx<Pair> ItemsCollection;

        private ICollectionView _itemsView;

        public ICollectionView ItemsView
        {
            get { return _itemsView; }
        }

        public ItemChoose(Action<int> onChoose, Dictionary<int, string> items, bool isOrderByKey = true, Action onCancel = null)
        {
            _onChoose = onChoose;
            _onCancel = onCancel;

            if (isOrderByKey)
                ItemsCollection = new ObservableCollectionEx<Pair>(items
                    .OrderBy(item => item.Key)
                    .Select(item => new Pair(item.Key, item.Value)));
            else
                ItemsCollection = new ObservableCollectionEx<Pair>(items
                    .OrderBy(item => item.Value)
                    .Select(item => new Pair(item.Key, item.Value)));

            _itemsView = CollectionViewSource.GetDefaultView(ItemsCollection);

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var firstItem = ItemsCollection.FirstOrDefault();
            if (firstItem != null)
            {
                var columns = Items.Columns.Skip(firstItem.ValuesCount + 1);
                for (int columnIndex = 0; columnIndex < columns.Count(); columnIndex++)
                {
                    columns.ElementAt(columnIndex).Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        private void Items_Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGridRow = sender as DataGridRow;
            var pair = dataGridRow.Item as Pair;
            if (pair != null)
            {
                _itemChoosen = true;
                _onChoose(pair.Key);
            }
            Close();
        }

        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var character = e.Key.ToString().ToLower();

            var pairs = Items.Items.OfType<Pair>().ToArray();
            for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
            {
                var pair = pairs[pairIndex];
                if (pair.Value.ToLower().StartsWith(character))
                {
                    Items.ScrollIntoView(pair, null);
                    break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!_itemChoosen && _onCancel != null)
                _onCancel();
        }
    }
}