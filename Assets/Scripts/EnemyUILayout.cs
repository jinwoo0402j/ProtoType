using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyUILayout : MonoBehaviour
{
    [SerializeField] private GameObject enemyPartUIPrefab; // 인스펙터에서 할당할 EnemyPartUI 프리팹
    [SerializeField] private Transform containerTransform; // UI 요소들이 자식으로 추가될 부모 Transform (Layout Group이 없는 일반 패널)

    private List<EnemyPartUI> partUIs = new List<EnemyPartUI>();

    public void SetupLayout(List<EnemyPart> enemyParts, List<Transform> placeholders, Action<int> partSelectionCallback)
    {
        // 기존 UI들을 정리
        foreach (Transform child in containerTransform)
        {
            Destroy(child.gameObject);
        }
        partUIs.Clear();

        if (enemyPartUIPrefab == null)
        {
            Debug.LogError("EnemyUILayout: enemyPartUIPrefab이 할당되지 않았습니다!");
            return;
        }

        if (enemyParts.Count != placeholders.Count)
        {
            Debug.LogError("적 부위 개수와 UI 위치 지정자(Placeholder) 개수가 일치하지 않습니다! UI를 정상적으로 배치할 수 없습니다.");
        }

        // 각 부위에 대해 UI 인스턴스 생성 및 위치 지정
        for (int i = 0; i < enemyParts.Count; i++)
        {
            GameObject partUIObject = Instantiate(enemyPartUIPrefab, containerTransform);
            partUIObject.name = enemyParts[i].partName + " UI"; // 오브젝트 이름 설정

            // RectTransform을 가져와서 Placeholder의 위치를 복사
            if (i < placeholders.Count && placeholders[i] != null)
            {
                partUIObject.transform.position = placeholders[i].position;
            }
            
            EnemyPartUI partUIComponent = partUIObject.GetComponent<EnemyPartUI>();
            if (partUIComponent != null)
            {
                partUIComponent.Setup(enemyParts[i], i, partSelectionCallback);
                partUIs.Add(partUIComponent);
            }
            else
            {
                Debug.LogError(string.Format("{0} 프리팹에 EnemyPartUI 컴포넌트가 없습니다!", enemyPartUIPrefab.name));
                Destroy(partUIObject); // 잘못된 프리팹이면 파괴
            }
        }
    }

    // 모든 부위 UI의 표시를 업데이트
    public void UpdateAllPartStatuses()
    {
        foreach (EnemyPartUI partUI in partUIs)
        {
            partUI.UpdateUI();
        }
    }

    // 모든 부위의 타겟팅 가능 상태를 설정
    public void SetAllPartsTargetable(bool isTargetable)
    {
        foreach (EnemyPartUI partUI in partUIs)
        {
            // 파괴된 부위는 SetTargetable 내부에서 알아서 처리함
            partUI.SetTargetable(isTargetable);
        }
    }
} 