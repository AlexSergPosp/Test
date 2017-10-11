using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellUtils;
using UnityEngine;

public interface ITransactionable
{
    void TransactionIterationFinished();
    void Unpack(int priority);
}

class Transaction
{
    public const int priorityCount = 3;
    static Transaction()
    {
        actionQueue = new List<List<Action>>();
        int lenght = Enum.GetValues(typeof(Priority)).Length;
        for (int i = 0; i < lenght; ++i)
        {
            actionQueue.Add(new List<Action>());
        }
    }
    static List<List<Action>> actionQueue;
    static List<ITransactionable> registered = new List<ITransactionable>();
    public static bool hold;

    static bool actionQueueDirty;
    public static void AddDataAction(int priority, Action act)
    {
        actionQueueDirty = true;
        actionQueue[priority].Add(act);
    }
    public static void RegisterPackedReaction(ITransactionable obj)
    {
        // MAKE IT FASTER!
        if (!registered.Contains(obj))
            registered.Add(obj);
    }

    public static bool performing = false;
    public static int currentPriorityUnpack;

    public static void Perform(Action action)
    {
        hold = true;
        action();
        hold = false;

        performing = true;

        for (currentPriorityUnpack = 0; currentPriorityUnpack < priorityCount; currentPriorityUnpack++)
        {
            foreach (var reactor in registered)
            {
                reactor.Unpack(currentPriorityUnpack);
            }
            bool complete = false;
            while (!complete)
            {
                actionQueueDirty = false;
                foreach (var list in actionQueue)
                {
                    while (list.Count != 0)
                    {
                        list.Take(0)();
                        
                        if (actionQueueDirty) break;
                    }
                    if (actionQueueDirty) break;
                }
                if (!actionQueueDirty) complete = true;
            }
        }
        foreach (var item in registered)
        {
            item.TransactionIterationFinished();
        }
        registered.Clear();
        performing = false;
    }


    /*  Pass plain formula and recieve fully subscribed reactive cell

        Example:
        var relativeHp = Calculate(() = > {
            if (monster.value == null) return 0;
            return monster.value.hp.value / monster.maxHealth.value;
        });

        Restrictions:
        1 Do not do bind, listen, connections dispose or other operations. Write pure calculation code.
        2 Do not change state. Your formula should not mutate world. Only constant access.
        3 Do not use reactive values in code branches or it will not be subscribed properly. (May be fixed later)
    */
    public static ICell<T> Calculate<T>(Func<T> formula)
    {
        calculationMode = true;
        var probe = formula();
        calculationMode = false;

        var copyTouched = touched;
        touched = new List<ICell>();

        return new AnonymousCell<T>((Action<T> reaction, Priority p) => {
            CellJoinDisposable<T> group = new CellJoinDisposable<T>();
            group.SetArray(copyTouched.Select(cell => cell.OnChanged(() =>
            {
                var val = formula();
                if (!object.Equals(group.lastValue, val))
                {
                    reaction(val);
                    group.lastValue = val;
                }
            }, p)));
            group.lastValue = formula();
            return group;
        }, () => formula());   
    }

    static public void AddTouchedCell(ICell cell)
    {
        // MAKE IT FASTER!
        if (!touched.Contains(cell))
            touched.Add(cell);
    }
    static List<ICell> touched = new List<ICell>();
    public static bool calculationMode;

    
    // Tests.
    public static void Test()
    {
        Debug.Log("Test 1");

        Cell<int> c1 = new Cell<int>(1);
        Cell<int> c2 = new Cell<int>(2);

        c1.ListenUpdates(val =>
        {
            Debug.Log("cell low 1_1 update: " + val.ToString());
        }, Priority.Post);
        var dispLow2 = c1.ListenUpdates(val => Debug.Log("cell low 1_2 update: " + val.ToString()), Priority.Post);
        var dispMid1 = c1.ListenUpdates(val =>
        {
            Debug.Log("cell 1_1 update: " + val.ToString());
        });
        c1.ListenUpdates(val =>
        {
            c1.value = 4;
            dispLow2.Dispose();
            dispMid1.Dispose();
            Debug.Log("cell 1_2 update: " + val.ToString());
        });
        c1.ListenUpdates(val => Debug.Log("cell high 1_1 update: " + val.ToString()), Priority.Pre);
        c1.ListenUpdates(val => Debug.Log("cell high 1_2 update: " + val.ToString()), Priority.Pre);
        var disp1 = c2.ListenUpdates(val => Debug.Log("cell 2 update: " + val.ToString()));

        Debug.Log("Transaction start");

        var cJoin = from v1 in c1
                    from v2 in c2
                    select v1 + v2;

        cJoin.ListenUpdates(val =>
        {
            Debug.Log("cJoin update: " + val.ToString());
        }, Priority.Post);

        Transaction.Perform(() =>
        {
            c1.value = 2;
            c1.value = 3;
            c2.value = c1.value;

            disp1.Dispose();
            dispLow2.Dispose();

            Debug.Log("Transaction finish");
        });

        Debug.Log("Test 1 finish");
    }

    public static void TestCalculation()
    {
        Cell<int> count = new Cell<int>(50 );
        List<Cell<float>> cells = new List<Cell<float>>();

        for (int i = 0; i < 50; ++i)
        {
            cells.Add(new Cell<float>(i));
        }

        ICell<string> result = Transaction.Calculate(() =>
        {
            string message = "count: " + count.ToString() + "list: ";
            foreach(var item in cells)
            {
                message += item.value.ToString() + " ";
            }
            return message;
        });

        result.Bind(message => Debug.Log(message));

        for (int i = 0; i < count.value; ++i)
        {
            cells[UnityEngine.Random.Range(0, count.value)].value = 0;
        }
        count.value = 20;

        Debug.Log("test 2 finished");
    }
}
