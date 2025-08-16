using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIItemControl : MonoBehaviour
{
    public int currentRank; //현재 순위

    public string getItem; //보유한 아이템

    //아이템 사용 판단 시간
    private float decisionDelay = 1f;
    private float lastDecisionTime;

    private Transform aiTransform;

    private RaceManager raceManager;

    private void Awake()
    {
        aiTransform = this.transform;
    }

    private void Start()
    {
        raceManager = RaceManager.Instance;
    }

    private void Update()
    {
        if(Time.time - lastDecisionTime > decisionDelay)
        {
            lastDecisionTime = Time.time;
        }
    }

    //앞에 공격 대상 유무 확인
    private bool HasTTargetAhead()
    {
        return false;
    }

    //뒤에 추격 대상 유무 확인
    private bool IsTargetBehind()
    {
        return false;
    }

    public void ItemGet(string itemName)
    {
        getItem = itemName;
        Debug.Log($"{transform.name}이(가) {getItem}아이템을 획득");
    }

    //아이템 사용 로직
    void UseItem() 
    {
        if (string.IsNullOrEmpty(getItem)) return; //보유 아이템 없으면 종료

        switch (getItem)
        {
            case "Missile":

                break;

            case "Shield":

                break;

            case "Boost":

                break;
        }
    }

    void UseMissile()
    {

    }

    void UseShield()
    {

    }

    void UseBoost()
    {

    }

}