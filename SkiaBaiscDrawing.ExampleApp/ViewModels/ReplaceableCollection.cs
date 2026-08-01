using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.ViewModels
{
    public class ReplaceableCollection<T> : ObservableCollection<T>
    {


        public void ReplaceAll(IEnumerable<T> items)
        {
            var buffer = items as IList<T> ?? items.ToList();

            if (Count == buffer.Count && Items.SequenceEqual(buffer))
                return;

            base.ClearItems();                       // 操作底層，不發事件
            foreach (var item in buffer)
                base.Add(item);
            
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
