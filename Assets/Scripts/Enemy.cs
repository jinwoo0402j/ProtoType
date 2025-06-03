using UnityEngine;
using System.Collections.Generic; // 의도 패턴 등을 위해 사용할 수 있습니다.
using System.Linq; // 판정승 규칙 등에 사용될 수 있음

// 적의 행동 유형 (간단하게 정의)
public enum EnemyActionType
{
    Attack,
    Defend,
    Buff,
    Debuff
}

// 적의 다음 행동을 나타내는 구조체 또는 클래스
[System.Serializable]
public class EnemyIntent
{
    public EnemyActionType actionType;
    public int value; // 공격력, 방어력 등
    // public Sprite intentIcon; // 나중에 UI에 표시할 의도 아이콘

    public EnemyIntent(EnemyActionType type, int val)
    {
        actionType = type;
        value = val;
    }

    public override string ToString()
    {
        return string.Format("의도: {0}, 값: {1}", actionType, value);
    }
}

public class Enemy : MonoBehaviour
{
    public string enemyName = "기본 슬라임";
    public int currentBlock = 0; // 몬스터 전체의 방어력 (또는 부위별 방어력으로 변경 가능)

    public List<EnemyPart> parts = new List<EnemyPart>();
    public bool has판정승Rule = false; // 몸통 제외 모든 부위 파괴 시 사망 규칙
    public bool isDead = false; // 접근 수준을 public으로 변경

    public List<EnemyIntent> actionPatterns = new List<EnemyIntent>();
    private EnemyIntent nextAction; // 이 부분은 추후 부위별 행동으로 변경될 수 있음

    void Awake()
    {
        InitializeParts(); // 부위 초기화 먼저
        InitializeActionPatterns(); // 그 다음 행동 패턴 초기화 (추후 부위와 연결)
    }

    void Start()
    {
        // 초기 사망 상태 확인 (모든 부위가 정상적으로 생성되었다면 살아있는 상태)
        CheckDeath(); 
        if (!isDead)
        {
            ChooseNextAction(); 
        }
    }

    void InitializeParts()
    {
        parts.Clear(); // 중복 초기화 방지
        
        // 몬스터 기획에 따라 다양한 부위와 각 부위의 체력을 설정해야 합니다.
        // 예시: 기본 슬라임은 몸통, 왼팔, 오른팔을 가집니다.

        // 1. 몸통 부위 (핵심 부위)
        int bodyHealth = 50; // 몸통 체력
        parts.Add(new EnemyPart("몸통", bodyHealth, true)); 

        // 2. 왼팔 부위
        int armHealth = 25; // 팔 체력
        parts.Add(new EnemyPart("왼팔", armHealth, false));

        // 3. 오른팔 부위
        parts.Add(new EnemyPart("오른팔", armHealth, false));

        Debug.Log(string.Format("{0}의 부위 초기화 완료. 총 {1}개의 부위 생성.", enemyName, parts.Count));
        foreach (var part in parts)
        {
            Debug.Log(string.Format(" - 부위명: {0}, 체력: {1}/{2}, 핵심부위: {3}", part.partName, part.currentHealth, part.maxHealth, part.isCorePart));
        }
    }

    void InitializeActionPatterns()
    {
        // 예시 행동 패턴 (추후 각 행동이 어느 부위에서 발동되는지 연결 필요)
        actionPatterns.Add(new EnemyIntent(EnemyActionType.Attack, 10));
        actionPatterns.Add(new EnemyIntent(EnemyActionType.Defend, 5));
    }

    public void ChooseNextAction()
    {
        if (isDead) return;
        // TODO: 부위 파괴 시스템과 연동하여, 행동 가능한 부위의 행동만 선택하도록 수정 필요
        if (actionPatterns.Count > 0)
        {
            nextAction = actionPatterns[Random.Range(0, actionPatterns.Count)];
            Debug.Log(string.Format("{0} 다음 행동: {1}", enemyName, nextAction.ToString()));
        }
        else
        { Debug.LogWarning(string.Format("{0}에게 설정된 행동 패턴이 없습니다.", enemyName)); }
    }

