#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

#endregion

namespace ProcessTools.Core.DynamicDataDisplay
{
    public sealed class FilteringDataSource<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        private readonly IList<T> collection;

        private readonly IFilter<T> filter;

        public FilteringDataSource(IList<T> collection, IFilter<T> filter)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (filter == null)
                throw new ArgumentNullException("filter");

            this.collection = collection;

            var observableCollection = collection as INotifyCollectionChanged;
            if (observableCollection != null)
            {
                observableCollection.CollectionChanged += collection_CollectionChanged;
            }

            this.filter = filter;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (filter.Filter(collection)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseCollectionChanged();
        }

        private void RaiseCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}