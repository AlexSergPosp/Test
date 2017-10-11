using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class VisualController : MonoBehaviour
{

    public GoldView gold;
    public GoldView tapGold;
    public GoldView passiveGold;
    private ConnectionCollector connection = new ConnectionCollector();
    private GameController gameController;
    public UpgradeListView list;
    public Button upgrade, menu, bg;

    private bool blockRayCasts = false;

    public static VisualController inst;
    public const string postfix = "/sec";


    public void Awake()
    {
        inst = this;
    }

    void Start () {

        gameController = GameController.inst;
        gold.count.text = gameController.playerData.gold.count.value.ToString();
        passiveGold.count.text = gameController.playerData.passiveGold.count.value.ToString() + postfix;

        connection.add = gameController.playerData.gold.count.Bind(val =>
        {
            gold.count.text = val.ToString();
        });
        connection.add = gameController.playerData.passiveGold.count.Bind(val =>
        {
            passiveGold.count.text = val.ToString() + postfix; ;
        });

        connection.add = gameController.playerData.tapGold.count.Bind(val =>
        {
            tapGold.count.text = val.ToString() + " for tap"; ;
        });


        upgrade.onClick.AddListener(() =>
        {
            PopupController.Show(PopupType.Upgrades, gameController.playerData.upgrades);
        });

        menu.onClick.AddListener(() =>
        {
            PopupController.Show(PopupType.Menu, "");
        });

        CreateText();

        bg.ClickStream().Listen(() => {

            var tapGold = gameController.playerData.tapGold.count.value;
            Sayit(tapGold.ToString(), Color.white, Camera.main.ScreenToViewportPoint(transform.position));
            gameController.playerData.gold.Apply(tapGold);
        });

    }

    private InstantiationPool<Hit> damageText;

    public void CreateText()
    {
        damageText = new InstantiationPool<Hit>("Prefabs/Hit");
        damageText.onUse = (createdText) =>
        {
            createdText.transform.SetParent(this.transform, false);

            int yOffset = Random.Range(-200, -100);
            createdText.textView.rectTransform.offsetMin = new Vector2(0.0F, 0.0f);
            createdText.textView.rectTransform.offsetMax = new Vector2(0.0F, 0.0f);
            createdText.textView.rectTransform.sizeDelta = new Vector2(180.0F, 100.0F);
        };

        damageText.onRecycle = text =>
        {
            text.GetComponent<Animation>().Stop();
        };
    }

    public void Sayit(string text, Color color, Vector3 position)
    {
        Hit txt = damageText.Create();
        damageText.prefab.transform.position = new Vector3(Random.Range(-150, 150), Random.Range(0, 300));
        txt.GetComponent<Animation>().Play("RisingText");
        txt.animationLenght = 1.0f;
        txt.Adjust(text);

        //txt.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
        txt.textView.GetComponent<RectTransform>().localScale = new Vector3(0.8f, 0.8f, 1.0f);
        txt.textView.color = color;
    }
}
