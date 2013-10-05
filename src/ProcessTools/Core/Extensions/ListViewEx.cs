#region

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

#endregion

namespace ProcessTools.Core.Extensions
{
    public static class ListViewEx
    {
        private static GridViewColumnHeader _lastHeaderClicked;
        private static ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private static DataTemplate _arrowUp;
        private static DataTemplate _arrowDown;

        public static void SetUpResources(ResourceDictionary resources)
        {
            _arrowUp = resources["HeaderTemplateArrowUp"] as DataTemplate;
            _arrowDown = resources["HeaderTemplateArrowDown"] as DataTemplate;
        }

        public static void GridViewColumnHeaderClicked(ListView listView, GridViewColumnHeader clickedHeader)
        {
            if (!clickedHeader.HasValue()) return;
            if (clickedHeader.Role == GridViewColumnHeaderRole.Padding) return;
            ListSortDirection direction;
            if (!Equals(clickedHeader, _lastHeaderClicked))
            {
                direction = ListSortDirection.Descending;
            }
            else
            {
                if (_lastDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            string sortString = null;
            if (clickedHeader.Column.DisplayMemberBinding.HasValue())
            {
                sortString = ((Binding) clickedHeader.Column.DisplayMemberBinding).Path.Path;
            }
            else
            {
                var stackPanel = clickedHeader.Column.CellTemplate.LoadContent() as StackPanel;
                foreach (object item in stackPanel.Children)
                {
                    if (item is TextBlock)
                    {
                        sortString =
                            BindingOperations.GetBinding((item as TextBlock), TextBlock.TextProperty).Path.Path;
                        break;
                    }
                }
            }
            Sort(listView, sortString, direction);

            // Remove arrow from previously sorted header
            if (_lastHeaderClicked.HasValue() && _lastHeaderClicked != clickedHeader)
            {
                _lastHeaderClicked.Column.HeaderTemplate = null;
            }
            if (!_lastHeaderClicked.HasValue())
                (listView.View as GridView).Columns[1].HeaderTemplate = null;

            if (direction == ListSortDirection.Ascending)
            {
                if (_arrowUp.HasValue())
                    clickedHeader.Column.HeaderTemplate = _arrowUp;
            }
            else
            {
                if (_arrowDown.HasValue())
                    clickedHeader.Column.HeaderTemplate = _arrowDown;
            }

            _lastHeaderClicked = clickedHeader;
            _lastDirection = direction;
        }

        public static void Sort(ListView lisView, string sortBy, ListSortDirection direction)
        {
            if (lisView.HasValue())
            {
                ICollectionView dataView =
                    CollectionViewSource.GetDefaultView(lisView.ItemsSource != null
                                                            ? lisView.ItemsSource
                                                            : lisView.Items);
                dataView.SortDescriptions.Clear();
                var sortDescription = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sortDescription);
                dataView.Refresh();
            }
        }
    }
}