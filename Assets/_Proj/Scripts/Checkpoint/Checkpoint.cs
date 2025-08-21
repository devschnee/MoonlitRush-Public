using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Check Point에 스크립트 적용
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public int checkpointId;
    public bool isFinalCheckpoint = false;
    public Checkpoint nextCheckpoint;

    public void SetNextCheckpoint(Checkpoint next)
    {
        nextCheckpoint = next;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인        
        RacerInfo racer = other.GetComponent<RacerInfo>();
        if (racer != null)
        {
            racer.lapCounter.PassCheckpoint(this);
        }
    }
}
