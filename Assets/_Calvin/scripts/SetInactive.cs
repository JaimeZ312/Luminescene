using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetInactive : MonoBehaviour
{

    public void Deactivate()
    {
        Debug.Log("testtesttest");
        gameObject.SetActive(false);
    }

}
