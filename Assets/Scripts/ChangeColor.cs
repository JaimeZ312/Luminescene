using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    float timeLeft;
    Material oldMat;
    public Material newMat;

    private void Start()
    {
        timeLeft = 5f;
        oldMat = GetComponent<Renderer>().material;
    }
    void Update()
    {
        ColorChange();
    }


    void ColorChange()
    {
        if (timeLeft > Time.deltaTime)
        {
            GetComponent<Renderer>().material.color = Color.Lerp(oldMat.color, newMat.color, Time.deltaTime / timeLeft);

            // update the timer
            timeLeft -= Time.deltaTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
