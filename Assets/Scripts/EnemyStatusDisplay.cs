using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스를 사용합니다.

[RequireComponent(typeof(TextMeshProUGUI))] // TextMeshProUGUI 컴포넌트가 필요합니다.
public class EnemyStatusDisplay : MonoBehaviour
{
    private TextMeshProUGUI uiText;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        if (uiText == null)
        {
            Debug.LogError("EnemyStatusDisplay: TextMeshProUGUI component not found on this GameObject!");
        }
    }

    public void UpdateStatus(string statusDescription)
    {
        if (uiText != null)
        {
            uiText.text = statusDescription;
        }
    }
} 