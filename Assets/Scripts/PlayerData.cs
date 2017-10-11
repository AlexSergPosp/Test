using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UniRx;

[System.Serializable]
public class PlayerData {

    public Gold gold;
    public PasiiveGold passiveGold;
    public GoldForTap tapGold;
    public List<Upgrade> upgrades;

    [NonSerialized]
    public ConnectionCollector collector = new ConnectionCollector();

    public void Inicialization()
    {
        gold = new Gold(0);
        passiveGold = new PasiiveGold(0);
        tapGold = new GoldForTap(1);
        upgrades = new List<Upgrade>(GameController.inst.gameData.upgrades);


        foreach (var upgrade in upgrades)
        {
            collector.add = upgrade.state.Where(val => val == UpgradeState.Buyed).Listen(val =>
            {
                if (upgrade.upgradeType == UpgradeType.Active)
                    AddResourseGold(GameController.inst.playerData.tapGold, upgrade.addResources);

                if (upgrade.upgradeType == UpgradeType.Passive)
                    AddResoursePassiveGold(GameController.inst.playerData.passiveGold, upgrade.addResources);
            });
            collector.add = upgrade.level.Bind(val =>
            {
                if (upgrade.upgradeType == UpgradeType.Active)
                    AddResourseGold(GameController.inst.playerData.tapGold, upgrade.addResources);

                if (upgrade.upgradeType == UpgradeType.Passive)
                    AddResoursePassiveGold(GameController.inst.playerData.passiveGold, upgrade.addResources);
            });
        }

    }


    public void AddResourseGold(GoldForTap gold, Resources res)
    {
        gold.Apply(res.count.value);
    }

    public void AddResoursePassiveGold(PasiiveGold gold, Resources res)
    {
        gold.Apply(res.count.value);
    }
}
