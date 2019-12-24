// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FFXIVTataruHelper.EventArguments;

namespace FFXIVTataruHelper.TataruComponentModel
{
    public class AsyncBindingList<T> : BindingList<T>, INotifyListChanged<T>
    {
        #region Constructors

        public AsyncBindingList() : base()
        {
            this._AsyncListChanged = new AsyncEvent<AsyncListChangedEventHandler<T>>(this.EventErrorHandler, "AsyncBindingList \n AsyncListChanged");
        }

        /// <summary>
        /// Constructor that allows substitution of the inner list with a custom list.
        /// </summary>
        public AsyncBindingList(IList<T> list) : base(list)
        {
            this._AsyncListChanged = new AsyncEvent<AsyncListChangedEventHandler<T>>(this.EventErrorHandler, "AsyncBindingList \n AsyncListChanged");
        }

        #endregion

        #region Events

        public event AsyncEventHandler<AsyncListChangedEventHandler<T>> AsyncListChanged
        {
            add { this._AsyncListChanged.Register(value); }
            remove { this._AsyncListChanged.Unregister(value); }
        }
        private AsyncEvent<AsyncListChangedEventHandler<T>> _AsyncListChanged;

        #endregion

        protected override void ClearItems()
        {
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            T itemToDelete = this.Items[index];

            var tmp = new ListChangedEventArgs(ListChangedType.ItemDeleted, index);

            var ea = new AsyncListChangedEventHandler<T>(this, itemToDelete, new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
            //Task.Run(async () =>
            //{
                 _AsyncListChanged.InvokeAsync(ea);
            //});

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemChanged:
                    {
                        T item = this.Items[e.NewIndex];
                        var ea = new AsyncListChangedEventHandler<T>(this, item, e);
                        //Task.Run(async () =>
                        //{
                            _AsyncListChanged.InvokeAsync(ea);
                        //});
                    }
                    break;
                case ListChangedType.ItemAdded:
                    {
                        T item = this.Items[e.NewIndex];
                        var ea = new AsyncListChangedEventHandler<T>(this, item, e);
                        //Task.Run(async () =>
                        //{
                             _AsyncListChanged.InvokeAsync(ea);
                        //});
                    }
                    break;
            }

            base.OnListChanged(e);
        }


        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }
    }
}
