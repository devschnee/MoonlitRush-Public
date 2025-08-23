using System.Collections;
using TMPro;
using UnityEngine;
using System;

public class FinalCount : MonoBehaviour
{
    public static FinalCount Instance;

    public TextMeshProUGUI finalCountText;
    public int defaultSeconds = 10;

    public CarController playerCar;
    public AICarController AICar;

    private bool isGameEnding = false;
    public event Action OnCountdownFinished;

    private void Awake()
    {
        Instance = this;
        if (finalCountText) finalCountText.gameObject.SetActive(false);
    }
    public void StartCountdown(int seconds)
    {
        if (isGameEnding) return;
        isGameEnding = true;

        finalCountText.gameObject.SetActive(true);
        StartCoroutine(CoCount(seconds > 0 ? seconds : defaultSeconds));
    }

    public void FinishAI() => StartCountdown(defaultSeconds);
    IEnumerator CoCount(int sec)
    {
        for (int i = sec; i > 0; i--)
        {
            finalCountText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        finalCountText.text = "Finish!";
        yield return new WaitForSeconds(1f);

        finalCountText.text = "";
        finalCountText.gameObject.SetActive(false);

        OnCountdownFinished?.Invoke();
    }
}

