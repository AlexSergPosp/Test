using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public enum UpgradeState
{
    NotAvailable,
    Available,
    Buyed,
}

public enum UpgradeType
{
    Active,
    Passive,
    All
}

[System.Serializable]
public class Upgrade
{
    public string id;
    public string name;
    public string desc;

    public Resources cost;
    public Resources addResources;
    public UpgradeType upgradeType;

    public IntCell level = new IntCell(0);
    public Cell<UpgradeState> state = new Cell<UpgradeState>(UpgradeState.Available);

    public virtual void Apply()
    {
        if (state.value != UpgradeState.Buyed)
            state.value = UpgradeState.Buyed;
        else
        {
            switch (upgradeType)
            {
                case UpgradeType.Active:
                    if (GameController.inst.playerData.gold.TryConsume(cost.count.value * level.value))
                    {
                        level.value++;
                    }
                    break;
                case UpgradeType.Passive:
                    if (GameController.inst.playerData.gold.TryConsume(cost.count.value * level.value))
                    {
                        level.value++;
                    }
                    break;
                case UpgradeType.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
    }
}
