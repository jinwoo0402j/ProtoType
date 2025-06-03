using UnityEngine;
// using UnityEngine.UI; // HandDisplay 스크립트를 통하므로 CombatManager에서 직접 UI 네임스페이스가 필요 없을 수 있습니다.
using System.Collections.Generic; // List 사용을 위해 추가
using System.Text; // StringBuilder 사용을 위해 추가

public class CombatManager : MonoBehaviour
{
    public Player player;
    public Enemy enemy;
    private HandDisplay handDisplay; // HandDisplay 스크립트 참조
    private EnemyStatusDisplay enemyStatusDisplay; // EnemyStatusDisplay 스크립트 참조
    private GameMessageDisplay gameMessageDisplay; // GameMessageDisplay 스크립트 참조
    private DeckViewUI deckViewUI; // DeckViewUI 스크립트 참조

    public enum CombatState { PlayerTurn, EnemyTurn, Victory, Defeat, Paused, DeckViewing, SelectingTarget }
    public CombatState currentState;

    private CombatState previousCombatState; // 덱 뷰를 열기 전 상태를 저장하기 위한 변수

    // 타겟 선택 관련 변수
    private int selectedCardIndexToPlay = -1;
    private CardData selectedCardData = null; 
    private List<EnemyPart> currentTargetableParts = new List<EnemyPart>();

    void Start()
    {
        // Player 객체를 씬에서 찾아서 할당합니다.
        player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogError("CombatManager: Player 객체를 씬에서 찾을 수 없습니다. Player가 CombatManager에 할당되지 않았습니다.");
            // player가 없으면 게임 진행이 거의 불가능하므로, 여기서 추가적인 오류 처리를 하거나 게임을 멈출 수 있습니다.
            // 예를 들어, enabled = false; 를 호출하여 이 컴포넌트의 Update 메서드 실행을 막을 수 있습니다.
        }

        // Enemy 객체는 여전히 인스펙터에서 할당해야 할 수 있습니다 (씬에 여러 Enemy가 있을 수 있으므로).
        // 만약 Enemy도 하나만 존재한다면 player와 유사하게 FindObjectOfType으로 찾을 수 있습니다.
        if (enemy == null)
        {
            Debug.LogError("CombatManager: Enemy가 인스펙터에서 할당되지 않았습니다.");
        }

        // HandDisplay 컴포넌트를 씬에서 찾습니다.
        handDisplay = FindObjectOfType<HandDisplay>();
        if (handDisplay == null)
        {
            Debug.LogWarning("CombatManager: HandDisplay UI object not found in scene. Player hand will not be shown on screen.");
        }

        enemyStatusDisplay = FindObjectOfType<EnemyStatusDisplay>(); // EnemyStatusDisplay 찾기
        if (enemyStatusDisplay == null)
        {
            Debug.LogWarning("CombatManager: EnemyStatusDisplay UI object not found in scene. Enemy status will not be shown on screen.");
        }

        gameMessageDisplay = FindObjectOfType<GameMessageDisplay>(); // GameMessageDisplay 찾기
        if (gameMessageDisplay == null)
        {
            Debug.LogWarning("CombatManager: GameMessageDisplay UI object not found in scene. Game messages will not be shown.");
        }
        else
        {
            gameMessageDisplay.ClearMessage(); // 시작 시 메시지 클리어
        }

        deckViewUI = FindObjectOfType<DeckViewUI>(true); // 비활성화된 오브젝트도 포함해서 찾습니다.
        if (deckViewUI == null)
        {
            Debug.LogWarning("CombatManager: DeckViewUI 컴포넌트를 가진 오브젝트를 씬에서 전혀 찾지 못했습니다 (비활성화된 오브젝트 포함). Deck view will not be available.");
        }
        else
        {
            Debug.Log(string.Format("CombatManager: DeckViewUI 오브젝트 '{0}'를 찾았습니다. 활성화 상태(activeInHierarchy): {1}, 직접 활성화 상태(activeSelf): {2}", deckViewUI.gameObject.name, deckViewUI.gameObject.activeInHierarchy, deckViewUI.gameObject.activeSelf));
            deckViewUI.Hide(); // 시작 시 덱 뷰는 숨김
        }

