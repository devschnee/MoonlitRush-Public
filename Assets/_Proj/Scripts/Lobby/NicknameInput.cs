using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_InputField inputField;   
    [SerializeField] Button OkButton;        
    [SerializeField] GameObject panelRoot;

    public AudioSource source;
    public AudioClip buttonSound;
    void OnEnable()
    {
        // 패널이 열릴 때 입력창 자동 포커스 + 커서 깜빡임 활성화
        if (inputField != null)
        {
            inputField.caretBlinkRate = 0.85f;
            inputField.ActivateInputField();
        }

        // 버튼 클릭 이벤트 초기화 후 다시 연결
        if (OkButton != null)
        {
            OkButton.onClick.RemoveAllListeners();
            OkButton.onClick.AddListener(OnSubmitNickname);
        }
    }

    public void OnSubmitNickname()
    {
        string nick = inputField ? inputField.text.Trim() : "";

        if (string.IsNullOrEmpty(nick))
        {
            Debug.LogWarning("닉네임이 비어 있습니다.");
            return;
        }

        // 닉네임 저장
        PlayerPrefs.SetString("PlayerNickname", nick);
        PlayerPrefs.Save();

        // 패널 닫기
        if (panelRoot)
        panelRoot.SetActive(false);
            source.PlayOneShot(buttonSound);


        Debug.Log($"[NicknameInput] 닉네임 저장 완료: {nick}");
    }
}
