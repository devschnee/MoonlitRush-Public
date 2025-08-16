using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class StartCount : MonoBehaviour
{
    public TextMeshProUGUI startCountText;

   //public CarController playerCar;
    public AICarController_2 AICar;

    private void Start()
    {
        StartCoroutine(CountRoutine());
    }

    IEnumerator CountRoutine()
    {
        SetMovement(false);

        for(int i = 3; i > 0; i--)
        {
            startCountText.text = i.ToString();
        yield return new WaitForSeconds(1f);
        }

        startCountText.text = "GO!";
        yield return new WaitForSeconds(1f);

        startCountText.text = "";

        SetMovement(true);
    }

    //카운트 중 움직임 막음
    void SetMovement(bool movement)
    {
        //if (playerCar != null)
        //{
        //    playerCar.moveStart = movement;
        //}

        if (AICar != null) {
            AICar.moveStart = movement;
        }
    }
}

//빈 오브젝트에 부착
//플레이어에 bool 변수 하나 추가 (ex: moveStart)
