using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeCubes : MonoBehaviour
{
    public GameObject cube;
    void Start()
    {
        for (int i = 0; i < 20; i++)
        {

            Instantiate<GameObject>(cube, new Vector3(Random.Range(-10, 10), Random.Range(-5, 5), 0), Quaternion.identity);
        }
    }


}
