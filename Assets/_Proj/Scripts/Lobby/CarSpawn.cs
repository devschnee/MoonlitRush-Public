using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawn : MonoBehaviour
{
    public GameObject[] playerCar;
    public Transform spawnPoint; // 차량 시작 위치

    void Start()
    {
        int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);
        int carCount = PlayerPrefs.GetInt("CarCount", 0);


        if (selectedCarIndex >= 0 && selectedCarIndex < playerCar.Length)
        {
            GameObject gameObject1 = Instantiate(playerCar[selectedCarIndex], spawnPoint.position, spawnPoint.rotation);
            GameObject spawnPlayer = gameObject1;
        }
        else
        {
            Debug.LogError("선택된 차량 index가 잘못됨!");
        }
    }
        //GameObject car = Instantiate(
        //    playerCar[selectedIndex],
        //    spawnPoint.position,
        //    spawnPoint.rotation
        //);

    


}
