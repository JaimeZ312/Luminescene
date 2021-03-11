using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlAlphaTimed : MonoBehaviour
{
    public Material matRocks;
    public Renderer rend;

    public float amountToDecrease = 10.0f;
    public float timeForNextDecrease = 1.0f;
    private float timeToDecrease = 2.0f;
    private bool appliedMaterial = false;

    void Start()
    {
        Invoke("ApplyMaterial", 13f);

        rend = GetComponent<Renderer>();
        rend.enabled = true;
    }

    void Update()
    {
        if (appliedMaterial && timeToDecrease <= Time.time)
        {
            Color color = matRocks.color;
            color.a -= amountToDecrease * Time.deltaTime;
            matRocks.color = color;
            timeToDecrease = Time.time + timeForNextDecrease;
        }

        if (matRocks.color.a <= 0)
        {
            rend.enabled = false;
        }
    }

    void ApplyMaterial()
    {
        GetComponent<Renderer>().material = matRocks;
        appliedMaterial = true;
    }
}
