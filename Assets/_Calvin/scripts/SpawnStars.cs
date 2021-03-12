using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnStars : MonoBehaviour
{
    public Transform[] spawnLocation;

    public GameObject[] star;

    public int i;
    public int maxNumberOfStars;

    void Start()
    {

    }

    public void SpawnFirstStar()
    {
        Instantiate(star[1], spawnLocation[1].position, Quaternion.identity);
    }

    public void SpawnSecondStar()
    {
        Instantiate(star[2], spawnLocation[2].position, Quaternion.identity);
    }

    public void SpawnThirdStar()
    {
        Instantiate(star[3], spawnLocation[3].position, Quaternion.identity);
    }

    
    public void SpawnLastStar()
    {
        Instantiate(star[0], spawnLocation[0].position, Quaternion.identity );
        Instantiate(star[4], spawnLocation[0].position, Quaternion.identity);
    }

   
}
