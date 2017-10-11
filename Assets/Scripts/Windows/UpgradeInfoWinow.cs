using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeInfoWinow : PopupWindow<Upgrade>
{
    public Text name, desc, cost;
    public Image icon;
    public Button close, buy;

    private string cashed = "none";

    public void Awake()
    {
        close.onClick.AddListener(Hide);
    }

    public override void Show(bool state, Upgrade data)
    {
        gameObject.SetActive(state);

        if (state && !cashed.Equals(data.id))
        {
            name.text = data.name;
            desc.text = data.desc;

            connectionCollector.add = data.level.Bind(val => {
                cost.text = (data.cost.count.value * val).ToString();
            });
            buy.onClick.AddListener(data.Apply);
            cashed = data.id;
        }
    }
}
