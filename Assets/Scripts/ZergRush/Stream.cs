using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CellUtils;
using ZergRush;
using SingleAssignmentDisposable = CellUtils.SingleAssignmentDisposable;

public enum Priority
{
    Pre,
    Normal,
    Post
}

public interface IStream<T> : IEmptyStream
{
    IDisposable Listen(Action<T> action, Priority priority = Priority.Normal);
}

public interface IEmptyStream
{
    IDisposable Listen(Action action, Priority p = Priority.Normal);
}

// Once streams guarantee to execute connnections only once and then disconnects all automaticaly
public interface IOnceEmptyStream : IEmptyStream
{

}

public interface IOnceStream<T> : IStream<T>
{

}

[Serializable]
public class Stream<T1, T2> : Stream<UniRx.Tuple<T1, T2>>
{
    public void Send(T1 t1, T2 t2)
    {
        Send(UniRx.Tuple.Create(t1,t2));
    }

    public IDisposable Listen(Action<T1, T2> action)
    {
        return Listen(obj => action(obj.Item1, obj.Item2));
    }
}
[Serializable]
public class Stream<T1, T2, T3> : Stream<UniRx.Tuple<T1, T2, T3>>
{
    public void Send(T1 t1, T2 t2,T3 t3)
    {
        Send(UniRx.Tuple.Create(t1, t2, t3));
    }

    public IDisposable Listen(Action<T1, T2, T3> action)
    {
        return Listen(obj => action(obj.Item1, obj.Item2, obj.Item3));
    }
}
[Serializable]
public class Stream<T1, T2, T3, T4> : Stream<UniRx.Tuple<T1, T2, T3, T4>>
{
    public void Send(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        Send(UniRx.Tuple.Create(t1, t2, t3, t4));
    }

    public IDisposable Listen(Action<T1, T2, T3, T4> action)
    {
        return Listen(obj => action(obj.Item1, obj.Item2, obj.Item3, obj.Item4));
    }
}

[Serializable]
public class Stream<T> : IStream<T>, IDisposable, ITransactionable
{
    [NonSerialized]
    Reactor<T> observer = new Reactor<T>();
    [NonSerialized]
    protected IDisposable inputStreamConnection;
    [NonSerialized]
    List<T> holdedValues;

    // If true this stream will send value only once after transaction even if it was fired multiple times.
    //public bool sendOnceInTransaction = false;
    protected bool autoDisconnectAfterEvent = false;

    [OnDeserializing]
    void OnDeserializing(StreamingContext c)
    {
        observer = new Reactor<T>();
    }

    public void Send(T value)
    {
        if (inputStreamConnection != null)
        {
            inputStreamConnection.Dispose();
            inputStreamConnection = null;
        }

        if (Transaction.calculationMode)
        {
            //Logger.Say("Your should not send values in streams in Transaction.Calculate function! Retard!");
        }

        if (Transaction.performing)
        {
            for (int i = 0; i < Transaction.priorityCount; i++)
                SendInTransaction(value, i);
            return;
        }

        if (Transaction.hold)
        {
            if (holdedValues == null)
                holdedValues = new List<T>();
            holdedValues.Add(value);
            Transaction.RegisterPackedReaction(this);
            return;
        }

        observer.React(value);

        if (autoDisconnectAfterEvent) observer.Clear();
    }

    public void SendInTransaction(T value, int p)
    {
        observer.TransactionUnpackReact(value, p);
    }

    public IDisposable Listen(Action<T> action, Priority priority = Priority.Normal)
    {
        return observer.AddReaction(action, priority);
    }
    public IDisposable Listen(Action action, Priority p = Priority.Normal)
    {
        return observer.AddReaction(_ => action(), p);
    }

    public void Dispose()
    {
        observer = null;
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = null;
    }

