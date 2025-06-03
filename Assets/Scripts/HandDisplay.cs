using UnityEngine;
using TMPro; // UnityEngine.UI 대신 TMPro 네임스페이스를 사용합니다.

[RequireComponent(typeof(TextMeshProUGUI))] // TextMeshProUGUI 컴포넌트가 필요하도록 변경합니다.
public class HandDisplay : MonoBehaviour
{
    private TextMeshProUGUI uiText; // 타입을 TextMeshProUGUI로 변경합니다.

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>(); // GetComponent도 TextMeshProUGUI로 변경합니다.
        if (uiText == null)
        {
            Debug.LogError("HandDisplay: TextMeshProUGUI component not found on this GameObject!");
        }
    }

    public void ShowHand(string handDescription)
    {
        if (uiText != null)
        {
            uiText.text = handDescription; // .text 속성은 동일하게 사용 가능합니다.
        }
    }
} 