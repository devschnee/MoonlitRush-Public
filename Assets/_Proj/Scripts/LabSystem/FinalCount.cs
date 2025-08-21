using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public enum FinishType
{
  PlayerWin,
  AIWin
}
public class FinalCount : MonoBehaviour
{
  public static FinalCount Instance;

  public TextMeshProUGUI finalCountText;
  public int defaultSeconds = 10;

  public CarController playerCar;
  public AICarController AICar;

  private Coroutine countdownCoroutine;
  private bool isCounting = false;

  private void Awake()
  {
    Instance = this;
    if (finalCountText) finalCountText.gameObject.SetActive(false);
  }
  public void FinishPlayer()
  {
    if (isCounting) return;
    countdownCoroutine = StartCoroutine(CountdownCoroutine(FinishType.PlayerWin, defaultSeconds));
  }

  public void FinishAI()
  {
    if (isCounting) return;
    countdownCoroutine = StartCoroutine(CountdownCoroutine(FinishType.AIWin, defaultSeconds));
  }

  IEnumerator CountdownCoroutine(FinishType type, int sec)
  {
    isCounting = true;
    finalCountText.gameObject.SetActive(true);

    for (int i = sec; i > 0; i--)
    {
      finalCountText.text = i.ToString();
      yield return new WaitForSeconds(1f);

      // 카운트다운 중 상대방 골인 여부 체크
      if (type == FinishType.PlayerWin && AICar.isFinished)
      {
        yield return ShowResult("Win!", type);
        yield break;
      }

      if (type == FinishType.AIWin && playerCar.isFinished)
      {
        yield return ShowResult("Finish!", type);
        yield break;
      }
    }

    // 카운트다운 끝날때까지 상대방 노 골. no goal 차는 시간 기록 none
    if (type == FinishType.PlayerWin)
      yield return ShowResult("Win!", type); // AI 기록 없음
    else
      yield return ShowResult("Time Over!", type); // Player 기록 없음
  }
  IEnumerator ShowResult(string msg, FinishType type)
  {
    finalCountText.text = msg;

    if (type == FinishType.PlayerWin)
    {
      TimeManager.Instance.RecordFinish(playerCar.GetComponent<RacerInfo>().displayName);

      if (!AICar.isFinished)
        TimeManager.Instance.RecordNone(AICar.GetComponent<RacerInfo>().displayName);
      else
        TimeManager.Instance.RecordFinish(AICar.GetComponent<RacerInfo>().displayName);
      StopAllAI();
      StopPlayer();
    }
    else if (type == FinishType.AIWin)
    {
      TimeManager.Instance.RecordFinish(AICar.GetComponent<RacerInfo>().displayName);

      if(!playerCar.isFinished)
        TimeManager.Instance.RecordNone(playerCar.GetComponent<RacerInfo>().displayName);
      else
        TimeManager.Instance.RecordFinish(playerCar.GetComponent<RacerInfo>().displayName);
      StopAllAI();
      StopPlayer();

      if (!playerCar.isFinished)
        playerCar.StartCoroutine(playerCar.SmoothStop(2f));

      foreach(var ai in FindObjectsOfType<AICarController>())
      {
        if (!ai.isFinished) { }
          //ai.StartCoroutine(ai.SmoothStop(2f));
      }
    }
      yield return new WaitForSeconds(2f);

    // LoadScene이 static이면 SceneManagers.LoadScene("EndingScene")으로 변경
    SceneManagers.Instance.LoadScene("EndingScene");

    finalCountText.gameObject.SetActive(false);
    isCounting = false;
  }
  public void StopAllAI()
  {
    foreach (var ai in FindObjectsOfType<AICarController>())
    {
      ai.isFinished = true;
      ai.moveInput = 0;
      ai.steerInput = 0;
    }
  }

  public void StopPlayer()
  {
    playerCar.isFinished = true;
    playerCar.moveInput = 0;
    playerCar.steerInput = 0;
  }
  public event System.Action OnCountdownFinished;
}
