using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionCollector : List<IDisposable>, IDisposable, IConnectionCollector
{

    public IDisposable add
    {
        set { Add(value); }
    }

    public void DisconnectAll()
    {
        foreach (var c in this)
        {
            c.Dispose();
        }

        Clear();
    }

    public void RemoveAndDispose(IDisposable disp)
    {
        var removed = Remove(disp);
        if (!removed)
        {
           
        }
        disp.Dispose();
    }

    public void Dispose()
    {
        DisconnectAll();
    }

    public void Collect(IDisposable connection)
    {
        add = connection;
    }
}

public interface IConnectionCollector
{
    void Collect(IDisposable connection);
}
