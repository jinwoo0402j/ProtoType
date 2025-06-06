using UnityEngine;
using System.Collections.Generic; // 의도 패턴 등을 위해 사용할 수 있습니다.
using System.Linq; // 판정승 규칙 등에 사용될 수 있음

// EnemyPart.cs에 EnemyIntentType 및 EnemyIntent가 정의되어 있으므로 여기서는 제거합니다.
// public enum EnemyActionType { ... } 
// [System.Serializable] public class EnemyIntent { ... }

[System.Serializable]
public struct PartData // 인스펙터에서 적의 부위 정보를 설정하기 위한 구조체
{
    public string partName;
    public int maxHealth;
    public bool isCore;
    public List<EnemyIntent> possibleIntents; // EnemyPart.cs의 EnemyIntent 사용
    public GameObject partPrefab; // 부위의 시각적 표현을 위한 프리팹 (선택 사항)
}

public class Enemy : MonoBehaviour
{
    public string enemyName = "기본 슬라임";
    // public int currentBlock = 0; // 부위별 방어력으로 대체되므로 제거
    public List<EnemyPart> parts = new List<EnemyPart>();
    // public bool has판정승Rule = false; // IsDefeated() 로직에 통합 예정
    public bool isDead = false;

    public List<PartData> enemyPartDataList = new List<PartData>(); // 인스펙터에서 설정할 부위 데이터
    [SerializeField] public List<Transform> partUIPositionPlaceholders = new List<Transform>(); // 각 부위 UI가 표시될 위치 지정용 오브젝트

    // private EnemyIntent nextAction; // 부위별 행동으로 대체되므로 제거
    // public List<EnemyIntent> actionPatterns = new List<EnemyIntent>(); // 부위별 행동으로 대체되므로 제거


    void Awake()
    {
        InitializeParts(); 
        // InitializeActionPatterns(); // 부위별 행동 초기화로 대체되므로 제거
    }

    void Start()
    {
        if (IsDefeated()) // 초기 상태부터 사망 판정
        {
            Die(enemyName + " (은)는 시작부터 활동 불가 상태입니다.");
        }
        else
        {
            ChooseAllPartIntents(); // 모든 부위 행동 선택
        }
    }

    void InitializeParts()
    {
        parts.Clear(); // 기존 부위 정보 초기화

        if (enemyPartDataList == null || enemyPartDataList.Count == 0)
        {
            Debug.LogError(string.Format("{0} : enemyPartDataList가 비어있거나 설정되지 않았습니다! 부위를 초기화할 수 없습니다.", enemyName));
            return;
        }

        for (int i = 0; i < enemyPartDataList.Count; i++)
        {
            PartData data = enemyPartDataList[i];
            GameObject partGameObject;

            if (data.partPrefab != null)
            {
                // 프리팹이 있으면 인스턴스화
                partGameObject = Instantiate(data.partPrefab, transform);
                partGameObject.name = data.partName; // 게임 오브젝트 이름 설정
            }
            else
            {
                // 프리팹이 없으면 빈 GameObject 생성
                partGameObject = new GameObject(data.partName);
                partGameObject.transform.SetParent(transform); // Enemy의 자식으로 설정
                partGameObject.transform.localPosition = Vector3.zero; // 위치 초기화 (필요에 따라 조정)
            }
            
            EnemyPart partComponent = partGameObject.AddComponent<EnemyPart>();
            partComponent.Setup(data.partName, data.maxHealth, new List<EnemyIntent>(data.possibleIntents), data.isCore);
            parts.Add(partComponent);

            Debug.Log(string.Format("부위 생성: {0}, 체력: {1}, 코어: {2}, 행동 가능 개수: {3}", 
                data.partName, data.maxHealth, data.isCore, data.possibleIntents != null ? data.possibleIntents.Count : 0));
        }

        if (parts.Count == 0)
        {
            Debug.LogError(string.Format("{0}: 부위가 하나도 생성되지 않았습니다. enemyPartDataList 설정을 확인해주세요.", enemyName));
            isDead = true; // 부위가 없으면 즉시 사망 처리
        }
        else if (!parts.Any(p => p.isCorePart))
        {
            Debug.LogWarning(string.Format("{0}: 코어 부위가 지정되지 않았습니다. 첫 번째 부위를 코어로 간주합니다.", enemyName));
            // 또는 게임 디자인에 따라 코어 부위가 없으면 안 되도록 에러 처리할 수도 있음
            if (parts.Count > 0) parts[0].isCorePart = true; 
        }
    }

