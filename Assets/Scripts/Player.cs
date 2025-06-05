using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Shuffle을 위해 사용합니다.

public class Player : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int currentBlock = 0; // 현재 방어력 변수 추가

    public int maxEnergy = 3;
    public int currentEnergy;

    public List<CardData> deck = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public List<CardData> drawPile = new List<CardData>();

    void Awake()
    {
        // 테스트용 기본 덱 설정
        InitializeDeck();
    }

    void Start()
    {
        currentHealth = maxHealth;
        // StartCombat 등에서 호출될 수 있지만, 초기 테스트를 위해 여기서 호출
        // StartTurn(); 
    }

    void InitializeDeck()
    {
        // 예시 카드 추가 (나중에는 카드 데이터베이스나 ScriptableObject에서 로드할 수 있습니다)
        deck.Add(new CardData("타격", 1, "적에게 6의 피해를 줍니다.", CardEffectType.Attack, 6));
        deck.Add(new CardData("타격", 1, "적에게 6의 피해를 줍니다.", CardEffectType.Attack, 6));
        deck.Add(new CardData("타격", 1, "적에게 6의 피해를 줍니다.", CardEffectType.Attack, 6));
        deck.Add(new CardData("수비", 1, "5의 방어도를 얻습니다.", CardEffectType.Defend, 5));
        deck.Add(new CardData("수비", 1, "5의 방어도를 얻습니다.", CardEffectType.Defend, 5));

        ShuffleDeck();
    }

    public void ShuffleDeck()
    {
        drawPile = deck.OrderBy(a => Random.Range(0f, 1f)).ToList();
        Debug.Log("덱을 섞었습니다. 뽑을 카드 수: " + drawPile.Count);
    }

    public void StartTurn(int drawCount = 5)
    {
        currentEnergy = maxEnergy;
        currentBlock = 0; // 턴 시작 시 방어력 초기화
        DrawCards(drawCount);
        Debug.Log(string.Format("플레이어 턴 시작. 에너지: {0}, 방어력: {1}, 핸드 카드 수: {2}", currentEnergy, currentBlock, hand.Count));
        PrintHand();
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.Log("뽑을 카드도, 버린 카드 더미도 비었습니다.");
                    break; // 더 이상 뽑을 카드가 없음
                }
                RecycleDiscardPile();
            }

            if (drawPile.Count > 0) // Recycle 후 다시 확인
            {
                CardData cardToDraw = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(cardToDraw);
                Debug.Log(string.Format("카드 뽑음: {0}", cardToDraw.cardName));
            }
        }
    }

    void RecycleDiscardPile()
    {
        Debug.Log("버린 카드 더미를 가져와 섞습니다.");
        drawPile = discardPile.OrderBy(a => Random.Range(0f, 1f)).ToList();
        discardPile.Clear();
    }

    // cardIndex는 핸드에서의 인덱스를 의미합니다.
    // targetPart가 null이면 Enemy의 기본 TakeDamage 로직(주로 몸통)을 따릅니다. -> targetEnemyPartIndex로 변경
    public bool PlayCard(int handCardIndex, Enemy targetEnemy, int? targetEnemyPartIndex = null) // EnemyPart targetPart = null -> int? targetEnemyPartIndex = null
    {
        if (handCardIndex < 0 || handCardIndex >= hand.Count)
        {
            Debug.LogError("잘못된 카드 인덱스입니다.");
            return false; // 실패 반환
        }

        CardData cardToPlay = hand[handCardIndex];

        if (currentEnergy >= cardToPlay.cost)
        {
            currentEnergy -= cardToPlay.cost;
            hand.RemoveAt(handCardIndex);
            discardPile.Add(cardToPlay);

            Debug.Log(string.Format("플레이어 카드 사용: {0}, 남은 에너지: {1}", cardToPlay.ToString(), currentEnergy));
            ApplyCardEffect(cardToPlay, targetEnemy, targetEnemyPartIndex); // targetPart -> targetEnemyPartIndex
            return true; // 성공 반환
        }
        else
        { 
            Debug.Log(string.Format("{0} 사용 실패: 에너지가 부족합니다. (필요: {1}, 현재: {2})", cardToPlay.cardName, cardToPlay.cost, currentEnergy));
            return false; // 에너지 부족으로 실패 반환
        }
    }

    // targetPart가 null이면 Enemy의 기본 TakeDamage 로직을 따르도록 하거나, 특정 기본 부위를 공격합니다. -> targetEnemyPartIndex로 변경
    void ApplyCardEffect(CardData card, Enemy targetEnemy, int? targetEnemyPartIndex = null) // EnemyPart targetPart = null -> int? targetEnemyPartIndex = null
    {
        switch (card.effectType)
        {
            case CardEffectType.Attack:
                if (targetEnemy != null && !targetEnemy.isDead) // 적이 살아있는지 확인
                {
                    if (targetEnemyPartIndex.HasValue) // 특정 부위 인덱스가 타겟으로 지정되었고
                    {
                        // Enemy.cs의 TakeDamageToPart(int partIndex, int amount) 호출
                        Debug.Log(string.Format("{0}의 부위 인덱스 '{1}'에게 {2} 피해를 입힙니다.", targetEnemy.enemyName, targetEnemyPartIndex.Value, card.effectValue));
                        targetEnemy.TakeDamageToPart(targetEnemyPartIndex.Value, card.effectValue);
                    }
                    else // 특정 부위 인덱스가 지정되지 않은 경우 (공격 카드지만, CombatManager에서 여기까지 오면 안됨)
                    {
                        Debug.LogWarning(string.Format("공격 카드 '{0}'에 대한 타겟 부위 인덱스가 지정되지 않았습니다. 공격이 적용되지 않습니다.", card.cardName));
                        // 또는, 기존처럼 Enemy의 기본 TakeDamage(amount)를 호출할 수도 있지만, 현재 Enemy.cs에는 해당 메서드가 명확한 부위 타겟팅 없이 존재하지 않을 수 있음.
                        // targetEnemy.TakeDamage(card.effectValue); // 이 줄은 Enemy.cs의 상황에 따라 주석 처리하거나 수정 필요
                    }
                }
                else
                { 
                    if (targetEnemy != null && targetEnemy.isDead) Debug.LogWarning("공격 대상 적이 이미 쓰러져 있습니다.");
                    else Debug.LogWarning("공격 대상 적이 없습니다.");
                }
                break;
            case CardEffectType.Defend:
                currentBlock += card.effectValue; // 방어력 증가
                Debug.Log(string.Format("{0}의 방어도를 얻습니다. 현재 방어력: {1}", card.effectValue, currentBlock));
                break;
            case CardEffectType.Draw:
                Debug.Log(string.Format("카드 {0}장을 뽑습니다.", card.effectValue));
                DrawCards(card.effectValue);
                break;
            // TODO: Buff, Debuff 효과 구현
            default:
                Debug.LogWarning(string.Format("알 수 없는 카드 효과 유형: {0}", card.effectType));
                break;
        }
    }

    public void EndTurn()
    {
        // 남은 핸드 카드를 버린 카드 더미로 옮깁니다.
        discardPile.AddRange(hand);
        hand.Clear();
        Debug.Log("플레이어 턴 종료. 남은 핸드 카드를 버립니다.");
    }

    public void TakeDamage(int amount)
    {
        int damageToTake = amount;

        if (currentBlock > 0)
        {
            if (currentBlock >= damageToTake)
            {
                currentBlock -= damageToTake;
                Debug.Log(string.Format("방어력으로 피해 {0} 흡수. 남은 방어력: {1}", damageToTake, currentBlock));
                damageToTake = 0;
            }
            else
            {
                damageToTake -= currentBlock;
                Debug.Log(string.Format("방어력 {0} 모두 소모. 남은 피해 {1}", currentBlock, damageToTake));
                currentBlock = 0;
            }
        }

        if (damageToTake > 0)
        {
            currentHealth -= damageToTake;
            Debug.Log(string.Format("플레이어가 {0}의 피해를 입었습니다. 현재 체력: {1}/{2}", damageToTake, currentHealth, maxHealth));
        }
        else
        {
            Debug.Log("모든 피해를 방어력으로 막았습니다.");
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("플레이어가 패배했습니다...");
            // TODO: 게임 오버 로직
        }
    }

    public void PrintHand()
    {
        if (hand.Count == 0)
        {
            Debug.Log("핸드에 카드가 없습니다.");
            return;
        }
        string handString = "현재 핸드:\n";
        for (int i = 0; i < hand.Count; i++)
        {
            handString += string.Format("[{0}] {1}\n", i, hand[i].ToString());
        }
        Debug.Log(handString);
    }

    public string GetHandRepresentation()
    {
        // 현재 체력, 에너지, 방어력 정보를 먼저 추가합니다.
        string playerStatus = string.Format("플레이어 정보\n체력: {0}/{1} (방어력: {2})\n에너지: {3}/{4}\n\n", 
                                        currentHealth, maxHealth, currentBlock, currentEnergy, maxEnergy);

        System.Text.StringBuilder sb = new System.Text.StringBuilder(playerStatus);

        if (hand.Count == 0)
        {
            sb.Append("플레이어 핸드: 비어있음");
        }
        else
        {
            sb.Append("플레이어 핸드:\n");
            for (int i = 0; i < hand.Count; i++)
            {
                // 키보드 입력(1~5)과 일치하도록 1부터 시작하는 인덱스로 표시합니다.
                sb.AppendFormat("[{0}] {1} (비용: {2})\n", i + 1, hand[i].cardName, hand[i].cost);
            }
        }
        return sb.ToString();
    }

    // 전체 덱 목록을 반환하는 메서드 (CardData 리스트)
    public List<CardData> GetFullDeckListReference()
    {
        return deck; // deck 변수가 플레이어의 전체 초기 덱을 나타낸다고 가정합니다.
    }

    // 전체 덱 목록을 보기 좋은 문자열로 반환하는 메서드
    public string GetDeckListAsString()
    {
        if (deck == null || deck.Count == 0)
        {
            return "덱에 카드가 없습니다.";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder("전체 덱 목록:\n");
        // 덱을 종류별로 묶어서 보여주거나, 이름 순으로 정렬할 수도 있습니다.
        // 여기서는 간단히 리스트 순서대로 보여줍니다.
        // Dictionary를 사용해 카드 이름과 개수를 세는 것도 좋은 방법입니다.
        Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        foreach (CardData card in deck)
        {
            if (cardCounts.ContainsKey(card.cardName))
            {
                cardCounts[card.cardName]++;
            }
            else
            {
                cardCounts[card.cardName] = 1;
            }
        }

        foreach (KeyValuePair<string, int> entry in cardCounts)
        {
            // 예시: 타격 x3 (비용: 1)
            // 카드의 비용을 가져오기 위해 원본 덱에서 해당 이름의 첫 번째 카드를 찾습니다. (모든 타격 카드는 비용이 같다고 가정)
            CardData sampleCard = deck.Find(c => c.cardName == entry.Key);
            int cost = (sampleCard != null) ? sampleCard.cost : 0;
            sb.AppendFormat("{0} x{1} (비용: {2})\n", entry.Key, entry.Value, cost);
        }

        return sb.ToString();
    }
} 