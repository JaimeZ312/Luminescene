using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnStars : MonoBehaviour
{
    private Vector3 origin = new Vector3(0,0,0);
    public Transform[] spawnLocation;

    public GameObject[] star;

    public float distXZ;
    public float distYMin;
    public float distYMax;

    public float initialSpawnTime;
    public float repeatSpawnTime;

    public int i;
    public int numberOfStars;
    public int maxNumberOfStars;
    // Start is called before the first frame update
    void Start()
    {
        //InvokeRepeating("CreateSpawnLocation", initialSpawnTime, repeatSpawnTime);
    }

    // Update is called once per frame
    void Update()
    {
        //while (numberOfStars <= maxNumberOfStars)
        //{
        //    numberOfStars++;
        //    spawnLocation = origin + new Vector3(Random.Range(-distXZ, distXZ), Random.Range(distYMin, distYMax), Random.Range(-distXZ, distXZ));
        //    if (numberOfStars > maxNumberOfStars)
        //    {
        //        break;
        //    }
        //}
    }


    public void SpawnFirstStar()
    {
        Instantiate(star[0], spawnLocation[0].position, Quaternion.identity );
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
        Instantiate(star[0], spawnLocation[0].position, Quaternion.identity);
        Instantiate(star[4], spawnLocation[0].position, Quaternion.identity);
    }

    void CreateSpawnLocation()
    {

        if (i <= maxNumberOfStars)
        {
            Debug.Log(i);
            Instantiate(star[Random.Range(0, 2)], new Vector3(spawnLocation[i].position.x, spawnLocation[i].position.y, spawnLocation[i].position.z), Quaternion.identity);
            i++;
        }
        else Debug.Log("all stars spawned");
    }
}
