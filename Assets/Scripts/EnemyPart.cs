using UnityEngine;

[System.Serializable] // Enemy 클래스에서 리스트 형태로 인스펙터에 노출시키기 위함
public class EnemyPart
{
    public string partName;
    public int maxHealth;
    public int currentHealth;
    public bool isDestroyed = false;
    public bool isCorePart = false; // 이 부위가 파괴되면 몬스터가 즉시 사망하는지 여부

    // 생성자 (선택 사항, Enemy 스크립트에서 직접 초기화할 수도 있음)
    public EnemyPart(string name, int health, bool isCore = false)
    {
        partName = name;
        maxHealth = health;
        currentHealth = maxHealth;
        isDestroyed = false;
        isCorePart = isCore;
    }

    // 부위가 피해를 받는 로직
    public void TakeDamage(int amount)
    {
        if (isDestroyed)
        {
            Debug.Log(string.Format("{0} 부위는 이미 파괴되었습니다.", partName));
            return;
        }

        currentHealth -= amount;
        Debug.Log(string.Format("{0} 부위가 {1}의 피해를 입었습니다. 현재 체력: {2}/{3}", partName, amount, currentHealth, maxHealth));

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDestroyed = true;
            Debug.Log(string.Format("{0} 부위가 파괴되었습니다!", partName));
            // 여기에 부위 파괴 시 특별한 효과나 이벤트 호출 로직 추가 가능
        }
    }

    // 부위가 행동을 수행할 수 있는지 여부
    public bool CanPerformActions()
    {
        return !isDestroyed;
    }

    // 부위 상태를 문자열로 반환 (디버깅 및 UI용)
    public string GetStatusRepresentation()
    {
        string status = string.Format("{0}: {1}/{2}", partName, currentHealth, maxHealth);
        if (isDestroyed)
        {
            status += " (파괴됨)";
        }
        return status;
    }
} 