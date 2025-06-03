using UnityEngine;
using TMPro;
using System.Collections; // Coroutine 사용을 위해 필요

[RequireComponent(typeof(TextMeshProUGUI))] // TextMeshProUGUI 컴포넌트가 필요합니다.
public class GameMessageDisplay : MonoBehaviour
{
    private TextMeshProUGUI uiText;
    private Coroutine messageCoroutine;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        if (uiText == null)
        {
            Debug.LogError("GameMessageDisplay: TextMeshProUGUI component not found on this GameObject!");
            return;
        }
        uiText.text = ""; // 초기에는 비워둡니다.
    }

    public void ShowMessage(string message, float duration = 0f)
    {
        if (uiText == null) return;

        uiText.text = message;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        if (duration > 0f)
        {
            messageCoroutine = StartCoroutine(ClearMessageAfterDelay(duration));
        }
    }

    public void ClearMessage()
    {
        if (uiText != null)
        {
            uiText.text = "";
        }
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (uiText != null) {
            uiText.text = ""; // 지정된 시간이 지나면 메시지 지우기
        }
        messageCoroutine = null;
    }
} 