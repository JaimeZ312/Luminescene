using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBehaviour : MonoBehaviour
{
    public float duration = 5.0f;

    public float intensity = 5f;

    float counter = 0f;

    public List<Light> pointLights = new List<Light>();
    void Start()
    {
        for (int i = 0; i < pointLights.Count; i++)
        {
            pointLights[i] = GetComponentInChildren<Light>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*float phi = Time.time / duration * 2 * Mathf.PI;
        float amplitude = Mathf.Cos(phi) * intensity;
        pointLight.intensity = amplitude;*/
    }

    public void BrightenUp()
    {
        Debug.Log("Brighten Up called");
        for (int i = 0; i < pointLights.Count; i++)
        {
            Debug.Log(pointLights.Count);
            for (float counter = 0; counter < duration; counter += Time.deltaTime)
            {
                pointLights[i].intensity = Mathf.Lerp(0, 3, counter / duration);
            }
        }
    }
}
