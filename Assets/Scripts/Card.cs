using UnityEngine;

// 카드 효과 유형을 정의하는 열거형입니다.
public enum CardEffectType
{
    Attack,
    Defend,
    Buff,
    Debuff,
    Draw
}

[System.Serializable] // 이 클래스의 필드들이 Unity 인스펙터에 표시되도록 합니다.
public class CardData
{
    public string cardName;
    public int cost;
    public string description;
    public CardEffectType effectType;
    public int effectValue;
    public Sprite cardImage; // Unity 에디터에서 카드 이미지를 할당할 수 있도록 합니다.

    // 생성자
    public CardData(string name, int cost, string desc, CardEffectType type, int value, Sprite image = null)
    {
        this.cardName = name;
        this.cost = cost;
        this.description = desc;
        this.effectType = type;
        this.effectValue = value;
        this.cardImage = image;
    }

    // 텍스트 기반으로 카드 정보를 출력하기 위한 메서드 (추후 Debug.Log 등으로 활용)
    public override string ToString()
    {
        return string.Format("[{0}] 비용:{1} - {2} (효과: {3}, 값: {4})", cardName, cost, description, effectType, effectValue);
    }
}

// 실제 게임에서 카드 개체를 다루기 위한 MonoBehaviour. 지금은 비워두고 필요시 확장합니다.
// 예를 들어, 카드 클릭 이벤트나 UI 업데이트 등을 처리할 수 있습니다.
public class Card : MonoBehaviour
{
    public CardData data;

    public void Initialize(CardData cardData)
    {
        this.data = cardData;
        // 추후: 카드 UI 업데이트 로직 (이름, 비용, 설명, 이미지 등)
        // gameObject.name = data.cardName; // Unity 에디터에서 쉽게 식별하기 위함
    }

    // 카드를 플레이하는 기본 로직 (텍스트 기반)
    // 실제 효과 적용은 Player나 Enemy, CombatManager에서 이루어집니다.
    public void Play()
    {
        Debug.Log(string.Format("카드 사용: {0}", data.ToString()));
        // 이 카드가 사용되었다는 것을 CombatManager나 Player에게 알리는 로직이 필요합니다.
    }
} 