    public EnemyIntent GetNextAction() // 이 메서드는 UI 표시용이므로 유지
    {
        if (isDead) return null;
        return nextAction;
    }

    public void PerformAction(Player targetPlayer)
    {
        if (isDead || nextAction == null)
        {
            Debug.LogWarning(string.Format("{0}은(는) 죽었거나 다음 행동이 정해지지 않아 행동할 수 없습니다.", enemyName));
            return;
        }

        // TODO: 선택된 nextAction이 어떤 부위와 연결되어 있는지 확인하고, 해당 부위가 파괴되지 않았을 때만 실행
        //       우선은 기존 로직대로 전체 몬스터가 행동하는 것으로 유지

        currentBlock = 0; // 행동 시작 전 방어력 초기화 (몬스터 전체 방어력일 경우)

        Debug.Log(string.Format("{0} 행동 시작: {1}, 현재 방어력: {2}", enemyName, nextAction.ToString(), currentBlock));
        switch (nextAction.actionType)
        {
            case EnemyActionType.Attack:
                if (targetPlayer != null)
                {
                    Debug.Log(string.Format("{0}이(가) {1}에게 {2} 피해를 입힙니다.", enemyName, targetPlayer.name, nextAction.value));
                    targetPlayer.TakeDamage(nextAction.value);
                }
                break;
            case EnemyActionType.Defend:
                currentBlock += nextAction.value;
                Debug.Log(string.Format("{0}이(가) {1} 방어도를 얻습니다. 현재 방어력: {2}", enemyName, nextAction.value, currentBlock));
                break;
            default:
                Debug.LogWarning(string.Format("알 수 없는 적 행동 유형: {0}", nextAction.actionType));
                break;
        }
        ChooseNextAction();
    }

    // 특정 부위에 피해를 주는 메서드 (추후 카드 효과 등에서 호출)
    public void TakeDamageToPart(EnemyPart partToDamage, int amount)
    {
        if (isDead || partToDamage == null || !parts.Contains(partToDamage))
        {
            Debug.LogWarning("잘못된 부위 타겟이거나 몬스터가 이미 죽었습니다.");
            return;
        }

        // 방어력은 몬스터 전체에 적용 (기획에 따라 부위별 방어력으로 변경 가능)
        int damageToTake = amount;
        if (currentBlock > 0)
        {
            if (currentBlock >= damageToTake)
            {
                currentBlock -= damageToTake;
                Debug.Log(string.Format("{0} 전체 방어력으로 피해 {1} 흡수. 남은 방어력: {2}", enemyName, damageToTake, currentBlock));
                damageToTake = 0;
            }
            else
            {
                damageToTake -= currentBlock;
                Debug.Log(string.Format("{0} 전체 방어력 {1} 모두 소모. 남은 피해 {2}", enemyName, currentBlock, damageToTake));
                currentBlock = 0;
            }
        }

        if (damageToTake > 0)
        {
            partToDamage.TakeDamage(damageToTake);
        }
        else
        {
            Debug.Log(string.Format("{0} 모든 피해를 전체 방어력으로 막았습니다. 부위({1}) 피해 없음.", enemyName, partToDamage.partName));
        }
        CheckDeath();
    }

    // 기존 TakeDamage는 몸통(첫 번째 핵심 부위)에 피해를 주도록 임시 수정
    public void TakeDamage(int amount) 
    {
        if (isDead) return;

        EnemyPart coreBodyPart = parts.Find(p => p.isCorePart);
        if (coreBodyPart != null && !coreBodyPart.isDestroyed)
        {
            TakeDamageToPart(coreBodyPart, amount);
        }
        else // 핵심 몸통이 없거나 이미 파괴된 경우 (이런 상황은 적 기획에 따라 다르게 처리될 수 있음)
        {
            // 혹은 다른 부위를 순차적으로 타겟하거나, 첫 번째 살아있는 부위를 타겟
            EnemyPart alivePart = parts.Find(p => !p.isDestroyed);
            if (alivePart != null)
            {
                TakeDamageToPart(alivePart, amount);
            }
            else
            {
                 Debug.LogWarning(string.Format("{0}에게 피해를 줄 수 있는 부위가 없습니다.", enemyName));
            }
        }
    }

