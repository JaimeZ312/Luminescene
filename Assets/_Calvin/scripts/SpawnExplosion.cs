using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnExplosion : MonoBehaviour
{

    public GameObject particleExplosion;
    public float minWaitTime = 0f;
    public float maxWaitTime = 4f;

    void Start()
    {
        Invoke("Supernova", Random.Range(minWaitTime, maxWaitTime)) ;

    }

    private void Update()
    {
        
    }

    void Supernova()
    {
        Instantiate(particleExplosion, transform.position, Quaternion.identity);
    }
}
