using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InstantiatableInPool : MonoBehaviour
{
    public IInstantiationPoolBase pool;
    [HideInInspector]
    public ConnectionCollector connections = new ConnectionCollector();

    public void Recycle()
    {
        if (pool == null)
        {
            //Debug.LogError("cant recycle because pool is not set");
            return;
        }
        connections.DisconnectAll();
        pool.Recycle(this);
    }

    public void RecycleWithDelay(float secs)
    {
        Invoke("Recycle", secs);
    }
}

public class PoolEntry : InstantiatableInPool
{
}

public interface IInstantiationPoolBase
{
    void Recycle(InstantiatableInPool inst);
}

public class InstantiationPool<T> : IInstantiationPoolBase
    where T : InstantiatableInPool
{
    public InstantiationPool()
    {
    }

    public InstantiationPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public InstantiationPool(string prefabPath)
    {
        path = prefabPath;
    }

    public Action<T> onRecycle;
    public Action<T> onCreated;
    // onUse called when object was created or reused
    public Action<T> onUse;

    public string path;
    public GameObject prefab;

    public List<T> usedObjects = new List<T>();
    List<T> recycledObjects = new List<T>();

    public void KillAll()
    {
        Action<T> kill = (T t) => GameObject.Destroy(t);
        foreach (var val in usedObjects)
        {
            kill(val);
        }
        foreach (var val in recycledObjects)
        {
            kill(val);
        }
        usedObjects.Clear();
        recycledObjects.Clear();
    }

    public void RecycleAll()
    {
        foreach (var inst in usedObjects)
        {
            inst.gameObject.SetActive(false);
            if (onRecycle != null) onRecycle(inst);
            inst.connections.DisconnectAll();
            recycledObjects.Add(inst);
        }
    }

    public void Recycle(InstantiatableInPool inst)
    {
        var t = inst as T;
        if (t == null)
        {
            Debug.LogError("wring object");
            return;
        }
        usedObjects.Remove(t);
        t.gameObject.SetActive(false);
        if (onRecycle != null) onRecycle(t);
        inst.connections.DisconnectAll();
        recycledObjects.Add(t);
    }

    public T Create(Transform parent = null, bool autoAddComponent = false)
    {
        T t = null;
        if (recycledObjects.Count > 0)
        {
            t = recycledObjects.Take(0);
        }
        else
        {
            if (prefab == null && !string.IsNullOrEmpty(path))
            {
                prefab = UnityEngine.Resources.Load<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError("loading prefab failed for path: " + path);
                    return null;
                }
                if (!autoAddComponent && prefab.GetOrAddComponent<T>() == null)
                {
                    Debug.LogError("loading prefab failed for path: " + path + " prefab has not " + typeof(T).Name + " component");
                    return null;
                }
            }
            if (prefab == null)
            {
                Debug.LogError("prefab is not set");
                return null;
            }
            var gameObject = UnityEngine.Object.Instantiate(prefab) as GameObject;

            t = autoAddComponent ? gameObject.GetOrAddComponent<T>() : gameObject.GetComponent<T>();

            if (parent != null) t.transform.SetParent(parent);
            t.transform.localScale = new Vector3(1f, 1f, 1f);
            t.pool = this;
            if (onCreated != null) onCreated(t);
        }
        if (parent != null && parent != t.transform.parent) t.transform.SetParent(parent);
        t.gameObject.SetActive(true);
        usedObjects.Add(t);
        if (onUse != null) onUse(t);
        return t;
    }
}