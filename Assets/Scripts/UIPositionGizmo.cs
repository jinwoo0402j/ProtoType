using UnityEngine;

public class UIPositionGizmo : MonoBehaviour
{
    // 이 함수는 오브젝트가 선택되었든 아니든 항상 씬 뷰에 기즈모를 그립니다.
    void OnDrawGizmos()
    {
        // 기즈모의 색상을 반투명한 노란색으로 설정합니다.
        Gizmos.color = new Color(1, 1, 0, 0.5f); 
        // 이 오브젝트의 위치에 작은 구체를 그립니다. (UI Canvas에서는 픽셀 단위처럼 작동)
        Gizmos.DrawSphere(transform.position, 15f);
    }

    // 이 함수는 오브젝트가 '선택되었을 때만' 씬 뷰에 기즈모를 그립니다.
    void OnDrawGizmosSelected()
    {
        // 선택되었을 때는 더 잘 보이도록 불투명한 청록색으로 설정합니다.
        Gizmos.color = Color.cyan;
        // 선택되었을 때는 더 큰 구체를 그려서 명확하게 표시합니다.
        Gizmos.DrawSphere(transform.position, 20f);
    }
} 