using UnityEngine;
using TMPro;
using UnityEngine.UI; // Close Button을 위해 사용

public class DeckViewUI : MonoBehaviour
{
    public GameObject panelGameObject;         // 인스펙터에서 할당할 덱 보기 패널
    public TextMeshProUGUI deckListText;    // 인스펙터에서 할당할 덱 리스트 표시용 텍스트
    public Button closeButton;              // 인스펙터에서 할당할 닫기 버튼 (선택 사항)

    void Awake()
    {
        if (panelGameObject == null)
        {
            panelGameObject = this.gameObject; // 스크립트가 패널 자체에 붙어있다면 이것으로 사용
        }
        if (deckListText == null)
        {
            Debug.LogError("DeckViewUI: deckListText가 할당되지 않았습니다!");
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide); // 닫기 버튼에 Hide 함수 연결
        }
        panelGameObject.SetActive(false); // 초기에는 숨겨둡니다.
    }

    public void DisplayDeck(string deckContent)
    {
        if (deckListText != null)
        {
            deckListText.text = deckContent;
        }
        panelGameObject.SetActive(true);
    }

    public void Hide()
    {
        panelGameObject.SetActive(false);
    }

    public bool IsVisible()
    {
        return panelGameObject.activeSelf;
    }
} 