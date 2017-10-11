using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;
using System.Text;
using CellUtils;

public static class ReactiveUI
{
    static bool Check<T>(T val)
    {
        if (val == null)
        {
            Debug.Log(typeof(T).Name + " type is null");
            return false;
        }
        return true;
    }
    public static void SetContent(this Text text, object val)
    {
        if (!Check(text)) return;
        text.text = val.ToString();
        //return new EmptyDisposable();
    }

    public static IDisposable SetContent<T>(this Text text, ICell<T> cell)
    {
        if (!Check(text)) return new EmptyDisposable();
        return cell.Bind(obj => text.text = obj.ToString());
    }
    public static IDisposable SetContent<T>(this Text text, string format, ICell<T> cell)
    {
        if (!Check(text)) return new EmptyDisposable();
        return cell.Bind(obj => text.text = string.Format(format, obj));
    }
    public static IDisposable SetContent<T>(this Text text, string format, ICell<T> cell, params Func<T, object>[] parameters)
    {
        if (!Check(text)) return new EmptyDisposable();
        return cell.Bind(obj => text.text = string.Format(format, parameters.Select(func => func(obj))));
    }
    public static IDisposable SetContent<T>(this Text text, string format, params ICell<T>[] parameters)
    {
        if (!Check(text)) return new EmptyDisposable();
        return text.SetContent(parameters.ToCellOfCollection().Select(val => string.Format(format, val.Cast<object>().ToArray())));
    }

    public static IDisposable SetEnabled(this Button button, ICell<bool> enabled)
    {
        if (!Check(button)) return new EmptyDisposable();
        return enabled.Bind(val => button.enabled = val);
    }
    public static IDisposable SetInteractable(this Button button, ICell<bool> enabled)
    {
        if (!Check(button)) return new EmptyDisposable();
        return enabled.Bind(val => button.interactable = val);
    }

    public static IDisposable SetActive(this GameObject obj, ICell<bool> active)
    {
        if (!Check(obj)) return new EmptyDisposable();
        return active.Bind(obj.SetActiveSafe);
    }

    public static void SetActiveSafe(this GameObject obj, bool active)
    {
        if (!Check(obj)) return;
        if (obj.activeSelf == active) return;
        obj.SetActive(active);
    }

    public static void SetActiveSafe(this Component obj, bool active)
    {
        if (obj == null) return;
        if (obj.gameObject.activeSelf == active) return;
        obj.gameObject.SetActive(active);
    }

    public static void SetActiveFast(this GameObject self, bool state)
    {
        if (self.activeSelf != state)
        {
            self.SetActive(state);
        }
    }

    public static IDisposable SetSprite(this Image obj, ICell<Sprite> sprite)
    {
        if (!Check(obj)) return new EmptyDisposable();
        return sprite.Bind(val => obj.sprite = val);
    }
    public static void SetSprite(this Image obj, Sprite sprite)
    {
        if (!Check(obj)) return;
        obj.sprite = sprite;
    }
    public static IDisposable SetFill(this Image obj, ICell<float> fill)
    {
        if (!Check(obj)) return new EmptyDisposable();
        return fill.Bind(val => obj.fillAmount = val);
    }

    public static IDisposable SetFill(this Image obj, ICell<double> fill)
    {
        if (!Check(obj)) return new EmptyDisposable();
        return fill.Bind(val => obj.fillAmount = (float)val);
    }
}
