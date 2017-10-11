using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using CellUtils;
using UniRx;
using ZergRush;

#if !NETCORE
using UnityEngine;
#endif

namespace ZergRush
{
    public interface IObservableCollection<T> : ICollection<T>
    {
        IStream<int> ObserveCountChanged();
        IStream<CollectionAddEvent<T>> ObserveAdd();
        IStream<CollectionMoveEvent<T>> ObserveMove();
        IStream<CollectionReplaceEvent<T>> ObserveReplace();
        IStream<CollectionRemoveEvent<T>> ObserveRemove();
        IStream<UniRx.Unit> ObserveReset();
        ICell<ICollection<T>> asCell { get; }
    }

#if !NETCORE
    public static  class ObservableExtension
    {
        public static UniRx.IObservable<Vector2> EveryTapOutsideRectTransform(RectTransform rectTransform)
        {
            return ObservableExtension.EveryTap().Where( position =>
            {
                var worldCamera = rectTransform.GetComponentInParent<Canvas>().worldCamera;

                Vector2 localPoint = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, worldCamera, out localPoint);

                return !rectTransform.rect.Contains(localPoint);
            });
        }

        public static UniRx.IObservable<Vector2> EveryTap()
        {
            return Observable.EveryUpdate().Where(l =>
            {
                if (!Application.isMobilePlatform && Input.GetMouseButtonDown(0))
                {
                    return true;
                }

                if (Application.isMobilePlatform && Input.touchCount > 0 && Input.touches.Any(touch => touch.phase == TouchPhase.Began))
                {
                    return true;
                }
                return false;

            }).Select(l =>
            {
                if (!Application.isMobilePlatform && Input.GetMouseButtonDown(0))
                {
                    return (Vector2)Input.mousePosition;
                }

                return Input.touches.First(touch => touch.phase == TouchPhase.Began).position;
            });
        }
    }
