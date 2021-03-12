using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBehaviour : MonoBehaviour
{

    Light pointLight;
    void Start()
    {
        pointLight = GetComponent<Light>();
        CallCoroutine();
    }

    IEnumerator LerpFunction(float duration)
    {
        float time = 0;
        float initialValue = pointLight.intensity;

        while (time < duration)
        {
            pointLight.intensity = Mathf.Lerp(-10, initialValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        pointLight.intensity = initialValue;
    }

    void CallCoroutine()
    {
        StartCoroutine(LerpFunction(24));
    }
}
