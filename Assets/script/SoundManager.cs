using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{  
    public AudioSource backgroundMusic;
    public AudioSource effect;
    public static SoundManager Instance;
    public AudioClip[] audioClips;
    private void Awake()
    {
        Instance = this;
    }     
    // Start is called before the first frame update
    void Start()
    {
        backgroundMusic.volume = 0.5F;
        backgroundMusic.clip = audioClips[0];
        backgroundMusic.loop = true;
        backgroundMusic.Play();
    }

    // Update is called once per frame
    void Update()
    {
      
    }
   public void PlayFX(int number)
    {
        effect.clip =audioClips[number];
        effect.Stop();
        effect.Play();
    }
}