#endif
    public static class ReactiveCollectionExtensions
    {
        public static IDisposable ProcessEach<T>(this IObservableCollection<T> self, Action<T> func, Action<T> onRemove = null)
        {
            foreach (var item in self)
            {
                func(item);
            }
            var finalDisp = new ListDisposable();
            var collection = new Collection<T>();
            finalDisp.Add(self.ObserveReset().Listen(unit =>
            {
                if (onRemove != null)
                    foreach (var element in collection)
                    {
                        onRemove(element);
                    }

                collection.Clear();
            }));
            finalDisp.Add(self.ObserveAdd().Listen(add =>
            {
                func(add.Value);
                collection.Add(add.Value);
            }));
            finalDisp.Add(self.ObserveReplace().Listen(replaceEvent =>
            {
                collection.Add(replaceEvent.NewValue);
                collection.Remove(replaceEvent.OldValue);
                func(replaceEvent.NewValue);
                if (onRemove != null)
                {
                    onRemove(replaceEvent.OldValue);
                }
            }));

            if (onRemove != null)
            {
                finalDisp.Add(self.ObserveRemove().Listen(remove =>
                {
                    onRemove(remove.Value);
                    collection.Remove(remove.Value);
                }));
            }
            return finalDisp;
        }

        public static UniRx.Tuple<IObservableCollection<T>, IDisposable> Filter<T>(this IObservableCollection<T> collection, Func<T, ICell<bool>> predicate)
        {
            var inorganicCollection = new InorganicCollection<T>();

            var disposables = new Dictionary<T, IDisposable>();

            var foreachDisp = collection.ProcessEach(objAdd =>
            {
                var value = objAdd;
                var disp = predicate(value).Bind(b =>
                {
                    if (b)
                    {
                        if (inorganicCollection.Contains(value))
                            return;

                        inorganicCollection.Add(value);
                    }
                    else
                        inorganicCollection.Remove(value);
                });
                disposables[value] = disp;
            }, objRemove =>
            {
                inorganicCollection.Remove(objRemove);
                if (disposables.ContainsKey(objRemove))
                {
                    var disposable = disposables[objRemove];
                    disposables.Remove(objRemove);
                    disposable.Dispose();
                }
                else
                {
                    Debug.Log("disposable is not exist");                 
                }
            });

            return new UniRx.Tuple<IObservableCollection<T>, IDisposable>(inorganicCollection, foreachDisp);
        }

        public static IEmptyStream ObserveChange<T>(this IObservableCollection<T> self)
        {
            return self.asCell.UpdateStream();
        }
    }
    

    public interface IReadOnlyCollection<T> : IEnumerable<T>
    {
        int Count { get; }
        T ElementAt(int index);
    }

    public interface IReactiveCollection<T> : IReadOnlyCollection<T>
    {
        IDisposable Listen(
            Action<CollectionAddEvent<T>> onAdd,
            Action<CollectionRemoveEvent<T>> onRemove,
            Action<CollectionMoveEvent<T>> onMove,
            Action<CollectionReplaceEvent<T>> onReplace,
            Action onReset
        );
    }

    [Serializable]
    public class InorganicCollection<T> : Collection<T>, IObservableCollection<T>
    {
        public InorganicCollection()
        {

        }

        public InorganicCollection(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public InorganicCollection(List<T> list)
                : base(list != null ? new List<T>(list) : null)
        {
        }

        protected override void ClearItems()
        {
            var beforeCount = Count;
            base.ClearItems();

            if (collectionReset != null) collectionReset.Send(UniRx.Unit.Default);
            if (updates != null) updates.value = this;
            if (beforeCount > 0)
            {
                if (countChanged != null) countChanged.Send(Count);
            }
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            if (collectionAdd != null) collectionAdd.Send(new CollectionAddEvent<T>(index, item));
            if (countChanged != null) countChanged.Send(Count);
            if (updates != null) updates.value = this;
        }

        public void Move(int oldIndex, int newIndex)
        {
            MoveItem(oldIndex, newIndex);
        }

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            T item = this[oldIndex];
            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, item);

            if (collectionMove != null) collectionMove.Send(new CollectionMoveEvent<T>(oldIndex, newIndex, item));
            if (updates != null) updates.value = this;
        }

        protected override void RemoveItem(int index)
        {
            T item = this[index];
            base.RemoveItem(index);

            if (collectionRemove != null) collectionRemove.Send(new CollectionRemoveEvent<T>(index, item));
            if (countChanged != null) countChanged.Send(Count);
            if (updates != null) updates.value = this;
        }

        public void RemoveItem(Func<T, bool> filter)
        {
            var element = this.FirstOrDefault(filter);
            if (element != null)
            {
                RemoveItem(IndexOf(element));
            }
        }

        protected override void SetItem(int index, T item)
        {
            T oldItem = this[index];
            base.SetItem(index, item);

            if (collectionReplace != null) collectionReplace.Send(new CollectionReplaceEvent<T>(index, oldItem, item));
            updates.value = this;
        }

        public void Set(IEnumerable<T> newData)
        {
            ClearItems();
            foreach (var i in newData)
            {
                Add(i);
            }
        }
        
        [NonSerialized]
        Stream<int> countChanged = null;
        public IStream<int> ObserveCountChanged()
        {
            return countChanged ?? (countChanged = new Stream<int>());
        }

        [NonSerialized]
        Stream<UniRx.Unit> collectionReset = null;
        public IStream<UniRx.Unit> ObserveReset()
        {
            return collectionReset ?? (collectionReset = new Stream<UniRx.Unit>());
        }

        

        [NonSerialized]
        Stream<CollectionAddEvent<T>> collectionAdd = null;
        public IStream<CollectionAddEvent<T>> ObserveAdd()
        {
            return collectionAdd ?? (collectionAdd = new Stream<CollectionAddEvent<T>>());
        }

        [NonSerialized]
        Stream<CollectionMoveEvent<T>> collectionMove = null;
        public IStream<CollectionMoveEvent<T>> ObserveMove()
        {
            return collectionMove ?? (collectionMove = new Stream<CollectionMoveEvent<T>>());
        }

        [NonSerialized]
        Stream<CollectionRemoveEvent<T>> collectionRemove = null;
        public IStream<CollectionRemoveEvent<T>> ObserveRemove()
        {
            return collectionRemove ?? (collectionRemove = new Stream<CollectionRemoveEvent<T>>());
        }

        [NonSerialized]
        Stream<CollectionReplaceEvent<T>> collectionReplace = null;
        public IStream<CollectionReplaceEvent<T>> ObserveReplace()
        {
            return collectionReplace ?? (collectionReplace = new Stream<CollectionReplaceEvent<T>>());
        }

        [NonSerialized]
        Cell<ICollection<T>> updates;
        public ICell<ICollection<T>> asCell
        {
            get
            {
                if (updates == null)
                {
                    updates = new Cell<ICollection<T>>();
                    updates.checkEquality = false;
                    updates.value = this;
                }

                return updates;
            }
        }
    }  

    public class ImmutableList<T> : IEnumerable<T>
    {
        public T[] data;

        public ImmutableList()
        {
            data = new T[0];
        }

        public ImmutableList(T single)
        {
            data = new T[] {single};
        }

        public ImmutableList(T[] data)
        {
            this.data = data;
        }

        public ImmutableList<T> Add(T value)
        {
            var newData = new T[data.Length + 1];
            Array.Copy(data, newData, data.Length);
            newData[data.Length] = value;
            return new ImmutableList<T>(newData);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.OfType<T>().GetEnumerator();
        }

        public ImmutableList<T> Remove(T value)
        {
            var i = IndexOf(value);
            if (i < 0)
                return this;
            var newData = new T[data.Length - 1];
            Array.Copy(data, 0, newData, 0, i);
            Array.Copy(data, i + 1, newData, i, data.Length - i - 1);
            return new ImmutableList<T>(newData);
        }

        public ImmutableList<T> Replace(int index, T value)
        {
            var newData = new T[data.Length];
            Array.Copy(data, newData, data.Length);
            newData[index] = value;
            return new ImmutableList<T>(newData);
        }

        public int IndexOf(T value)
        {
            for (var i = 0; i < data.Length; ++i)
                if (data[i].Equals(value))
                    return i;
            return -1;
        }

        public int Count
        {
            get { return data.Length; }
        }
    }


    public static class ReactiveCollectionExtension
    {
        class FilterCollectionRX<T> : IReactiveCollection<T>
        {
            public IReactiveCollection<T> parent;
            public Func<T, bool> predicate;

            public IDisposable Listen(
                Action<CollectionAddEvent<T>> onAdd,
                Action<CollectionRemoveEvent<T>> onRemove,
                Action<CollectionMoveEvent<T>> onMove,
                Action<CollectionReplaceEvent<T>> onReplace,
                Action onReset)
            {
                //List<T> curr = parent.ToList();

                //return parent.Listen(addEvent =>
                //{
                //    if (predicate(addEvent.Value))
                //    {

                //    }
                //});
                return null;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return parent.Where(predicate).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count { get { return parent.Count(predicate); } }
            public T ElementAt(int index)
            {
                return parent.Where(predicate).ElementAt(index);
            }
        }

        class FilterCollection<T> : Collection<T>, IEnumerable
        {
            public IReactiveCollection<T> parent;
            public Func<T, bool> predicate;

            public IDisposable Listen(
                Action<CollectionAddEvent<T>> onAdd,
                Action<CollectionRemoveEvent<T>> onRemove,
                Action<CollectionMoveEvent<T>> onMove,
                Action<CollectionReplaceEvent<T>> onReplace,
                Action onReset)
            {
                //List<T> curr = parent.ToList();

                //return parent.Listen(addEvent =>
                //{
                //    if (predicate(addEvent.Value))
                //    {

                //    }
                //});
                return null;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return parent.Where(predicate).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count { get { return parent.Count(predicate); } }
            public T ElementAt(int index)
            {
                return parent.Where(predicate).ElementAt(index);
            }
        }
        
        public static ICell<int> CountCell<T>(this IObservableCollection<T> coll)
        {
            return coll.ObserveCountChanged().Hold(coll.Count);
        }
    }
}