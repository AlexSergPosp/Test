using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeListView : PopupWindow<List<Upgrade>>
{
    public Button bg, close;

    public Transform parent;
    ConnectionCollector collector = new ConnectionCollector();

    private bool cached = false;

    public void Awake()
    {
        bg.onClick.AddListener(Hide);
        close.onClick.AddListener(Hide);
    }

    public override void Show(bool state, List<Upgrade> data)
    {
        gameObject.SetActive(state);
        if (state && ! cached)
        {
            foreach (var d in data)
            {
                var prefab = Instantiate(UnityEngine.Resources.Load("Prefabs/UpgradeView")) as GameObject;
                prefab.transform.SetParent(parent);
                var view = prefab.GetComponent<UpgradeView>();

                view.name.text = d.name;
                view.lvl.text = d.level.value.ToString();
                view.info.onClick.AddListener(() =>
                {
                    PopupController.Show(PopupType.UpgradeInfo, d);
                });

                collector.add = d.level.Bind(val =>
                {
                    view.lvl.text = val.ToString();
                    view.gold.text = (val*d.addResources.count.value).ToString();
                });
            }
            cached = true;
        }
    }
}
