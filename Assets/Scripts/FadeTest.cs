using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeTest : MonoBehaviour
{
    Material materialToChange;
    Color targetColor;

    void Start()
    {
        materialToChange = gameObject.GetComponent<Renderer>().material;
        targetColor = materialToChange.color;
        targetColor.a = 0;
        Invoke("CallCoroutine", 32f);
    }

    IEnumerator LerpFunction(Color endValue, float duration)
    {
        float time = 0;
        Color startValue = materialToChange.color;

        while (time < duration)
        {
            materialToChange.color = Color.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        materialToChange.color = endValue;
        this.gameObject.SetActive(false);
    }

    void CallCoroutine()
    {
        StartCoroutine(LerpFunction(targetColor, 3));
    }
}
