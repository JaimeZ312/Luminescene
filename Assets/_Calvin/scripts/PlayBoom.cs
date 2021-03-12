using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayBoom : MonoBehaviour
{
    private void Awake()
    {
        Invoke("PlaySound", 1.5f);
    }

    public void PlaySound()
    {
        this.GetComponent<AudioSource>().Play();
    }
}
