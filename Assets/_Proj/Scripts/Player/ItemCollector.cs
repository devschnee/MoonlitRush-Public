using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
  public ItemSlot itemSlots;

  [Tooltip("PickupItem에 itemData없을때 사용하는 Scriptable Object")]
  public ItemData defaultBooster;
  public ItemData defaultShield;

  void OnTriggerEnter(Collider other)
  {
    Debug.Log("Trigger Enter with: " + other.name + " | Tag: " + other.tag);

    if (!other.CompareTag("ItemBooster") && !other.CompareTag("ItemShield") && !other.CompareTag("ItemMissile")) return;

    Debug.Log("Trigger with: " + other.tag);
    var pick = other.GetComponent<PickupItem>();

    // 데이터 선택 로직
    ItemData data = null;
    if (pick != null && pick.itemData != null) data = pick.itemData;
    else
    {
      if (other.CompareTag("ItemBooster")) data = defaultBooster;
      else if (other.CompareTag("ItemShield")) data = defaultShield;
      //else if (other.CompareTag("ItemMissile")) data = defaultMissile;
    }
    
    if (data == null || itemSlots == null) return;


    // 성공 시에만 이어서 파괴/리스폰 진행
    if(itemSlots.AddItem(data)) return;

    Vector3 pos = other.transform.position;
    Quaternion rot = other.transform.rotation;

    if(pick != null && pick.itemboxPrefab)
    {
      Destroy(other.gameObject);
      StartCoroutine(BoxRespawnCoroutine(pick.itemboxPrefab, pos, rot, pick.respawnDelay, other.tag));
    }
    else
    {
      // 프리팹 미지정 시 : 비활성 -> 3초 후 활성
      StartCoroutine(DisEnableCoroutine(other.gameObject, 3f));
    }
  }

  IEnumerator BoxRespawnCoroutine(GameObject prefab, Vector3 pos, Quaternion rot, float delay, string tagName)
  {
    yield return new WaitForSeconds(delay);
    var go = Object.Instantiate(prefab, pos, rot);
    go.tag = tagName;
  }

  IEnumerator DisEnableCoroutine(GameObject go, float delay)
  {
    go.SetActive(false);
    yield return new WaitForSeconds(delay);
    go.SetActive(true);
  }
}
