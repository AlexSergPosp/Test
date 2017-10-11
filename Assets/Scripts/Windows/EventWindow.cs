using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventWindow : PopupWindow<DeathEvent>
{
    public Text name;
    public Text desc;
    public Button close;
    public Gold gold;

    public void Awake()
    {
        close.onClick.AddListener(Hide);
        gold = GameController.inst.playerData.gold;
    }

    public override void Show(bool state, DeathEvent data)
    {
        gameObject.SetActive(state);

        if (state)
        {
            name.text = data.name;
            var st = data.strench * gold.count.value;
            gold.Apply((long)st);
            desc.text = string.Format(data.desc, st);
        }
    }
}
