using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Singleton Instance : Accessible from anywhere
    public static TimeManager Instance;

    public class PlayerTimeData
    {
        public string playerName;
        public float finishTime; // -1이면 "None"
    }

  private Dictionary<string, PlayerTimeData> times = new Dictionary<string, PlayerTimeData>();
  
  private float raceStartTime; // Time.time
  private float raceEndTime;
    private bool isTiming;
    private float pausedTime;
    private float totalPausedDuration; // Total time paused so far
    private bool isPaused;

    // Time.time keeps passing, but the paused time needs to be subtracted from the record. if paused.
    public float RaceDuration => isTiming ? Time.time - raceStartTime - totalPausedDuration : raceEndTime - raceStartTime - totalPausedDuration;


    void Awake()
    {
        // Prevent duplication in other scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Maintained even where other scenes (Timer Data)
    }

    void Start()
    {
        // 에디터에서 빈 씬에서 테스트 할때 Play 누르자마자 실행되게 만들었음. 씬 여러개 연결되면 시작 타임 정해주고(카운트다운 이후 등) 삭제.
#if UNITY_EDITOR
        StartTimer();
#endif
    }

    // Start Timer: Record Time + Write
    public void StartTimer()
    {
        raceStartTime = Time.time;
        isTiming = true;
        isPaused = false;
        totalPausedDuration = 0;
    }

    // Stop Timer : Save current time
    public void StopTimer()
    {
        if (!isTiming) return;
        if (isPaused) ResumeTimer();
        raceEndTime = Time.time;
        isTiming = false;
    }

    // Pause: Saves the current time and sets it to stopped state
    public void PauseTimer()
    {
        if (!isTiming || isPaused) return;
        pausedTime = Time.time;
        isPaused = true;
    }

    // Unpause: Accumulates as much as the pause time
    public void ResumeTimer()
    {
        if (!isPaused) return;
        totalPausedDuration += Time.time - pausedTime;
        isPaused = false;
    }

    // Returns elapsed time as a string (minutes:seconds.milliseconds)
    public string GetFormatRaceTime()
    {
        float time = RaceDuration;
        int minutes = Mathf.FloorToInt(time / 60);
        float seconds = time % 60;
        return $"{minutes:00}:{seconds:00.000}";
    }

    // Reset Timer
    public void ResetTimer()
    {
        raceStartTime = 0f;
        raceEndTime = 0f;
        isTiming = false;
        isPaused = false;
        pausedTime = 0f;
        totalPausedDuration = 0f;
    }

   public void RecordFinish(string racerName)
  {
    if (!times.ContainsKey(racerName))
    {
      float t = Time.time - raceStartTime - totalPausedDuration;
      times[racerName] = new PlayerTimeData {  playerName = racerName, finishTime = t };
    }
  }

  public void RecordNone(string racerName)
  {
    if (!times.ContainsKey(racerName))
    {
      times[racerName] = new PlayerTimeData { playerName = racerName, finishTime = -1f };
    }
  }

  public List<PlayerTimeData> GetRanking()
  {
    var list = new List<PlayerTimeData>(times.Values);
    
    // Sort racing records by fastest time
    list.Sort((a, b) =>
    {
      if (a.finishTime < 0 && b.finishTime < 0) return 0;
      if (a.finishTime < 0) return 1;
      if (b.finishTime < 0) return -1;
      return a.finishTime.CompareTo(b.finishTime);
    });
    return list;
  }
  public string FormatTime(float t)
  {
    if (t < 0) return "None";
    int m = Mathf.FloorToInt(t / 60f);
    float s = t % 60f;
    return $"{m:00} : {s:00.000}";
  }
    //public void RecordFinishTime(string pName, float fTime)
    //{
    //    string name = string.IsNullOrWhiteSpace(pName)
    //    ? (string.IsNullOrWhiteSpace(NicknameInput.PlayerNickname) ? "Player" : NicknameInput.PlayerNickname)
    //    : pName;

    //    var exist = data.FirstOrDefault(d => d.playerName == pName);
    //    if (exist != null)
    //    {
    //        exist.finishTime = Mathf.Min(exist.finishTime, fTime);
    //        return;
    //    }


    //    data.Add(new PlayerTimeData
    //    {
    //        playerName = pName,
    //        finishTime = fTime,
    //    });
    //}
}
