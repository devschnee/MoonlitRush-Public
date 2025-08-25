using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    public static PlayerSFX Instance;

    public AudioSource mainSource; //외부소리 효과음
    public AudioSource intSource; //내부소리 효과음    
    public AudioClip idleClip;
    public AudioClip idleClipInt;
    public AudioClip boostClip;
    public AudioClip driftClip;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else { Destroy(gameObject); }

        MoveSound();
    }

  public  void MoveSound() //StartCount의 startClip 효과음과 바로 이어져야 한다... 근데 방법을 모르겠음,,, 시작카운트가 멈춤...
    {       
        //double dspTimeStart = AudioSettings.dspTime; //오디오 시스템 시간  
        //double dspTimeIdle = dspTimeStart + StartCount.Instance.startClip.length;

        mainSource.clip = idleClip;
        mainSource.loop = true;
        mainSource.Play();
       // mainSource.PlayScheduled(dspTimeIdle);

        intSource.clip = idleClipInt;
        intSource.loop = true;
        intSource.Play();
        //intSource.PlayScheduled(dspTimeIdle);
    }
        
    public void BoostSound()
    {
        mainSource.clip = boostClip;
        mainSource.Play();
    }

    public void DriftSound()
    {
        mainSource.clip = driftClip;
        mainSource.Play();
    }

   

}


