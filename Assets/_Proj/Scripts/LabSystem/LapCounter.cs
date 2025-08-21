
using TMPro;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
  public TextMeshProUGUI lapText;
  public TextMeshProUGUI timeText;
  private float lapStartTime;

  public int currentLap = 0;
  //public int nextCheckpointIndex = 0;
  private bool raceFinished = false;
  public Checkpoint nextCheckpoint;  // 다음 체크포인트의 오브젝트를 직접 참조

  public CheckpointManager checkpointManager;

  private void Start()
  {
    if (checkpointManager == null)
    {
      checkpointManager = FindObjectOfType<CheckpointManager>();
    }

    if (checkpointManager != null && checkpointManager.allCheckPoints.Count > 0)
    {
      nextCheckpoint = checkpointManager.allCheckPoints[0];
    }

    lapStartTime = Time.time;
  }

  private void Update()
  {
    if (raceFinished) return;

    if (timeText != null && !raceFinished)
    {
      var tm = TimeManager.Instance;
      if (tm != null)
      {
        
      timeText.text = tm.GetFormatRaceTime();
      }
      else
      {
        print("TimeManager is null");
      }
    }
  }




  public void PassCheckpoint(Checkpoint passedCheckpoint)
  {
    if (raceFinished) return;

    // 통과한 체크포인트가 다음 체크포인트와 일치하는지 확인
    if (passedCheckpoint == nextCheckpoint)
    {
      // 다음 체크포인트를 설정
      nextCheckpoint = passedCheckpoint.nextCheckpoint;

      // 마지막 체크포인트를 통과했는지 확인
      if (passedCheckpoint.isFinalCheckpoint)
      {
        if (currentLap < RaceManager.Instance.totalLaps)
          currentLap++;

        if (currentLap >= RaceManager.Instance.totalLaps)
        {
          raceFinished = true;
          TimeManager.Instance.StopTimer();
          RaceManager.Instance.ActivateEndTrigger();
        }

        nextCheckpoint = checkpointManager.allCheckPoints[0];
      }
      if (lapText != null)
      {
        int displayLap = Mathf.Min(currentLap + 1, RaceManager.Instance.totalLaps);
        lapText.text = $"{displayLap} / {RaceManager.Instance.totalLaps} Lap";
      }
    }
  }
}
