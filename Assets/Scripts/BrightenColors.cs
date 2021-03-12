using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrightenColors : MonoBehaviour
{
    float timeToStart = 2f;
    float timePassed;
    public float duration;
    Material oldMat;
    Color oldColor;
    Color tempColor;

    private void Start()
    {
        timePassed = 0f;
        duration = 10;
        oldMat = GetComponent<Renderer>().material;
        oldColor = oldMat.GetColor("_EmissionColor");
        tempColor = oldMat.color;
    }
    void Update()
    {
        ColorChange();
    }
    void ColorChange()
    {
        oldMat.SetColor("_EmissionColor", oldColor * Mathf.Lerp(-10, 4, timePassed));
        tempColor.a = Mathf.Lerp(oldMat.color.a, 0, timePassed);
        oldMat.color = tempColor;
        if (timePassed < 1)
        {
            timePassed += Time.deltaTime / duration;
        }
    }
}
