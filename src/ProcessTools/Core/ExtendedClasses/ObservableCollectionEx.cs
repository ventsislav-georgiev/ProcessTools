using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ProcessTools.Core.ExtendedClasses
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged
    {
        #region Fields

        public event PropertyChangedEventHandler ItemPropertyChanged;

        private bool _suspendCollectionChangeNotification = false;

        #endregion Fields

        #region Constructors

        public ObservableCollectionEx()
            : base()
        {
            CollectionChanged += new NotifyCollectionChangedEventHandler(ObservableCollectionEx_CollectionChanged);
        }

        public ObservableCollectionEx(IEnumerable<T> items)
            : base(items)
        {
            CollectionChanged += new NotifyCollectionChangedEventHandler(ObservableCollectionEx_CollectionChanged);
        }

        #endregion Constructors

        #region Methods

        //public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suspendCollectionChangeNotification)
            {
                //NotifyCollectionChangedEventHandler collectionChanged = CollectionChanged;
                //if (collectionChanged != null)
                //{
                //    foreach (NotifyCollectionChangedEventHandler nh in collectionChanged.GetInvocationList())
                //    {
                //        var dispObj = nh.Target as DispatcherObject;
                //        if (dispObj != null)
                //        {
                //            Dispatcher dispatcher = dispObj.Dispatcher;
                //            if (dispatcher != null && !dispatcher.CheckAccess())
                //            {
                //                NotifyCollectionChangedEventHandler nh1 = nh;
                //                dispatcher.BeginInvoke(
                //                    (Action)(() => nh1.Invoke(this,
                //                                               new NotifyCollectionChangedEventArgs(
                //                                                   NotifyCollectionChangedAction.Reset))),
                //                    DispatcherPriority.DataBind);
                //                continue;
                //            }
                //        }
                //        nh.Invoke(this, e);
                //    }
                //}
                base.OnCollectionChanged(e);
            }
        }

        public void SuspendCollectionChangeNotification()
        {
            _suspendCollectionChangeNotification = true;
        }

        public void ResumeCollectionChangeNotification()
        {
            _suspendCollectionChangeNotification = false;
        }

        public void RaiseOnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the ObservableCollection(Of T).
        /// </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            foreach (var i in collection) Items.Add(i);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Removes the first occurence of each item in the specified collection from ObservableCollection(Of T).
        /// </summary>
        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            foreach (var i in collection) Items.Remove(i);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Clears the current collection and replaces it with the specified item.
        /// </summary>
        public void Replace(T item)
        {
            ReplaceRange(new T[] { item });
        }

        /// <summary>
        /// Clears the current collection and replaces it with the specified collection.
        /// </summary>
        public void ReplaceRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            Items.Clear();
            foreach (var i in collection) Items.Add(i);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void ObservableCollectionEx_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged -= new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(a);

            if (ItemPropertyChanged != null)
            {
                ItemPropertyChanged(sender, e);
            }
        }

        #endregion Methods
    }
}