    public void SetInputStream(IStream<T> stream)
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = stream.Listen(val => observer.React(val));
    }

    public void ClearInputStream()
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = null;
    }

    public void TransactionIterationFinished()
    {
        holdedValues = null;
    }

    public void Unpack(int p)
    {
        foreach (var val in holdedValues)
        {
            observer.TransactionUnpackReact(val, p);
        }
    }
}

public class OnceStream<T> : Stream<T>, IOnceStream<T>
{
    public OnceStream()
    {
        autoDisconnectAfterEvent = true;
    }
}


public class AbandonedStream : IEmptyStream
{
    public IDisposable Listen(Action action, Priority p = Priority.Normal)
    {
        return new EmptyDisposable();
    }
}

public class AbandonedStream<T> : IStream<T>
{
    public IDisposable Listen(Action<T> action, Priority p = Priority.Normal)
    {
        return new EmptyDisposable();
    }
    public IDisposable Listen(Action action, Priority priority = Priority.Normal)
    {
        return new EmptyDisposable();
    }
}

public class WaitForStreamEvent
{
    protected IDisposable connection;
    public WaitForStreamEvent(IEmptyStream stream)
    {
        connection = stream.FirstOnly().Listen(() =>
        {
            finished_ = true;
        });
    }
    protected bool finished_;
    public bool finished
    {
        get
        {
            return finished_;
        }
    }
    public void Tick(double dt) { }
    public void Dispose() { connection.Dispose(); }
}

[Serializable]
public class EmptyStream : Stream<UniRx.Unit>
{
    public void Send()
    {
        Send(UniRx.Unit.Default);
    }
    public void SetInputStream(IEmptyStream stream)
    {
        if (inputStreamConnection != null) inputStreamConnection.Dispose();
        inputStreamConnection = stream.Listen(() => Send());
    }
}

[Serializable]
public class OnceEmptyStream : EmptyStream, IOnceEmptyStream
{
    public OnceEmptyStream()
    {
        autoDisconnectAfterEvent = true;
    }
}

class AnonymousEmptyStream : IOnceEmptyStream
{
    readonly Func<Action, Priority, IDisposable> listen;

    public AnonymousEmptyStream(Func<Action, Priority, IDisposable> subscribe)
    {
        this.listen = subscribe;
    }

    public IDisposable Listen(Action observer, Priority p)
    {
        return listen(observer, p);
    }
}

class AnonymousStream<T> : IOnceStream<T>
{
    readonly Func<Action<T>, Priority, IDisposable> listen;

    public AnonymousStream(Func<Action<T>, Priority, IDisposable> subscribe)
    {
        this.listen = subscribe;
    }

    public IDisposable Listen(Action<T> observer, Priority p)
    {
        return listen(observer, p);
    }

    public IDisposable Listen(Action observer, Priority p)
    {
        return listen(_ => observer(), p);
    }
}


public static class StreamAPI
{
    static public IDisposable LateListen<T>(this IStream<T> stream, Action<T> action)
    {
        return stream.Listen(action, Priority.Post);
    }

    public static IDisposable ListenQueue<T>(this IStream<T> stream, ICollection<Action<T>> collection)
    {
        return stream.Listen(obj =>
        {
            if (collection.Count == 0)
                return;

            collection.Last()(obj);
        });
    }

    public static IDisposable LateListen(this IEmptyStream stream, Action action)
    {
        return stream.Listen(action, Priority.Post);
    }

