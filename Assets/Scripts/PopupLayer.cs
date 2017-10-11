using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class PopupLayer : MonoBehaviour
{

    public Subject<Unit> click = new Subject<Unit>();

    public void Click()
    {
        click.OnNext(Unit.Default);
    }
}
