using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformUtils
{
    static public void SetLocalPosToZero(this RectTransform rect)
    {
        rect.localScale = new Vector3(1, 1, 1);
        rect.localPosition = new Vector3(0, 0, 0);
    }

    static public void SetPos(this RectTransform rect, Vector2 pos)
    {
        Vector2 size = rect.rect.size;
        rect.offsetMin = pos - size / 2;
        rect.offsetMax = pos + size / 2;
        rect.localScale = new Vector3(1, 1, 1);
    }

    static public void FillTransform(this RectTransform rect)
    {
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(0, 0);
        rect.offsetMax = new Vector2(0, 0);
        rect.localScale = new Vector3(1, 1, 1);
    }

    static public void DotTransform(this RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(0, 0);
        rect.offsetMax = new Vector2(0, 0);
        rect.localScale = new Vector3(1, 1, 1);
    }

    static public void SetHeight(this RectTransform rect, float height)
    {
        Vector2 offsetMin = rect.offsetMin;
        Vector2 offsetMax = rect.offsetMax;

        offsetMin.y = -height / 2;
        offsetMax.y = height / 2;

        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static public void SetSize(this RectTransform rect, float width, float height)
    {
        Vector2 offsetMin = rect.offsetMin;
        Vector2 offsetMax = rect.offsetMax;

        offsetMin.y = -height / 2;
        offsetMax.y = height / 2;

        offsetMin.x = -width / 2;
        offsetMax.x = width / 2;

        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}