    public static IEmptyStream Where(this IEmptyStream stream, Func<bool> o)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            return stream.Listen(() =>
            {
                if (o()) reaction();
            }, p);
        });
    }


    public static IStream<T> Only<T2, T>(this IStream<T2> stream) where T : class where T2 : class
    {
        return new AnonymousStream<T>((reaction, priority) => 
        {
            return stream.Listen(obj=>
            {
                var o = obj as T;
                if (o != null )
                    reaction(o);
            }, priority);
        });
    }
    public static IOnceStream<T> Once<T>(this IStream<T> stream)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            SingleAssignmentDisposable disp = new SingleAssignmentDisposable();
            disp.Disposable = stream.Listen(val =>
            {
                reaction(val);
                disp.Dispose();
            }, p);
            return disp;
        });
    }

    public static IOnceStream<T> Once<T>(this Stream<T> stream)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            SingleAssignmentDisposable disp = new SingleAssignmentDisposable();
            disp.Disposable = stream.Listen(val =>
            {
                reaction(val);
                disp.Dispose();
            }, p);
            return disp;
        });
    }

    public static IOnceEmptyStream Once(this EmptyStream stream)
    {
        return OnceEmptyStream(stream);
    }
    public static IOnceEmptyStream Once(this IEmptyStream stream)
    {
        return OnceEmptyStream(stream);
    }

    private static IOnceEmptyStream OnceEmptyStream(IEmptyStream stream)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            SingleAssignmentDisposable disp = new SingleAssignmentDisposable();
            disp.Disposable = stream.Listen(() =>
            {
                reaction();
                disp.Dispose();
            }, p);
            return disp;
        });
    }

    public static IStream<T2> Map<T, T2>(this IStream<T> stream, Func<T, T2> map)
    {
        return new AnonymousStream<T2>((Action<T2> reaction, Priority p) =>
        {
            return stream.Listen(val => reaction(map(val)), p);
        });
    }

    public static IEmptyStream ToEmpty<T>(this IStream<T> stream)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            return stream.Listen(_ => reaction(), p);
        });
    }

    public static IStream<T> Filter<T>(this IStream<T> stream, Func<T, bool> filter)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            return stream.Listen(val =>
            {
                if (filter(val)) reaction(val);
            }, p);
        });
    }
    public static IStream<T> FirstOnly<T>(this IStream<T> stream)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            SingleAssignmentDisposable disp = new SingleAssignmentDisposable();
            disp.Disposable = stream.Listen(val =>
            {
                reaction(val);
                disp.Dispose();
            }, p);
            return disp;
        });
    }

    public static IEmptyStream MergeWith(this IEmptyStream stream, params IEmptyStream[] others)
    {
        return new AnonymousEmptyStream((reaction, p) =>
        {
            ListDisposable disp = new ListDisposable();
            disp.Add(stream.Listen(reaction, p));

            foreach (var other in others)
            {
                disp.Add(other.Listen(reaction, p));
            }

            return disp;
        });
    }

    public static IStream<T> MergeWith<T>(this IStream<T> stream, params IStream<T>[] others)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {
            ListDisposable disp = new ListDisposable();
            disp.Add(stream.Listen(reaction, p));

            foreach (var other in others)
            {
                disp.Add(other.Listen(reaction, p));
            }

            return disp;
        });
    }

    public static IEmptyStream FirstOnly(this IEmptyStream stream)
    {
        return new AnonymousEmptyStream((Action reaction, Priority p) =>
        {
            SingleAssignmentDisposable disp = new SingleAssignmentDisposable();
            disp.Disposable = stream.Listen(() =>
            {
                reaction();
                disp.Dispose();
            }, p);
            return disp;
        });
    }
}

public static class StreamQuery
{
    public static IStream<T2> Select<T, T2>(this IStream<T> stream, Func<T, T2> map)
    {
        return stream.Map(map);
    }
    public static IStream<T> Where<T>(this IStream<T> stream, Func<T, bool> filter)
    {
        return stream.Filter(filter);
    }
    public static IStream<T> Skip<T>(this IStream<T> stream, int count)
    {
        int i = 0;
        return stream.Filter(arg1 =>
        {
            if (i >= count)
                return true;

            i++;
            return false;
        });
    }

    public static IStream<TR> SelectMany<T, TR>(this IStream<T> source, IStream<TR> other)
    {
        return SelectMany(source, _ => other);
    }

