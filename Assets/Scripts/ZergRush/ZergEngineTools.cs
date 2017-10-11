using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;


public static class ZergEngineTools
{
    class UnityActionDisposable : IDisposable
    {
        public UnityEvent e;
        public UnityAction action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

    class UnityActionDisposable<T> : IDisposable
    {
        public UnityEvent<T> e;
        public UnityAction<T> action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

    public static IStream<BaseEventData> PressStream(this Button b, bool up)
    {
        return new AnonymousStream<BaseEventData>((Action<BaseEventData> reaction, Priority p) =>
        {
            var trigger = b.GetComponent<EventTrigger>();
            var ua = new UnityAction<BaseEventData>(data => reaction(data));

            EventTrigger.TriggerEvent te = new EventTrigger.TriggerEvent();
            te.AddListener(ua);

            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            trigger.triggers.Add(new EventTrigger.Entry { eventID = up ? EventTriggerType.PointerUp : EventTriggerType.PointerDown, callback = te });
            return new UnityActionDisposable<BaseEventData> { action = ua, e = te };
        });
    }

    public static IDisposable BindClick(this Button button,Action action)
    {
        return button.ClickStream().Listen(action);
    }

    public static IEmptyStream ClickStream(this Button button)
    {
        if (button == null)
        {
            Debug.Log("button is null!!!");
            return new AbandonedStream();
        }

        return new AnonymousEmptyStream((Action reaction, Priority p) => {
            var ua = new UnityAction(reaction);
            button.onClick.AddListener(ua);
            return new UnityActionDisposable { action = ua, e = button.onClick };
        });
    }
    public static IStream<bool> ToggleStream(this Toggle button)
    {
        if (button == null)
        {
            Debug.Log("button is null!!!");
            return null;
        }

        return new AnonymousStream<bool>((Action<bool> reaction, Priority p) => {
            var ua = new UnityAction<bool>(reaction);
            button.onValueChanged.AddListener(ua);
            return new UnityActionDisposable<bool> { action = ua, e = button.onValueChanged };
        });
    }
    public static IEmptyStream HoldStream(this Button button, float time)
    {
        if (button == null)
        {
            Debug.Log("button is null!!!");
            return new AbandonedStream();
        }
        
        

        return new AnonymousEmptyStream((Action reaction, Priority p) => {
            var ua = new UnityAction(reaction);
            button.onClick.AddListener(ua);
            return new UnityActionDisposable { action = ua, e = button.onClick };
        });
    }
}
