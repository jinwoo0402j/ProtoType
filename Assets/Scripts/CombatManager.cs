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
            ClearTargetSelectionState(); // 상태 초기화 추가
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
                Debug.LogWarning(string.Format("{0}에게 타겟 가능한 부위가 없습니다. 공격할 수 없습니다.", enemy.enemyName));
                if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("타겟 부위 없음!", 2f);
                ClearTargetSelectionState(); // 상태 초기화
                return;
            }

            previousCombatState = currentState; // PlayerTurn 상태 저장
            currentState = CombatState.SelectingTarget;
            
            StringBuilder message = new StringBuilder("타겟 부위 선택 (Esc: 취소):\n");
            for (int i = 0; i < currentTargetableParts.Count; i++)
            {
                EnemyPart part = currentTargetableParts[i];
                // 사용자가 입력할 숫자 (1부터 시작)와 부위 정보를 함께 표시
                message.AppendFormat("[{0}] {1} ({2}/{3}) {4} {5}\n", 
                                    i + 1, 
                                    part.partName, 
                                    part.currentHealth, 
                                    part.maxHealth, 
                                    part.isCorePart ? "(코어)" : "",
                                    part.currentBlock > 0 ? string.Format("(방어: {0})", part.currentBlock) : "");
            }
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage(message.ToString(), 0); // 계속 표시
            Debug.Log(message.ToString());
        }
        else // 공격 카드가 아니면 부위 선택 없이 바로 사용 (예: 방어, 드로우 카드)
        { 
            Debug.Log(string.Format("{0} 카드는 부위 선택 없이 바로 사용됩니다.", selectedCardData.cardName));
            // targetEnemyPartIndex를 null로 전달
            bool played = player.PlayCard(selectedCardIndexToPlay, enemy, null); 
            if (played)
            {
                UpdatePlayerHandUI();
                UpdateEnemyStatusUI(); // 적에게 직접 영향은 없지만, 만약의 경우를 대비
                 // 비공격 카드로 적을 죽일 수 있는 경우가 있다면 (예: 반사 데미지 버프 후 적이 자해하는 카드)
                if (enemy.IsDefeated())
                {
                    currentState = CombatState.Victory;
                    enemy.Die(string.Format("{0} (으)로 인한 승리", selectedCardData.cardName));
                    if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("승리!", 0);
                }
            }
            else if (selectedCardData.cost > player.currentEnergy) 
            {
                if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("에너지가 부족합니다!", 2f);
            }
            ClearTargetSelectionState(); // 사용 후 상태 초기화
            // 비공격 카드 사용 후에는 바로 PlayerTurn 상태 유지 (CombatManager의 Update 루프에서 다음 입력 대기)
            // 또는, 카드 사용 후 자동으로 턴이 종료되는 규칙이라면 여기서 EndPlayerTurn() 호출
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
        Debug.Log("========= 적의 턴 시작 =========");

        enemy.ResetAllPartBlocks();       // 1. 모든 부위 방어력 초기화
        enemy.ChooseAllPartIntents();     // 2. 모든 부위 행동 결정
        UpdateEnemyStatusUI();            // 3. 변경된 적 의도 UI에 반영 (플레이어가 볼 수 있도록)

        // 적 행동 실행 전 잠시 대기 (플레이어가 의도를 볼 시간)
        // 실제 게임에서는 Invoke나 코루틴으로 지연을 줄 수 있습니다.
        Debug.Log("적이 행동을 준비합니다..."); 
        // yield return new WaitForSeconds(1.0f); // 예시: 코루틴 사용 시

        enemy.ExecuteAllPartIntents(player); // 4. 적의 모든 부위 행동 실행

        UpdatePlayerHandUI();   // 플레이어 상태 UI 업데이트 (피격 등 반영)
        UpdateEnemyStatusUI();  // 적 상태 UI 업데이트 (행동 후 변화 반영)

        // 5. 플레이어 사망 확인
        if (player.currentHealth <= 0) // player.IsDead() 같은 메서드가 있다면 그것 사용
        {
            previousCombatState = currentState;
            currentState = CombatState.Defeat;
            Debug.Log("패배... 플레이어가 쓰러졌습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("패배...", 0); 
            // player.Die(); // 플레이어 사망 처리 메서드가 있다면 호출
            return; // 전투 종료
        }

        // 6. 적 사망 확인
        if (enemy.IsDefeated())
        {
            previousCombatState = currentState;
            currentState = CombatState.Victory;
            // enemy.Die()는 IsDefeated() 내부 또는 여기서 호출될 수 있으나, Enemy.cs의 Die는 이미 로그를 남기므로 중복 호출 주의
            // enemy.Die()가 호출되지 않았다면 여기서 호출: enemy.Die("적 턴 후 플레이어에 의해 패배"); 
            // -> Enemy.cs의 Die 메서드는 isDead 플래그만 설정하고 로그를 남기므로, CombatManager에서 호출하는 것이 적절.
            //    단, enemy.IsDefeated()가 true이면 enemy.Die()를 여기서 또 호출할 필요는 없음.
            //    만약 enemy.Die()가 아직 호출되지 않은 상태에서 IsDefeated()가 true가 될 수 있다면 (예: isDead 플래그만으로 판단)
            //    여기서 enemy.Die()를 호출해야 함.
            //    현재 Enemy.IsDefeated()는 isDead 플래그를 먼저 체크하므로, 만약 Die()가 이미 호출되었다면 또 할 필요는 없음.
            //    하지만 명확성을 위해, IsDefeated()가 true이고 enemy.isDead가 false이면 Die()를 호출하는 것이 안전할 수 있음.
            if (!enemy.isDead) { // 만약 IsDefeated()가 true를 반환했지만 Die()가 아직 호출 안된 경우 대비
                 enemy.Die("적의 턴 종료 후 패배 조건 만족");
            }
            Debug.Log("승리! 적을 물리쳤습니다.");
            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("승리!", 0);
            return; // 전투 종료
        }

        Debug.Log("========= 적의 턴 종료 =========");
        previousCombatState = currentState; 
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
        // 덱 뷰 상태에서는 ESC만 처리
        if (currentState == CombatState.DeckViewing)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnViewDeckButtonPressed(); 
            }
            return; 
        }

        // 승리, 패배, 일시정지 상태에서는 입력 무시 (덱 뷰잉 제외)
        if (currentState == CombatState.Victory || currentState == CombatState.Defeat || currentState == CombatState.Paused)
        {
            return; 
        }

        if (currentState == CombatState.PlayerTurn)
        {
            // E 키로 턴 종료
            if (Input.GetKeyDown(KeyCode.E))
            {
                PlayerEndTurn();
            }
            // 숫자 키로 카드 사용 시도 (1번부터)
            for (int i = 1; i <= 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    if (player.hand.Count >= i)
                    {
                        Debug.Log(string.Format("{0}번 카드 선택 시도.", i));
                        StartTargetSelection(i - 1); // 0-indexed로 변환하여 전달
                        // StartTargetSelection이 상태를 SelectingTarget으로 바꿀 수 있으므로, 이후 입력은 다음 프레임에 처리
                        return; // 한 번에 하나의 카드 선택/타겟팅 프로세스만 진행
                    }
                    else
                    {
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("해당 번호의 카드가 없습니다.", 1.5f);
                        Debug.LogWarning(string.Format("{0}번 카드가 핸드에 없습니다. 핸드 카드 수: {1}", i, player.hand.Count));
                    }
                }
            }
        }
        else if (currentState == CombatState.SelectingTarget)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
            }
            // 숫자 키로 타겟 부위 선택 (1번부터)
            for (int i = 1; i <= currentTargetableParts.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    int targetListIndex = i - 1; // 사용자가 보는 1-based 인덱스를 리스트의 0-based 인덱스로 변환
                    EnemyPart selectedPartObject = currentTargetableParts[targetListIndex];
                    int actualPartIndexInEnemyList = enemy.parts.IndexOf(selectedPartObject);

                    if (actualPartIndexInEnemyList == -1)
                    { // 이런 경우는 거의 없어야 하지만 방어 코드
                        Debug.LogError(string.Format("선택된 부위 '{0}'를 Enemy의 전체 부위 리스트에서 찾을 수 없습니다!", selectedPartObject.partName));
                        if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("부위 선택 오류!", 2f);
                        CancelTargetSelection(); // 타겟팅 취소
                        return;
                    }

                    Debug.Log(string.Format("부위 '{0}' (전체 목록에서 {1}번째 인덱스) 선택됨. 카드 사용 시도: {2}", 
                                        selectedPartObject.partName, actualPartIndexInEnemyList, selectedCardData.cardName ));
                    
                    bool cardPlayed = player.PlayCard(selectedCardIndexToPlay, enemy, actualPartIndexInEnemyList);

                    if (cardPlayed)
                    {
                        UpdatePlayerHandUI();
                        UpdateEnemyStatusUI();
                        if (gameMessageDisplay != null) gameMessageDisplay.ClearMessage(); // 타겟팅 메시지 제거

                        if (enemy.IsDefeated())
                        {
                            previousCombatState = currentState;
                            currentState = CombatState.Victory;
                            if(!enemy.isDead) enemy.Die(string.Format("플레이어의 '{0}' 카드 공격으로 사망", selectedCardData.cardName));
                            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("승리!", 0);
                            // ClearTargetSelectionState(); // 승리/패배 시에는 상태 초기화 불필요할 수 있음
                            return; // 전투 종료
                        }
                        // 플레이어가 죽었는지 확인 (예: 반사 데미지)
                        if (player.currentHealth <= 0) 
                        {
                            previousCombatState = currentState;
                            currentState = CombatState.Defeat;
                            if (gameMessageDisplay != null) gameMessageDisplay.ShowMessage("패배...", 0);
                            return; // 전투 종료
                        }
                        currentState = CombatState.PlayerTurn; // 다음 행동을 위해 플레이어 턴으로 복귀
                    }
                    else // 카드 사용 실패 (예: 에너지 부족 - 이미 PlayCard에서 메시지 처리했을 수 있음)
                    {
                        // Player.PlayCard에서 에너지 부족 메시지를 이미 띄웠을 것이므로, 여기서는 추가 메시지 불필요할 수 있음
                        // 하지만, 만약 다른 이유로 실패했다면 여기서 메시지 처리
                        if (gameMessageDisplay != null && selectedCardData.cost > player.currentEnergy) { /* 이미 처리됨 */ }
                        else if (gameMessageDisplay != null) { gameMessageDisplay.ShowMessage("카드 사용 불가!", 1.5f); }
                        // 실패 시 타겟팅 상태 유지할지, PlayerTurn으로 돌릴지 결정.
                        // 현재는 실패해도 PlayerTurn으로 돌아가도록 아래 ClearTargetSelectionState()가 호출됨.
                    }
                    ClearTargetSelectionState(); // 카드 사용 시도 후 타겟팅 관련 상태 초기화
                    return; // 한 번의 타겟 선택 및 카드 사용 처리 후 Update 종료
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