using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnExplosion : MonoBehaviour
{

    public GameObject particleExplosion;
    public GameObject particleExplosionTwo;


    public float minSpawnTime;
    public float maxSpawnTime;

    void Start()
    {
        //Invoke("Supernova", Random.Range(minSpawnTime,maxSpawnTime));
        Invoke("Supernova", 0f);

    }

    private void Update()
    {
        
    }

    void Supernova()
    {
        Instantiate(particleExplosion, transform.position, Quaternion.identity);
        Instantiate(particleExplosionTwo, transform.position, Quaternion.identity);

    }
}
