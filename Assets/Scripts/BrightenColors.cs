using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrightenColors : MonoBehaviour
{
    float timeToStart = 2f;
    float timePassed;
    public float duration;
    Material oldMat;
    Color oldColor, newColor;

    private void Start()
    {
        timePassed = 0f;
        duration = 10;
        oldMat = GetComponent<Renderer>().material;
        oldColor = oldMat.GetColor("_EmissionColor");
        newColor = oldColor * 4;
        //Invoke("CallCoroutine", 6f);
        CallCoroutine();
    }

    IEnumerator LerpFunction(float duration)
    {
        float time = 0;
        Color startValue = oldColor;

        while (time < duration)
        {
            oldMat.SetColor("_EmissionColor", oldColor * Mathf.Lerp(-10, 1, time / duration));
            time += Time.deltaTime;
            yield return null;
        }
        oldMat.SetColor("_EmissionColor", oldColor * 1);
    }

    void CallCoroutine()
    {
        StartCoroutine(LerpFunction(20));
    }
}
