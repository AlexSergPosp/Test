using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using StreamUtils;
using CellUtils;
using UnityEngine;
using Tuple = UniRx.Tuple;

public interface ICell
{
    IDisposable OnChanged(Action action, Priority p = Priority.Normal);
    object valueObject { get; }
}

public interface ICell<T> : ICell
{
    IDisposable Bind(Action<T> action, Priority p = Priority.Normal);
    IDisposable ListenUpdates(Action<T> reaction, Priority p = Priority.Normal);
    T value { get; }
}

//public interface ICellSink<out T> : ICell<T>
//{
//    new T value { get; set; }
//    void SetInputStream(IStream<T> stream);
//    void SetInputCell(ICell<T> stream);
//}

[Serializable]
public class Cell<T> : ICell<T>, ITransactionable
{
    public Cell() { }
    public Cell(T initial) { Value = initial; }
    
    public void SetInputCell(ICell<T> cell, Priority p)
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = cell.Bind(Set, p);
    }

    public void SetInputCell(ICell<T> cell)
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = cell.Bind(Set);
    }


    [NonSerialized]
    protected Stream<T> update;

    [NonSerialized]
    IDisposable inputStreamConnection;

    T Value = default(T);

    [NonSerialized]
    public bool checkEquality = true;

    public virtual T value
    {
        get
        {
            if (Transaction.calculationMode)
            {
                Transaction.AddTouchedCell(this);
            }
            if (holdedValueIsCurrent) return holdedValue;
            return Value;
        }
        set
        {
            if (Transaction.calculationMode)
            {
                UnityEngine.Debug.Log("Your should not make changes in Transaction.Calculate function! Retard!");
            }

            if (inputStreamConnection != null)
            {
                inputStreamConnection.Dispose();
                inputStreamConnection = null;
            }
            Set(value);
        }
    }

    [NonSerialized]
    bool holdedValueIsCurrent;
    [NonSerialized]
    T holdedValue;

    void Set(T val)
    {
        if (Transaction.hold || Transaction.performing)
        {
            if (checkEquality && holdedValueIsCurrent && object.Equals(holdedValue, val)) return;
            if (checkEquality && !holdedValueIsCurrent && object.Equals(Value, val)) return;

            holdedValue = val;
            holdedValueIsCurrent = true;
            Transaction.RegisterPackedReaction(this);

            if (Transaction.performing && update != null)
            {
                for (int i = 0; i <= Transaction.currentPriorityUnpack; i++)
                    update.SendInTransaction(val, i);
            }
        }
        else
        {
            if (checkEquality && object.Equals(Value, val)) return;
            Value = val;
            if (update != null)
                update.Send(Value);
        }
    }

    public IDisposable OnChanged(Action action, Priority p)
    {
        if (update == null) update = new Stream<T>();
        return this.update.Listen((T _) => action(), p);
    }

    //[IgnoreDataMember]
    public object valueObject { get { return value; } }

    public IDisposable ListenUpdates(Action<T> reaction, Priority p = Priority.Normal)
    {
        if (update == null) update = new Stream<T>();
        return update.Listen(reaction, p);
    }

   // [IgnoreDataMember]
    public IStream<T> updates
    {
        get { return update ?? (update = new Stream<T>()); }
    }

    public IDisposable Bind(Action<T> action, Priority p = Priority.Normal)
    {
        action(Value);
        if (update == null) update = new Stream<T>();
        return update.Listen(action, p);
    }

    public void SetInputStream(IStream<T> stream)
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = stream.Listen(Set);
    }

    public void ResetInputStream()
    {
        if (inputStreamConnection == null) return;
        inputStreamConnection.Dispose();
        inputStreamConnection = null;
    }

    // Do not call
    public void TransactionIterationFinished()
    {
        Value = holdedValue;
        holdedValueIsCurrent = false;
        holdedValue = default(T);
    }

    // Do not call
    public void Unpack(int p)
    {
        if (update == null) return;

        if (!object.Equals(holdedValue, Value))
        {
            update.SendInTransaction(holdedValue, p);
        }
    }

    public override string ToString()
    {
        return value.ToString();
    }
}

public class Cell<T1,T2> : Cell<UniRx.Tuple<T1, T2>>
{
    public Cell(T1 t1, T2 t2) : base(Tuple.Create(t1, t2))
    {
    }
    public Cell()
    {
    }

