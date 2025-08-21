using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishZoneHandler : MonoBehaviour
{
  public int countdownSeconds = 10;

  private bool countdownStarted = false;

  void Reset()
  {
    var col = GetComponent<Collider>();
    if (col) col.isTrigger = true;
  }

  void OnTriggerEnter(Collider other)
  {
    if (countdownStarted) return;

    countdownStarted = true;

    if (FinalCount.Instance != null)
    {
      var player = other.GetComponentInParent<CarController>();
      if (player != null)
      {
        FinalCount.Instance.FinishPlayer();
        return;
      }

      var ai = other.GetComponentInParent<AICarController>();
      if (ai != null)
      {
        FinalCount.Instance.FinishAI();
        return;
      }
    }
  }
}
