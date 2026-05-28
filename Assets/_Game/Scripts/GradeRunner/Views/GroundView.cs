using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 플레이어의 이동 지지대 역할을 담당하고, 땅에 닿는 모든 낙하 오브젝트를 감지하여 안전하게 풀로 회수 소멸시키는 바닥 컴포넌트
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    [RequireComponent(typeof(Collider2D))]
    public class GroundView : MonoBehaviour
    {
        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 땅에 닿은 낙하 오브젝트(장애물/아이템)를 감지하여 풀 회수용 메서드를 트리거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            FallingObjectView obj = collision.GetComponent<FallingObjectView>();
            if (obj != null)
            {
                // 바닥에 닿은 것은 감점이나 가산 없이 온전히 풀로 돌려보냄
                obj.func_Deactivate();
            }
        }

        #endregion
    }
}
