using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlAlphaTimed : MonoBehaviour
{

    public Material matSphere;

    public Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("ApplyMaterial", 5.0f);

        rend = GetComponent<Renderer>();
        rend.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            Color color = matSphere.color;
            color.a += 10f * Time.deltaTime;
            matSphere.color = color;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Color color = matSphere.color;
            color.a -= 10f * Time.deltaTime;
            matSphere.color = color;
        }

        if (matSphere.color.a <= 0)
        {
            rend.enabled = false;
        }
    }

    void ApplyMaterial()
    {
        GetComponent<Renderer>().material = matSphere;
    }
}