    public T1 value1 { get { return value.Item1; } set { this.value = Tuple.Create(value, this.value.Item2); } }
    public T2 value2 { get { return value.Item2; } set { this.value = Tuple.Create(this.value.Item1,value); } }

    public IDisposable Bind(Action<T1, T2> action)
    {
        return Bind((obj) => action(obj.Item1, obj.Item2));
    }
}

[Serializable]
public class StaticCell<T> : ICell<T>
{
    public StaticCell() { }
    public StaticCell(T initial) { Value = initial; }

    T Value = default(T);

    public virtual T value
    {
        get
        {
            return Value;
        }
    }

    public IDisposable ListenUpdates(Action<T> reaction, Priority p = Priority.Normal)
    {
        return new EmptyDisposable();
    }

    public IDisposable OnChanged(Action action, Priority p = Priority.Normal)
    {
        return new EmptyDisposable();
    }

    public object valueObject { get { return value; } }

    public IDisposable Bind(Action<T> action, Priority p = Priority.Normal)
    {
        action(Value);
        return new EmptyDisposable();
    }
}


public class AnonymousCell<T> : ICell<T>, IStream<T>, UniRx.IObservable<T>
{
    public Func<Action<T>, Priority, IDisposable> listen;
    public Func<T> current;
    //public T last;

    public T value { get { return current(); } }

    public AnonymousCell()
    {

    }

    public AnonymousCell(Func<Action<T>, Priority, IDisposable> subscribe, Func<T> current)
    {
        this.listen = subscribe;
        this.current = current;
    }

    public IDisposable ListenUpdates(Action<T> reaction, Priority p)
    {
        return listen(reaction, p);
    }

    public IDisposable Bind(Action<T> reaction, Priority p)
    {
        reaction(current());
        return ListenUpdates(reaction, p);
    }

    public IDisposable Subscribe(UniRx.IObserver<T> observer)
    {
        return ListenUpdates(observer.OnNext, Priority.Normal);
    }

    public IDisposable Listen(Action<T> reaction, Priority priority = Priority.Normal)
    {
        return Bind(reaction, priority);
    }

    public IDisposable Listen(Action action, Priority priority = Priority.Normal)
    {
        return Bind(_ => action(), priority); ;
    }

    public IDisposable OnChanged(Action action, Priority p = Priority.Normal)
    {
        return listen(_ => action(), p);
    }

    public object valueObject { get { return value; } }
}

public static class CellReactiveApi
{

    public static IDisposable LateBind<T>(this ICell<T> cell, Action<T> action)
    {
        return cell.Bind(action, Priority.Post);
    }
    public static IDisposable LateListenUpdates<T>(this ICell<T> cell, Action<T> action)
    {
        return cell.ListenUpdates(action, Priority.Post);
    }

    public static ICell<T> Accumulate<T, T2>(this IStream<T2> stream, T initial, Func<T, T2, T> accomulator)
    {
        var disp = new MapDisposable { last = initial };
        return new AnonymousCell<T>((Action<T> reaction, Priority p) =>
        {
            disp.Disposable = stream.Listen(val =>
            {
                var newVal = accomulator((T)disp.last, val);
                if (object.Equals(newVal, disp.last)) return;
                disp.last = newVal;
                reaction(newVal);
            }, p);
            return disp;
        }, () => (T)disp.last);
    }

    [Serializable]
    public class Carrier<T>
    {
        public T val = default(T);
    }

    // First argument will be previous value
    public static IDisposable BindHistoric<T>(this ICell<T> cell, Action<T, T> reaction, Priority p = Priority.Normal)
    {
        var disp = new Carrier<T> {val = cell.value};

        return cell.Bind(val =>
        {
            reaction(disp.val, val);
            disp.val = val;
        }, p);
    }

    public static IDisposable WhateverBind<T>(this ICell<T> cell, Action<T, ConnectionCollector> action, Priority p = Priority.Normal)
    {
        var collector = new ConnectionCollector();
        var disps = new ListDisposable
        {
            collector,
            cell.Bind(val =>
            {
                collector.DisconnectAll();
                action(val, collector);
            }, p)
        };
        return disps;
    }

    public static ICell<T> Hold<T>(this IStream<T> stream, T initial)
    {
        var disp = new MapDisposable { last = initial };
        return new AnonymousCell<T>((Action<T> reaction, Priority p) =>
        {
            disp.Disposable = stream.Listen(val =>
            {
                if (!object.Equals(val, disp.last))
                {
                    disp.last = val;
                    reaction(val);
                }
            }, p);
            return disp;
        }, () => (T)disp.last);
    }

