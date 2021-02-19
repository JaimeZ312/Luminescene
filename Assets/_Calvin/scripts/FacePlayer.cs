using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    private void Awake()
    {
        transform.LookAt(Vector3.zero);
        //Invoke("Destroy", 5f);
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }
}
