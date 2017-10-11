using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public enum PopupType
{
    None,
    Menu,
    Upgrades,
    UpgradeInfo,
    Event
}

public class PopupController : MonoBehaviour
{
    public List<PopupWindow> windows = new List<PopupWindow>();
    public Cell<PopupType> current = new Cell<PopupType>(PopupType.None);

    protected Stack<PopupType> Stack = new Stack<PopupType>();

    public static PopupController inst;

    private PopupWindow currentView = null;
    private IDisposable connection = Disposable.Empty;

    public void Awake()
    {
        inst = this;
        windows = GetComponentsInChildren<PopupWindow>(true).ToList();

        current.Bind(val =>
        {
            connection.Dispose();
            Debug.Log("Current Popup: " + val);
        });
    }

    public static T GetWindow<T>() where T : class
    {
        return inst.windows.FirstOrDefault(val => val is T) as T;
    }

    public static PopupWindow<T> Show<T>(PopupType type, T data) where T : class
    {
        var target = inst.windows.FirstOrDefault(val => val.type == type);

        if (target != null)
        {
            if (inst.current.value != PopupType.None)
                inst.currentView.Hide();

            target.Show(true, data);
            inst.current.value = type;

            inst.connection = target.closed.Listen(() =>
            {
                inst.currentView.Hide();
                inst.current.value = PopupType.None;
            });

            inst.currentView = target;
            return (PopupWindow<T>)target;
        }

        return null;
    }

    public static void CloseAll()
    {
        foreach (var w in inst.windows)
        {
            w.Show(false, null);
        }

        inst.current.value = PopupType.None;
    }
}
