using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fadeTime : MonoBehaviour
{

    public Material materialRef;
    // Start is called before the first frame update
    void Start()
    {
        Invoke("UpdateMaterial", 5.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateMaterial()
    {
        GetComponent<Renderer>().material = materialRef;
    }
}