        // Unity 에디터에서 Player와 Enemy 객체를 할당해야 합니다.
        if (player == null || enemy == null)
        {
            Debug.LogError("Player 또는 Enemy가 CombatManager에 설정되지 않았습니다. 전투를 시작할 수 없습니다.");
            return; // player나 enemy가 없으면 StartCombat() 호출하지 않음
        }
        StartCombat();
        UpdatePlayerHandUI(); // 전투 시작 시 (보통 핸드가 비어있거나 초기 상태) UI 업데이트
        UpdateEnemyStatusUI(); // 전투 시작 시 적 상태 UI 업데이트
    }

    void StartCombat()
    {
        Debug.Log("전투 시작!");
        if (gameMessageDisplay != null) gameMessageDisplay.ClearMessage(); // 새 전투 시작 시 이전 메시지 클리어
        currentState = CombatState.PlayerTurn; // 초기 상태 설정
        previousCombatState = currentState; // 초기 이전 상태도 설정
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        currentState = CombatState.PlayerTurn;
        player.StartTurn(); // 에너지 회복, 카드 뽑기
        Debug.Log("플레이어 턴입니다. 카드를 선택하세요.");
        UpdatePlayerHandUI(); // 핸드 UI 업데이트
        UpdateEnemyStatusUI(); // 플레이어 턴 시작 시 적 상태(의도 등) UI 업데이트
    }

    void StartTargetSelection(int cardIndexInHand)
    {
        if (player == null || enemy == null || enemy.isDead || cardIndexInHand < 0 || cardIndexInHand >= player.hand.Count)
        {
            Debug.LogWarning("타겟 선택 시작 불가: 플레이어, 적 또는 카드 정보가 유효하지 않습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("카드 사용 불가", 1.5f);
            return;
        }

        selectedCardData = player.hand[cardIndexInHand];
        selectedCardIndexToPlay = cardIndexInHand;

        // 공격 카드이고, 적이 살아있을 때만 부위 선택
        if (selectedCardData.effectType == CardEffectType.Attack)
        {
            currentTargetableParts = enemy.GetTargetableParts();
            if (currentTargetableParts.Count == 0)
            {
                Debug.LogWarning(string.Format("{0}에게 타겟 가능한 부위가 없습니다.", enemy.enemyName));
                if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("타겟 부위 없음!", 2f);
                // player.PlayCard(selectedCardIndexToPlay, enemy, null); // 타겟 없이 사용 시도 (기본 타겟팅)
                // 또는 여기서 카드 사용 자체를 막을 수도 있습니다.
                ClearTargetSelectionState(); // 상태 초기화
                return;
            }

            previousCombatState = currentState; // PlayerTurn 상태 저장
            currentState = CombatState.SelectingTarget;
            
            StringBuilder message = new StringBuilder("타겟 부위 선택 (Esc: 취소):\n");
            for (int i = 0; i < currentTargetableParts.Count; i++)
            {
                EnemyPart part = currentTargetableParts[i];
                message.AppendFormat("[{0}] {1} ({2}/{3})\n", i + 1, part.partName, part.currentHealth, part.maxHealth);
            }
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage(message.ToString(), 0); // 계속 표시
            Debug.Log(message.ToString());
        }
        else // 공격 카드가 아니면 부위 선택 없이 바로 사용 (예: 방어 카드)
        { 
            Debug.Log(string.Format("{0} 카드는 부위 선택 없이 바로 사용됩니다.", selectedCardData.cardName));
            bool played = player.PlayCard(selectedCardIndexToPlay, enemy, null); // 타겟 부위 없이 카드 사용
            if (!played && selectedCardData.cost > player.currentEnergy) // PlayCard가 false를 반환하는 주된 이유는 에너지 부족
            {
                if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("에너지가 부족합니다!", 2f);
            }
            UpdatePlayerHandUI();
            UpdateEnemyStatusUI();
            ClearTargetSelectionState(); // 사용 후 상태 초기화
        }
    }

    void CancelTargetSelection()
    {
        if (currentState == CombatState.SelectingTarget)
        {
            Debug.Log("타겟 선택이 취소되었습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ClearMessage();
            currentState = CombatState.PlayerTurn; // 또는 previousCombatState
            ClearTargetSelectionState();
        }
    }

    void ClearTargetSelectionState()
    {
        selectedCardIndexToPlay = -1;
        selectedCardData = null;
        currentTargetableParts.Clear();
    }

    // 이 메서드는 UI나 테스트 코드에서 호출될 것입니다.
    public void PlayerEndTurn()
    {
        if (currentState != CombatState.PlayerTurn)
        {
            Debug.LogWarning("플레이어 턴이 아닙니다.");
            return;
        }
        player.EndTurn(); // 남은 카드 버리기 등
        Debug.Log("플레이어가 턴을 마쳤습니다.");
        UpdatePlayerHandUI(); // 턴 종료 후 (핸드가 비워짐) UI 업데이트
        previousCombatState = currentState; // 상태 변경 전 저장
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        currentState = CombatState.EnemyTurn;
        Debug.Log("적의 턴입니다.");
        UpdateEnemyStatusUI(); // 적 턴 시작 직전 (주로 의도 확인) UI 업데이트

        EnemyIntent intent = enemy.GetNextAction(); // 이 줄은 사실 GetStatusRepresentation에 포함되므로 없어도 될 수 있음
        Debug.Log(string.Format("적의 의도: {0}", intent.ToString()));

        enemy.PerformAction(player);
        UpdateEnemyStatusUI(); // 적 행동 후 (체력 변경 등) UI 업데이트

        // 플레이어가 죽었는지 확인
        if (player.currentHealth <= 0)
        {
            currentState = CombatState.Defeat;
            previousCombatState = currentState; // 상태 변경 시 이전 상태도 업데이트
            Debug.Log("패배... 플레이어가 쓰러졌습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("패배...", 0); // 0은 계속 표시
            UpdatePlayerHandUI();
            UpdateEnemyStatusUI();
            // TODO: 게임 오버 로직, 입력 비활성화 등
            return;
        }

        Debug.Log("적이 턴을 마쳤습니다.");
        previousCombatState = currentState; // 상태 변경 전 저장
        StartPlayerTurn(); // 다시 플레이어 턴 시작
    }

    private void UpdatePlayerHandUI()
    {
        if (player != null && handDisplay != null)
        {
            handDisplay.ShowHand(player.GetHandRepresentation());
        }
    }

    private void UpdateEnemyStatusUI() // 새로운 메서드
    {
        if (enemy != null && enemyStatusDisplay != null)
        {
            enemyStatusDisplay.UpdateStatus(enemy.GetStatusRepresentation());
        }
    }

    void Update() // 키보드 입력 처리
    {
        if (currentState == CombatState.Victory || currentState == CombatState.Defeat || currentState == CombatState.DeckViewing)
        {
            if (currentState == CombatState.DeckViewing && Input.GetKeyDown(KeyCode.Escape))
            {
                OnViewDeckButtonPressed(); 
            }
            return; 
        }

        if (currentState == CombatState.PlayerTurn)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("E 키 입력: 턴 종료 시도");
                PlayerEndTurn();
            }

            for (int i = 0; i < 5; i++) 
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (i < player.hand.Count) 
                    {
                         Debug.Log(string.Format("{0}번 키 입력: 핸드의 {0}번째 카드 사용 시도", i + 1));
                         StartTargetSelection(i); 
                         break; 
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("핸드에 {0}번째 카드가 없습니다.", i + 1));
                    }
                }
            }
        }
        else if (currentState == CombatState.SelectingTarget)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                return; // 추가 입력 처리 방지
            }
            if (Input.GetKeyDown(KeyCode.E)) // 턴 종료 키로도 타겟 선택 취소
            {
                CancelTargetSelection();
                PlayerEndTurn(); // 그리고 턴 종료
                return;
            }

            for (int i = 0; i < currentTargetableParts.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    EnemyPart selectedPart = currentTargetableParts[i];
                    Debug.Log(string.Format("부위 '{0}' 선택됨. 카드 사용 실행.", selectedPart.partName));
                    
                    if (gameMessageDisplay != null) gameMessageDisplay.ClearMessage(); // 타겟 선택 메시지 제거
                    
                    bool playedSuccessfully = player.PlayCard(selectedCardIndexToPlay, enemy, selectedPart);
                    UpdatePlayerHandUI();
                    UpdateEnemyStatusUI();

                    if (!playedSuccessfully && selectedCardData != null && selectedCardData.cost > player.currentEnergy) // 실패 주 원인이 에너지 부족일 경우
                    {
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("에너지가 부족합니다!", 2f);
                    }
                    else if(!playedSuccessfully) // 기타 이유로 실패 (예: 카드가 null, targetEnemy가 null 등 Player.PlayCard 내부에서 처리)
                    {
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("카드 사용 실패!", 2f);
                    }

                    // 승리/패배 조건 확인은 PlayCard 이후에 항상 수행
                    if (enemy != null && enemy.isDead)
                    {
                        currentState = CombatState.Victory;
                        Debug.Log("승리! 적을 물리쳤습니다.");
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("승리!", 0); 
                        // previousCombatState = currentState; // 더 이상 SelectingTarget으로 돌아갈 필요 없음
                    }
                    // 플레이어 패배 조건은 Enemy 턴 이후에 주로 확인되지만, 여기서도 확인할 수 있음
                    else if (player != null && player.currentHealth <= 0) 
                    { 
                        currentState = CombatState.Defeat; 
                        Debug.Log("패배... 플레이어가 쓰러졌습니다."); 
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("패배...", 0); 
                    }
                    else
                    {
                        currentState = CombatState.PlayerTurn; // 행동 후 플레이어 턴으로 복귀
                    }
                    
                    ClearTargetSelectionState();
                    break; // 하나의 타겟 선택 후 루프 종료
                }
            }
        }
    }

    // 테스트를 위한 간단한 진행 예시 (Unity 에디터에서 버튼 등에 연결하거나, 직접 호출)
    // 예: 첫 번째 카드를 사용하고 턴을 종료하는 함수
    public void Test_PlayFirstCardAndEndTurn()
    {
        if (currentState == CombatState.PlayerTurn && player.hand.Count > 0)
        {
            StartTargetSelection(0); // 핸드의 첫 번째 카드로 타겟 선택 시작
            if (currentState == CombatState.PlayerTurn) // 카드를 사용해도 턴이 유지될 수 있음 (승리하지 않았다면)
            {
                PlayerEndTurn();
            }
        }
        else if (currentState == CombatState.PlayerTurn && player.hand.Count == 0)
        {
             Debug.Log("사용할 카드가 없습니다. 턴을 종료합니다.");
             PlayerEndTurn();
        }
    }

    // "덱 보기" 버튼에 연결될 메서드
    public void OnViewDeckButtonPressed()
    {
        if (player == null)
        {
            Debug.LogError("CombatManager: Player가 설정되지 않아서 덱 정보를 가져올 수 없습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("Player 정보 없음", 2f);
            return;
        }

        if (deckViewUI == null)
        {
            Debug.LogError("CombatManager: DeckViewUI가 설정되지 않아서 덱 뷰를 표시할 수 없습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("DeckView UI 없음", 2f);
            return;
        }

        // 승리 또는 패배 상태에서는 덱 뷰를 열지 않도록 추가적인 방어 코드
        if (currentState == CombatState.Victory || currentState == CombatState.Defeat)
        {
            Debug.LogWarning("승리 또는 패배 상태에서는 덱 뷰를 열 수 없습니다.");
            return;
        }

        if (currentState == CombatState.DeckViewing)
        {
            // 덱 뷰를 닫는 경우
            deckViewUI.Hide();
            currentState = previousCombatState; // 이전 전투 상태로 복귀
            Debug.Log(string.Format("덱 뷰를 닫습니다. 이전 상태({0})로 돌아갑니다.", currentState));
        }
        else
        {
            // 덱 뷰를 여는 경우
            string deckList = player.GetDeckListAsString();
            if (string.IsNullOrEmpty(deckList))
            {
                 Debug.LogWarning("Player의 덱 목록이 비어있거나 가져올 수 없습니다.");
                 // deckViewUI.ShowDeck(new System.Collections.Generic.List<CardData>()); // 빈 목록으로 표시하거나
                 deckViewUI.DisplayDeck("덱 정보 없음"); // 또는 메시지 표시
            }
            else
            {
                deckViewUI.DisplayDeck(deckList);
            }
            previousCombatState = currentState; // 현재 상태를 저장
            currentState = CombatState.DeckViewing; // 상태를 덱 뷰잉으로 변경
            Debug.Log("덱 뷰를 엽니다. 현재 전투 상태를 일시 중지합니다.");
        }
        // 덱 뷰 상태에 따라 다른 UI 요소들의 업데이트가 필요하다면 여기서 호출
        UpdatePlayerHandUI(); 
        UpdateEnemyStatusUI();
    }
} 