    void CheckDeath()
    {
        if (isDead) return; // 이미 사망 처리된 경우 중복 실행 방지

        EnemyPart corePart = parts.Find(p => p.isCorePart);
        if (corePart != null && corePart.isDestroyed)
        {
            Die(string.Format("{0}의 핵심 부위 '{1}'이(가) 파괴되어 사망했습니다.", enemyName, corePart.partName));
            return;
        }

        if (has판정승Rule)
        {
            bool allNonCorePartsDestroyed = true;
            foreach (EnemyPart part in parts)
            {
                if (!part.isCorePart && !part.isDestroyed)
                {
                    allNonCorePartsDestroyed = false;
                    break;
                }
            }
            if (allNonCorePartsDestroyed && (corePart == null || corePart.isDestroyed)) // 몸통까지 파괴되었거나, 몸통이 없는 경우 판정승
            {
                Die(string.Format("{0}의 모든 부위가 파괴되어 판정승으로 사망했습니다.", enemyName));
                return;
            }
            // 만약 몸통이 있고, 몸통 제외 모든 부위가 파괴되었을 때 판정승인 경우 (몸통은 살아있음)
            // 이 조건은 has판정승Rule의 정의를 어떻게 할지에 따라 달라짐. 현재는 몸통도 파괴되어야 함.
            // 만약 몸통 제외 모든 부위 파괴시 + 몸통 생존 시 판정승이라면 아래와 같이 수정:
            // if (allNonCorePartsDestroyed && corePart != null && !corePart.isDestroyed)
            // { Die(string.Format("{0}의 몸통 제외 모든 부위가 파괴되어 판정승으로 사망했습니다.", enemyName)); return; }
        }
        
        // 모든 부위가 파괴되었는지도 확인 (핵심 부위나 판정승 규칙이 없는 경우)
        bool allPartsDestroyed = parts.All(p => p.isDestroyed);
        if (allPartsDestroyed && parts.Count > 0) // 부위가 하나라도 있어야 함
        {
            Die(string.Format("{0}의 모든 부위가 파괴되어 사망했습니다.", enemyName));
            return;
        }
    }

    void Die(string reason)
    {
        isDead = true;
        Debug.Log(reason);
        // TODO: 전투 종료 및 보상 로직 호출, CombatManager에 알림
        // gameObject.SetActive(false); // 간단하게 비활성화
    }

    public string GetStatusRepresentation()
    {
        if (isDead)
        {
            return string.Format("적: {0} (쓰러짐)", enemyName);
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendFormat("적: {0} (전체 방어력: {1})\n", enemyName, currentBlock);
        // sb.AppendFormat("  전체 체력: {0}/{1} (이 부분은 부위별로 대체됨)\n", currentHealth, maxHealth);

        foreach (EnemyPart part in parts)
        {
            sb.AppendFormat("  - {0}\n", part.GetStatusRepresentation());
        }

        if (nextAction != null)
        {
            sb.AppendFormat("다음 행동: {0} (값: {1})", nextAction.actionType, nextAction.value);
            // TODO: 이 행동이 어느 부위와 연결되어 있는지 표시
        }
        return sb.ToString();
    }

    // 현재 타겟팅 가능한 (파괴되지 않은) 부위 리스트를 반환합니다.
    public List<EnemyPart> GetTargetableParts()
    {
        if (isDead) return new List<EnemyPart>(); // 죽었으면 빈 리스트 반환

        List<EnemyPart> targetableParts = new List<EnemyPart>();
        foreach (EnemyPart part in parts)
        {
            if (!part.isDestroyed)
            {
                targetableParts.Add(part);
            }
        }
        return targetableParts;
    }
} 