    // public void InitializeActionPatterns() { ... } // 제거

    public void ChooseAllPartIntents()
    {
        if (isDead) return;
        Debug.Log(string.Format("{0}의 모든 부위 행동 결정 시작", enemyName));
        foreach (EnemyPart part in parts)
        {
            if (!part.isDestroyed && !part.isCorePart) // 파괴되지 않았고, 코어 부위가 아닌 경우에만 행동 선택
            {
                part.ChooseNextIntent();
            }
            else if (part.isCorePart)
            {
                part.currentIntent = null; // 코어 파츠는 행동 없음 명시
            }
        }
    }
    
    public void ExecuteAllPartIntents(Player targetPlayer)
    {
        if (isDead)
        {
            Debug.LogWarning(string.Format("{0}은(는) 이미 죽어서 행동할 수 없습니다.", enemyName));
            return;
        }

        Debug.Log(string.Format("---- {0} 행동 시작 ----", enemyName));
        foreach (EnemyPart part in parts)
        {
            if (!part.isDestroyed && part.currentIntent != null && !part.isCorePart)
            {
                Debug.Log(string.Format("{0} 부위 ({1}) 행동 실행: {2}", enemyName, part.partName, part.currentIntent.GetDescription()));
                ExecutePartIntent(part, targetPlayer);
            }
        }
        Debug.Log(string.Format("---- {0} 행동 종료 ----", enemyName));
        // 모든 행동 실행 후 다음 턴을 위해 다시 의도 선택
        // ChooseAllPartIntents(); // 이 호출은 CombatManager의 턴 관리 로직에서 이루어져야 함
    }

    private void ExecutePartIntent(EnemyPart part, Player targetPlayer)
    {
        // part.currentIntent를 기반으로 실제 행동 실행
        switch (part.currentIntent.intentType)
        {
            case EnemyIntentType.Attack:
                if (targetPlayer != null)
                {
                    Debug.Log(string.Format("{0} ({1})이(가) 플레이어에게 {2} 피해를 입힙니다.", enemyName, part.partName, part.currentIntent.value));
                    targetPlayer.TakeDamage(part.currentIntent.value);
                }
                break;
            case EnemyIntentType.Defend:
                part.AddBlock(part.currentIntent.value);
                // Debug.Log은 AddBlock 내부에서 처리
                break;
            case EnemyIntentType.Buff:
                // TODO: 자신 또는 다른 부위에 버프 적용 로직
                Debug.Log(string.Format("{0} ({1})이(가) 버프 ({2})를 시전합니다.", enemyName, part.partName, part.currentIntent.description));
                break;
            case EnemyIntentType.Debuff:
                // TODO: 플레이어에게 디버프 적용 로직
                Debug.Log(string.Format("{0} ({1})이(가) 디버프 ({2})를 시전합니다.", enemyName, part.partName, part.currentIntent.description));
                break;
            default:
                Debug.LogWarning(string.Format("알 수 없는 부위 행동 유형: {0}", part.currentIntent.intentType));
                break;
        }
    }

    public void ResetAllPartBlocks()
    {
        if (isDead) return;
        // Debug.Log(string.Format("{0}의 모든 부위 방어력 초기화 시작", enemyName));
        foreach (EnemyPart part in parts)
        {
            if (!part.isDestroyed)
            {
                part.ResetBlock();
            }
        }
    }

    // 특정 부위에 피해를 주는 메서드 (플레이어의 공격이 호출)
    public void TakeDamageToPart(int partIndex, int amount)
    {
        if (isDead)
        {
            Debug.LogWarning(string.Format("{0}은(는) 이미 죽어서 피해를 받을 수 없습니다.", enemyName));
            return;
        }

        if (partIndex < 0 || partIndex >= parts.Count)
        {
            Debug.LogError(string.Format("잘못된 부위 인덱스({0})입니다. {1}의 부위 개수: {2}", partIndex, enemyName, parts.Count));
            return;
        }

        EnemyPart targetPart = parts[partIndex];
        if (targetPart.isDestroyed)
        {
            Debug.Log(string.Format("{0} 부위는 이미 파괴되어 피해를 입힐 수 없습니다.", targetPart.partName));
            // 이미 파괴된 부위를 공격했을 때의 추가 규칙 (예: 다른 부위로 피해 이전 등)이 없다면 여기서 종료
            return;
        }

        Debug.Log(string.Format("{0}의 {1} 부위({2}번째 인덱스)에 {3}의 피해를 시도합니다.", enemyName, targetPart.partName, partIndex, amount));
        targetPart.TakeDamage(amount);

        // 피해 후 사망 여부는 CombatManager가 IsDefeated()를 통해 확인하고 Die()를 호출할 것이므로 여기서는 직접 호출하지 않음
        // if (IsDefeated())
        // {
        //     Die(enemyName + " 전투 중 사망");
        // }
    }

