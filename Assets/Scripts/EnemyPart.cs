using System.Collections.Generic;
using UnityEngine;

public enum EnemyIntentType
{
    Attack,
    Defend,
    Buff,
    Debuff
    // 필요에 따라 추가 의도 유형 정의
}

[System.Serializable]
public class EnemyIntent
{
    public EnemyIntentType intentType;
    public int value; // 공격력, 방어력, 버프/디버프 수치 등
    public string description; // 예: "10의 피해를 입힙니다", "5의 방어도를 얻습니다"

    public EnemyIntent(EnemyIntentType type, int val, string desc)
    {
        intentType = type;
        value = val;
        description = desc;
    }

    public string GetDescription()
    {
        return description;
    }
}

public class EnemyPart : MonoBehaviour
{
    public string partName;
    public int currentHealth;
    public int maxHealth;
    public int currentBlock;
    public bool isDestroyed = false;
    public bool isCorePart = false; // 이 부위가 코어(본체)인지 여부

    public EnemyIntent currentIntent;
    public List<EnemyIntent> possibleIntents = new List<EnemyIntent>();

    public void Setup(string name, int maxHp, List<EnemyIntent> intents, bool isCore = false)
    {
        partName = name;
        maxHealth = maxHp;
        currentHealth = maxHealth;
        currentBlock = 0;
        isDestroyed = false;
        possibleIntents = intents ?? new List<EnemyIntent>();
        isCorePart = isCore;
        if (isCorePart)
        {
            // 코어 파츠는 행동을 가지지 않음 (기획 의도 반영)
            possibleIntents.Clear();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;

        int damageToHealth = amount;
        if (currentBlock > 0)
        {
            if (currentBlock >= amount)
            {
                currentBlock -= amount;
                damageToHealth = 0;
                Debug.Log(string.Format("{0} 부위가 {1}의 피해를 방어력으로 흡수. 남은 방어력: {2}", partName, amount, currentBlock));
            }
            else
            {
                damageToHealth -= currentBlock;
                currentBlock = 0;
                Debug.Log(string.Format("{0} 부위가 방어력을 모두 소모하고 {1}의 피해를 받음.", partName, damageToHealth));
            }
        }

        if (damageToHealth > 0)
        {
            currentHealth -= damageToHealth;
            Debug.Log(string.Format("{0} 부위가 {1}의 피해를 입음. 현재 체력: {2}/{3}", partName, damageToHealth, currentHealth, maxHealth));
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDestroyed = true;
            currentIntent = null; // 파괴 시 행동 취소
            Debug.Log(string.Format("{0} 부위가 파괴되었습니다!", partName));
            // 여기에 부위 파괴 시 추가 효과 (예: 이벤트 발생)를 넣을 수 있습니다.
        }
    }

    public void ChooseNextIntent()
    {
        if (isDestroyed || isCorePart || possibleIntents == null || possibleIntents.Count == 0)
        {
            currentIntent = null;
            return;
        }
        currentIntent = possibleIntents[Random.Range(0, possibleIntents.Count)];
        Debug.Log(string.Format("{0} 부위 다음 행동: {1}", partName, currentIntent != null ? currentIntent.GetDescription() : "없음"));
    }
    
    public string GetIntentDescription()
    {
        if (isDestroyed) return "파괴됨";
        if (currentIntent == null) return isCorePart ? "코어" : "행동 없음";
        return currentIntent.GetDescription();
    }

    public void AddBlock(int amount)
    {
        if (isDestroyed) return;
        currentBlock += amount;
        Debug.Log(string.Format("{0} 부위가 {1}의 방어력을 얻음. 현재 방어력: {2}", partName, amount, currentBlock));
    }

    public void ResetBlock()
    {
        if (currentBlock > 0)
        {
            Debug.Log(string.Format("{0} 부위의 방어력이 초기화됩니다. 이전 방어력: {1}", partName, currentBlock));
            currentBlock = 0;
        }
    }
} 