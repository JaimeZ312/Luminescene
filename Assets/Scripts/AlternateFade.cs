using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlternateFade : MonoBehaviour
{
    float timeToStart;
    float timePassed;
    public float duration;
    Material oldMat;
    Color tempColor;

    bool isStarting = false;

    // Start is called before the first frame update
    void Start()
    {
        timePassed = 0f;
        duration = 10f;

        oldMat = GetComponent<Renderer>().material;
        tempColor = oldMat.color;

        Invoke("EnBool", 10f);
    }

    // Update is called once per frame
    void Update()
    {
        if (isStarting == true)
        {
            ColourChange();
        }
        
    }

    void ColourChange()
    {
        
        if (timePassed < 1)
        {

            tempColor.a = Mathf.Lerp(oldMat.color.a, 0, timePassed);
            oldMat.color = tempColor;
            timePassed += Time.deltaTime / duration;


        }
    }

    public void EnBool()
    {
        isStarting = true;
    }
}
