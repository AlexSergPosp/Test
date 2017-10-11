using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hit : InstantiatableInPool
{

    public Text textView;
    public float animationLenght = 2f;

    static public string prefabName { get { return "HitText"; } }

    public void Adjust(string text)
    {
        textView.text = text;
    }

    void OnEnable()
    {
        Invoke("Recycle", animationLenght);
    }

    IEnumerator Launch()
    {
        yield return new WaitForSeconds(animationLenght);
        Recycle();
    }
}