    public static ICell<T> FaceControl<T>(this ICell<T> cell, Func<T, bool> predicate, T orphan)
    {
        return cell.Map(val => predicate(val) ? val : orphan);
    }

    public static ICell<T> NotNull<T>(this ICell<T> cell, T orphan) where T : class
    {
        return cell.Map(val => val ?? orphan);
    }

    public static IStream<T> EachNotNull<T>(this ICell<T> cell) where T : class
    {
        return new AnonymousStream<T>((act, p) =>
        {
            return cell.Bind(val =>
            {
                if (val != null) act(val);
            }, p);
        });
    }

    public static IStream<T> AsStream<T>(this ICell<T> cell)
    {
        return new AnonymousCell<T>(cell.ListenUpdates, () => cell.value);
    }

    public static IStream<T> UpdateStream<T>(this ICell<T> cell)
    {
        return new AnonymousStream<T>(cell.ListenUpdates);
    }

    public static UniRx.IObservable<T> AsObservable<T>(this ICell<T> cell)
    {
        return new AnonymousCell<T>(cell.ListenUpdates, () => cell.value);
    }


    // Return special stream that guarantie to call listen function once filter is returned true
    // So if filter return true on initial cell value listen function will be called right now.
    public static IOnceEmptyStream WhenOnce<T>(this ICell<T> cell, Func<T, bool> filter)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            if (filter(cell.value))
            {
                reaction();
                return new EmptyDisposable();
            }
            else
            {
                var disp = new SingleAssignmentDisposable();
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    if (!filter(val)) return;
                    reaction();
                    disp.Dispose();
                }, p);
                return disp;
            }
        });
    }

    public static IOnceStream<T> When<T>(this ICell<T> cell, Func<T, bool> filter)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            return cell.Bind(val =>
            {
                if (!filter(val)) return;
                reaction(val);
            }, p);
        });
    }

    public static IEmptyStream When(this ICell<bool> cell)
    {
        return cell.When(i => i);
    }

    class DisposableContainer<T> : IDisposable
    {
        public T value;
        public IDisposable disp;
        public void Dispose()
        {
            if (disp == null) return;
            disp.Dispose();
            disp = null;
        }
    }

    public static IStream<T> Where<T>(this ICell<T> cell, Func<T, bool> filter)
    {
        return new AnonymousStream<T>((reaction, priority) =>
        {
            DisposableContainer<bool> disp = new DisposableContainer<bool>();
            disp.disp = cell.Bind(val =>
            {
                var ok = filter(val);
                if (!disp.value && ok) reaction(val);
                disp.value = ok;
            }, priority);
            return disp;
        });
    }

    public static IEmptyStream WhenSatisfy<T>(this ICell<T> cell, Func<T, bool> filter)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            DisposableContainer<bool> disp = new DisposableContainer<bool>();
            disp.disp = cell.Bind(val =>
            {
                var ok = filter(val);
                if (!disp.value && ok) reaction();
                disp.value = ok;
            }, p);
            return disp;
        });
    }

    class MapDisposable : SingleAssignmentDisposable
    {
        public object last;
    }

    public static ICell<T2> Map<T, T2>(this ICell<T> cell, Func<T, T2> map)
    {
        return new AnonymousCell<T2>((Action<T2> reaction, Priority p) =>
        {
            var disp = new MapDisposable();
            disp.last = map(cell.value);
            disp.Disposable = cell.ListenUpdates(val =>
            {
                var newCurr = map(val);
                if (!object.Equals(newCurr, disp.last))
                {
                    disp.last = newCurr;
                    reaction(newCurr);
                }
            }, p);
            return disp;
        }, () => map(cell.value));
    }

    public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this Cell<T>[] cells)
    {
        return cells.Select(val => (ICell<T>)val).ToCellOfCollection();
    }

    public static ICell<IEnumerable<T>> ToCellOfCollection<T2,T>(this IEnumerable<T2> cells,Func<T2,ICell<T>> predicate )
    {
        return cells.Select(predicate).ToCellOfCollection();
    }

    public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<Cell<T>> cells)
    {
        return cells.Select(val => (ICell<T>) val).ToCellOfCollection();
    }

    public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<ICell<T>> cells)
    {
        Func<IEnumerable<T>> values = () => cells.Select(cell => cell.value);
        return new AnonymousCell<IEnumerable<T>>((Action<IEnumerable<T>> reaction, Priority p) =>
        {
            CellJoinDisposable<T> group = new CellJoinDisposable<T> { };
            foreach (var cell in cells)
            {
                group.Add(cell.ListenUpdates(_ => reaction(values()), p));
            }
            return group;
        }, values);
    }

    public static ICell<T3> Merge<T, T2, T3>(this ICell<T> cell, ICell<T2> cell2, Func<T, T2, T3> func)
    {
        Func<T3> curr = () => func(cell.value, cell2.value);
        return new AnonymousCell<T3>((Action<T3> reaction, Priority p) =>
        {
            var disp = new CellJoinDisposable<T3>();
            disp.lastValue = func(cell.value, cell2.value);
            disp.Add(cell.ListenUpdates(val =>
            {
                T3 newCurr = curr();
                if (!object.Equals(newCurr, disp.lastValue))
                {
                    disp.lastValue = newCurr;
                    reaction(newCurr);
                }
            }, p));
            disp.Add(cell2.ListenUpdates(val =>
            {
                T3 newCurr = curr();
                if (!object.Equals(newCurr, disp.lastValue))
                {
                    disp.lastValue = newCurr;
                    reaction(newCurr);
                }
            }, p));
            return disp;
        }, curr);
    }

    public static Cell<T> Materialize<T>(this ICell<T> cell)
    {
        var c = new Cell<T>();
        c.SetInputCell(cell);
        return c;
    }

    public static ICell<T> Join<T>(this ICell<ICell<T>> cell)
    {
        return new AnonymousCell<T>((Action<T> reaction, Priority p) =>
        {

            SingleAssignmentDisposable mainDisposable = new SingleAssignmentDisposable();
            SingleAssignmentDisposable inner = new SingleAssignmentDisposable();
            CellJoinDisposable<T> group = new CellJoinDisposable<T> { mainDisposable, inner };
            group.lastValue = cell.value.value;

            Action<ICell<T>> func = (ICell<T> innerCell) =>
            {
                if (!inner.IsDisposed)
                {
                    T value = innerCell.value;
                    if (!object.Equals(group.lastValue, value))
                    {
                        reaction(value);
                        group.lastValue = value;
                    }
                    inner.Dispose();
                }

                inner.Disposable = innerCell.ListenUpdates(val =>
                {
                    reaction(val);
                    group.lastValue = val;
                }, p);
            };

            func(cell.value);

            mainDisposable.Disposable = cell.ListenUpdates(func, p);
            return group;
        }, () => cell.value.value);
    }

    static public IStream<T> Join<T>(this ICell<IStream<T>> cell)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            SingleAssignmentDisposable mainDisposable = new SingleAssignmentDisposable();
            SingleAssignmentDisposable inner = new SingleAssignmentDisposable();
            CellJoinDisposable<T> group = new CellJoinDisposable<T> { mainDisposable, inner };

            Action<IStream<T>> func = (IStream<T> innerStream) =>
            {
                inner.Dispose();
                if (innerStream != null)
                    inner.Disposable = innerStream.Listen(reaction, p);
            };

            mainDisposable.Disposable = cell.Bind(func, p);
            return group;
        });
    }

    public static ICell<T> Join<T>(this ICell<Cell<T>> cell)
    {
        return Join(cell.Map(c => c as ICell<T>));
    }

    public static IStream<T> Join<T>(this ICell<Stream<T>> cell)
    {
        return Join(cell.Map(val => val as IStream<T>));
    }

    public static ICell<T2> Select<T, T2>(this ICell<T> cell, Func<T, T2> selector)
    {
        return Map(cell, selector);
    }

    public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, ICell<TR> other)
    {
        return SelectMany(source, _ => other);
    }

    public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, Func<T, ICell<TR>> selector)
    {
        return Map(source, selector).Join();
    }

    public static ICell<TR> SelectMany<T, TC, TR>(this ICell<T> source, Func<T, ICell<TC>> collectionSelector, Func<T, TC, TR> resultSelector)
    {
        return source.SelectMany(x => collectionSelector(x).Select(y => resultSelector(x, y)));
    }

    public static ICell<bool> Not(this ICell<bool> value)
    {
        return value.Select(val => !val);
    }

    public static ICell<object> AsObject<T>(this ICell<T> cell)
    {
        return cell.Select(val => val as object);
    }
}
