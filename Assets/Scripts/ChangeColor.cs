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
        Debug.Log("Color change called ");
        if (timeLeft > Time.deltaTime)
        {
            Debug.Log(oldMat.GetColor("_EmissionColor"));
            Debug.Log(newMat.GetColor("_EmissionColor"));
            oldMat.SetColor("_EmissionColor", Color.Lerp(oldMat.GetColor("_EmissionColor"), newMat.GetColor("_EmissionColor"), Time.deltaTime / timeLeft));
            
            //GetComponent<Renderer>().material.GetColor("_EmissionColor");
           // update the timer
           timeLeft -= Time.deltaTime;
        }
        else
        {
            oldMat.SetTexture("_MainTex", newMat.GetTexture("_MainTex"));
            oldMat.SetTexture("_EmissionMap", newMat.GetTexture("_EmissionMap"));
            //Destroy(gameObject);
        }
    }
}
