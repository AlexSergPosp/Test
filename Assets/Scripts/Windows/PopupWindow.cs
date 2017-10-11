using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupWindow : MonoBehaviour
{

    public PopupType type;

    public EmptyStream closed = new EmptyStream();
    public BoolCell currentlyOpen = new BoolCell(false);

 public ConnectionCollector connectionCollector = new ConnectionCollector();

    public virtual void Show(bool state, object data)
    {
        throw new NotImplementedException();
    }

    public void Hide()
    {
        Show(false, null);
    }
}

public class PopupWindow<T> : PopupWindow where T : class
{
    public virtual void Show(bool state, T data)
    {
        if (state == currentlyOpen.value)
            return;

        if (data != null)
            Fill(data);

        currentlyOpen.value = state;
        gameObject.SetActive(state);

        if (state == false)
        {
            connectionCollector.DisconnectAll();
            closed.Send();
        }
    }

    public override void Show(bool state, object data)
    {
        Show(state, data as T);
    }

    public virtual void Fill(T data)
    {
        
    }
}
