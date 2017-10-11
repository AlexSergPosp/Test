using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using UnityEngine;

public static class Utils
{
    public static T CastTo<T>(this string str)
        where T : new()
    {
        if (str.Length > 0)
        {
            if (str.Contains("%"))
            {
                str = str.Replace("%", "");
                double tempDouble = Convert.ToDouble(str) / 100.0;
                str = tempDouble.ToString();
            }

            try
            {
                return (T)Convert.ChangeType(str, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return new T();
            }
        }
        else return (T)Convert.ChangeType(0.0f, typeof(T));
    }

    public static T Take<T>(this List<T> list, int index)
    {
        T t = list[index];
        list.RemoveAt(index);
        return t;
    }

    static public T GetOrAddComponent<T>(this GameObject obj) where T : UnityEngine.Component
    {
        return obj.GetComponent<T>() ?? obj.AddComponent<T>();
    }
}

public static class OrderedDictionaryExt
{
    public static int GetIndex(this OrderedDictionary dictionary, string key)
    {
        int i = 0;
        foreach (var k in dictionary.Keys)
        {
            if (k as string == key)
            {
                return i;
            }
            i++;
        }
        return -1;
    }
}