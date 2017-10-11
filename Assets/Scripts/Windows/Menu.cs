using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : PopupWindow<string>
{
    public Text name, desc;
    public Button sound, donate, close;
    private bool cashed;


    public void Awake()
    {
        close.onClick.AddListener(Hide);
    }

    public override void Show(bool state, object data)
    {
        gameObject.SetActive(state);

        if (state && cashed)
        {
            
        } 
    }
}