    public static IStream<TR> SelectMany<T, TR>(this IStream<T> source, Func<T, IStream<TR>> selector)
    {
        return source.Map(selector).Join();
    }

    public static IStream<TR> SelectMany<T, TC, TR>(this IStream<T> source, Func<T, IStream<TC>> collectionSelector, Func<T, TC, TR> resultSelector)
    {
        return source.SelectMany(x => collectionSelector(x).Select(y => resultSelector(x, y)));
    }

    static public IStream<T> Join<T>(this IStream<IStream<T>> stream)
    {
        return new AnonymousStream<T>((Action<T> reaction, Priority p) =>
        {

            SingleAssignmentDisposable mainDisposable = new SingleAssignmentDisposable();
            SingleAssignmentDisposable inner = new SingleAssignmentDisposable();
            ListDisposable group = new ListDisposable { mainDisposable, inner };

            mainDisposable.Disposable = stream.Listen((IStream<T> innerStream) =>
            {
                var newDisp = innerStream.Listen(val =>
                {
                    reaction(val);
                }, p);
                inner.Dispose();
                inner.Disposable = newDisp;
            }, p);

            return group;
        });
    }
}

public class Reactor<T>
{
    Action<T> single;
    Priority singlePriority;

    ImmutableList<Action<T>>[] multipriorityReactions;

    public Reactor()
    {

    }

    public void React(T t)
    {
        if (Empty()) return;

        if (single != null)
        {
            single(t);
        }
        else
        {
            foreach (var list in multipriorityReactions)
            {
                if (list == null) continue;
                foreach (var action in list)
                {
                    action(t);
                }
            }
        }
    }

    public void TransactionUnpackReact(T t, int i)
    {
        if (Empty()) return;
        if (single != null)
        {
            UpgradeToMultyPriority();
        }
        var list = multipriorityReactions[i];
        if (list == null || list.Count == 0) return;

        Action act = () =>
        {
            foreach (var action in list)
            {
                action(t);
            }
        };
        Transaction.AddDataAction(i, act);
    }


    bool Empty()
    {
        return single == null && multipriorityReactions == null;
    }

    public IDisposable AddReaction(Action<T> reaction, Priority priority)
    {
        if (Empty())
        {
            single = reaction;
            singlePriority = priority;
        }
        else
        {
            if (single != null) UpgradeToMultyPriority();
            AddToMultipriority(reaction, priority);
        }
        return new Subscription() { parent = this, priority = priority, unsubscribeTarget = reaction };
    }

    void UpgradeToMultyPriority()
    {
        if (multipriorityReactions == null)
        {
            multipriorityReactions = new ImmutableList<Action<T>>[3];
            AddToMultipriority(single, singlePriority);
            single = null;
        }
    }


    void AddToMultipriority(Action<T> reaction, Priority p)
    {
        var list = multipriorityReactions[(int)p];
        if (list == null)
        {
            list = new ImmutableList<Action<T>>(reaction);
            multipriorityReactions[(int)p] = list;
        }
        else
        {
            multipriorityReactions[(int)p] = list.Add(reaction);
        }
    }

    void RemoveReaction(Action<T> reaction, Priority p)
    {
        if (single != null)
        {
            if (!object.ReferenceEquals(single, reaction) || p != singlePriority)
            {
                return;
            }
            single = null;
        }
        else if (multipriorityReactions != null)
        {
            multipriorityReactions[(int)p] = multipriorityReactions[(int)p].Remove(reaction);
        }
    }

    class Subscription : IDisposable
    {
        public Reactor<T> parent;
        public Action<T> unsubscribeTarget;
        public Priority priority;

        public void Dispose()
        {
            if (parent == null) return;
            parent.RemoveReaction(unsubscribeTarget, priority);
            unsubscribeTarget = null;
            parent = null;
        }
    }

    public void Clear()
    {
        single = null;
        multipriorityReactions = null;
    }
}

