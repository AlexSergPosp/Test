using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CellUtils
{
    public class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
    public class SingleAssignmentDisposable : IDisposable
    {
        IDisposable current;

        public bool IsDisposed { get { return current == null; } }

        public IDisposable Disposable
        {
            get
            {
                return current;
            }
            set
            {
                if (current != null)
                {
                    throw new InvalidOperationException("Disposable is already set");
                }
                current = value;
            }
        }

        public void Dispose()
        {
            if (current != null) current.Dispose();
            current = null;
        }
    }

    public class ListDisposable : ICollection<IDisposable>, IDisposable
    {
        private bool _disposed;
        private List<IDisposable> _disposables;

        public ListDisposable()
        {
            _disposables = new List<IDisposable>();
        }

        public ListDisposable(int capacity)
        {
            _disposables = new List<IDisposable>(capacity);
        }

        public ListDisposable(params IDisposable[] disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");

            _disposables = new List<IDisposable>(disposables);
        }

        public ListDisposable(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");

            _disposables = new List<IDisposable>(disposables);
        }

        public void SetArray(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");

            _disposables = disposables.ToList();
        }

        /// <summary>
        /// Gets the number of disposables contained in the CompositeDisposable.
        /// </summary>
        public int Count
        {
            get
            {
                return _disposables.Count;
            }
        }

        public void Add(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (!_disposed)
            {
                _disposables.Add(item);
            }
            else
            {
                item.Dispose();
            }
        }

        public bool Remove(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (!_disposed)
            {
                return _disposables.Remove(item);
            }
            return false;
        }

        /// <summary>
        /// Disposes all disposables in the group and removes them from the group.
        /// </summary>
        public void Dispose()
        {
            foreach (var d in _disposables)
                d.Dispose();
            _disposables.Clear();
            _disposed = true;
        }

        public void Clear()
        {
            foreach (var d in _disposables)
               d.Dispose();
            _disposables.Clear();
        }

        public bool Contains(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            return _disposables.Contains(item);
        }

        /// <summary>
        /// Copies the disposables contained in the CompositeDisposable to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">Array to copy the contained disposables to.</param>
        /// <param name="arrayIndex">Target index at which to copy the first disposable of the group.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than zero. -or - <paramref name="arrayIndex"/> is larger than or equal to the array length.</exception>
        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            var disArray = new List<IDisposable>();
            foreach (var item in disArray)
            {
                if (item != null) disArray.Add(item);
            }

            Array.Copy(disArray.ToArray(), 0, array, arrayIndex, array.Length - arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<IDisposable> GetEnumerator()
        {
            return _disposables as IEnumerator<IDisposable>;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the CompositeDisposable.
        /// </summary>
        /// <returns>An enumerator to iterate over the disposables.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed; }
        }
    }

    public class CellJoinDisposable<T> : ListDisposable
    {
        public object lastValue;
    }
}