    // void CheckDeath() // IsDefeated() 로 대체
    public bool IsDefeated()
    {
        if (isDead) return true; // 이미 Die()가 호출된 경우

        // 1. 코어 부위 파괴 여부 확인
        EnemyPart corePart = parts.FirstOrDefault(p => p.isCorePart);
        if (corePart != null && corePart.isDestroyed)
        {
            // Die(string.Format("{0}의 핵심 부위 '{1}'(이)가 파괴되어 사망합니다.", enemyName, corePart.partName));
            return true;
        }

        // 2. 코어 부위가 없거나, 코어 부위가 아직 파괴되지 않았을 경우,
        //    코어를 제외한 모든 다른 부위가 파괴되었는지 확인
        bool allNonCorePartsDestroyed = true;
        bool hasNonCoreParts = false;
        foreach (EnemyPart part in parts)
        {
            if (!part.isCorePart)
            {
                hasNonCoreParts = true;
                if (!part.isDestroyed)
                {
                    allNonCorePartsDestroyed = false;
                    break;
                }
            }
        }

        if (hasNonCoreParts && allNonCorePartsDestroyed)
        {
            // 코어를 제외한 모든 부위가 파괴되었다면 사망
            // Die(string.Format("{0}의 코어 제외 모든 부위가 파괴되어 사망합니다.", enemyName));
            return true;
        }
        
        // (추가 조건) 만약 부위가 하나뿐이고 그게 코어가 아니면서 파괴된 경우도 사망으로 처리할 수 있음
        // (또는 모든 부위가 파괴된 경우 - 이 경우는 위의 두 조건 중 하나에 포함될 가능성이 높음)
        // 현재는 명시된 두 가지 규칙만 적용

        return false; // 위의 조건에 해당하지 않으면 아직 살아있음
    }

    public void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        Debug.Log(string.Format("<b>{0} 사망! 이유: {1}</b>", enemyName, reason));
        // 모든 행동 중지, 스프라이트 변경, 전투 관리자에게 알림 등 추가 처리
        foreach (var part in parts)
        {
            part.currentIntent = null; // 모든 부위 행동 취소
        }
        // 게임 오브젝트를 즉시 파괴하지 않고, 비활성화하거나 다른 처리를 할 수 있음
        // gameObject.SetActive(false);
    }

    public string GetStatusRepresentation()
    {
        if (isDead) return string.Format("{0} (사망)", enemyName);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(string.Format("== {0} ==\n", enemyName));
        // sb.Append(string.Format("전체 방어력: {0}\n", currentBlock)); // currentBlock 제거됨

        if (parts == null || parts.Count == 0)
        {
            sb.Append("  부위 정보 없음\n");
        } 
        else
        {
            for (int i = 0; i < parts.Count; i++)
            {
                EnemyPart part = parts[i];
                sb.Append(string.Format("  부위 {0}: {1} [{2}/{3}]", i, part.partName, part.currentHealth, part.maxHealth));
                if (part.isCorePart) sb.Append(" (코어)");
                if (part.currentBlock > 0) sb.Append(string.Format(" (방어력: {0})", part.currentBlock));
                if (part.isDestroyed) 
                {
                    sb.Append(" - 파괴됨");
                }
                else if (!part.isCorePart && part.currentIntent != null)
                {
                    sb.Append(string.Format(" -> 의도: {0}", part.currentIntent.GetDescription()));
                }
                else if (part.isCorePart)
                {
                     sb.Append(" -> (행동 없음)");
                }
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    public List<EnemyPart> GetTargetableParts()
    {
        if (isDead || parts == null)
        {
            return new List<EnemyPart>();
        }
        // 파괴되지 않은 부위만 반환
        return parts.Where(p => !p.isDestroyed).ToList();
    }
} 