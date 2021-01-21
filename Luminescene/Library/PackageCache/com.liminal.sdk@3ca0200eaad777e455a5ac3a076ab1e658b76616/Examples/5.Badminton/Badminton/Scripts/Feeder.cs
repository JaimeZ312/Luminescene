using System.Collections;
using UnityEngine;

public class Feeder : MonoBehaviour
{
    public Shuttle ShuttlePrefab;
    public Transform[] SpawnPoints;
    public float Force = 20;

    IEnumerator Start()
    {
        while (true)
        {
            var spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
            var shuttle = Instantiate(ShuttlePrefab, spawnPoint.position, spawnPoint.rotation);
            shuttle.Rigidbody.velocity = spawnPoint.forward * Force;
            yield return new WaitForSeconds(3);
        }
    }
}