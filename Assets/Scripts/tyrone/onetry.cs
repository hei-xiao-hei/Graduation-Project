using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class onetry : MonoBehaviour
{
    public AudioSource tryone;
    public AudioClip[] clips;
    private int index;//音频下标


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(tryone.isPlaying==false)
        {
            tryone.clip = clips[index++];
            tryone.Play();
        }
    }
}
