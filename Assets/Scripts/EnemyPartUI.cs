using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Action 대리자를 사용하기 위해 추가

public class EnemyPartUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI partNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI blockText;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private Button targetButton;
    [SerializeField] private Image borderImage; // 타겟팅 가능 여부를 표시할 테두리

    private EnemyPart associatedPart;
    private int partIndex;

    // CombatManager에 선택된 부위 인덱스를 전달하기 위한 콜백
    public Action<int> OnPartSelected; 

    public void Setup(EnemyPart part, int index, Action<int> selectionCallback)
    {
        associatedPart = part;
        partIndex = index;
        OnPartSelected = selectionCallback;
        
        targetButton.onClick.AddListener(HandleButtonClick);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (associatedPart == null) return;

        partNameText.text = associatedPart.partName;

        if (associatedPart.isDestroyed)
        {
            healthText.text = "<color=red>파괴됨</color>";
            blockText.gameObject.SetActive(false);
            intentText.gameObject.SetActive(false);
            targetButton.interactable = false; // 파괴되면 버튼 비활성화
        }
        else
        {
            healthText.text = string.Format("체력: {0}/{1}", associatedPart.currentHealth, associatedPart.maxHealth);
            
            if (associatedPart.currentBlock > 0)
            {
                blockText.gameObject.SetActive(true);
                blockText.text = string.Format("방어: {0}", associatedPart.currentBlock);
            }
            else
            {
                blockText.gameObject.SetActive(false);
            }

            intentText.gameObject.SetActive(true);
            intentText.text = associatedPart.GetIntentDescription();
            targetButton.interactable = true; // 파괴되지 않았으면 버튼 활성화
        }
    }

    // CombatManager가 호출하여 타겟팅 모드 활성화/비활성화
    public void SetTargetable(bool isTargetable)
    {
        targetButton.interactable = isTargetable && !associatedPart.isDestroyed;

        if (borderImage != null)
        {
            // 타겟팅 가능할 때만 테두리를 보여주는 효과 (예: 노란색)
            borderImage.color = isTargetable ? Color.yellow : Color.clear;
        }
    }

    private void HandleButtonClick()
    {
        // OnPartSelected 콜백이 설정되어 있을 때만 호출
        OnPartSelected?.Invoke(partIndex);
    }

    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 리스너 제거
        targetButton.onClick.RemoveListener(HandleButtonClick);
    }
} 