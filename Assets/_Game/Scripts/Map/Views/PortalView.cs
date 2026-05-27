using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameArifiction.Player;
using GameArifiction.Interaction;
using EasyTransition;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 포탈의 충돌 범위를 통해 플레이어의 진입을 허용하고, 이지 트랜지션 연동을 통해 연출과 함께 씬 전환을 실행하는 뷰 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PortalView : MonoBehaviour, IInteractable
    {
        #region UI 참조
        [SerializeField]
        [Tooltip("이동하고자 하는 타겟 씬 빌드 인덱스입니다.")]
        private int m_targetMapIndex;

        [SerializeField]
        [Tooltip("포탈 진입 시 플레이어가 새롭게 스폰될 월드 트랜스폼 좌표 포인트입니다.")]
        private Transform m_spawnPoint;

        [Header("씬 전환 설정")]
        [SerializeField]
        [Tooltip("이동하고자 하는 다른 유니티 씬 이름입니다. 비워둘 시 동일 씬 내부 맵 전환이 작동합니다.")]
        private string m_targetSceneName;

        [Header("상호작용 설정")]
        [SerializeField]
        [Tooltip("상호작용 버튼에 표기할 문자열입니다.")]
        private string m_interactionPrompt = "포탈 이동";

        [Header("이지 트랜지션 설정")]
        [SerializeField]
        [Tooltip("씬 전환 시 화면 전환 연출을 위해 사용할 이지 트랜스 설정 자산입니다.")]
        private TransitionSettings m_transitionSettings;

        [SerializeField]
        [Tooltip("트랜스 효과가 진행되기 시작할 딜레이 시간(초)입니다.")]
        private float m_startDelay = 0f;
        #endregion
        

        #region 이벤트
        /// <summary>
        /// 포탈에 진입했을 때 발생하는 이벤트입니다. (대상 맵 인덱스, 스폰 위치)
        /// </summary>
        public event Action<int, Vector2> OnPortalEntered;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// 상호작용 버튼 UI에 표시될 간략한 안내 텍스트입니다.
        /// </summary>
        public string InteractionPrompt => m_interactionPrompt;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            Collider2D portalCollider = GetComponent<Collider2D>();
            if (portalCollider != null)
            {
                portalCollider.isTrigger = true;
                Debug.Log("[PortalView] 포탈의 Collider2D를 트리거(isTrigger = true)로 자동으로 설정했습니다.");
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 상호작용 버튼 클릭 시 호출되며, 씬 이름이 있으면 씬 전환을, 없으면 동일 씬 내부 맵 전환을 실행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 포탈 진입 시 플레이어 현재 위치를 저장하여 로비 복귀 시 해당 위치로 복원되도록 설계 오류 수정
        /// </summary>
        /// <param name="user">상호작용을 실행한 플레이어 오브젝트</param>
        public void Interact(GameObject user)
        {
            Debug.Log($"[PortalView] 플레이어와 포탈의 상호작용을 시작합니다. 이동 대상 씬: {m_targetSceneName}, 대상 맵 인덱스: {m_targetMapIndex}");

            // A. 다른 씬으로 넘어가는 씬 포탈인 경우 (씬 이름 지정 기준 혹은 유효한 인덱스 기준)
            // [수정]: 씬 이름이 지정되어 있거나, m_targetMapIndex가 0보다 클 경우 씬 전환으로 처리합니다.
            if (!string.IsNullOrEmpty(m_targetSceneName) || m_targetMapIndex > 0)
            {
                PlayerView playerView = user.GetComponent<PlayerView>();
                if (playerView != null)
                {
                    Vector2 backupPosition = (Vector2)user.transform.position;
                    playerView.SavePosition(backupPosition);
                }

                // 이동할 타겟 정보 식별 (씬 이름 우선, 없으면 빌드 인덱스)
                bool hasSceneName = !string.IsNullOrEmpty(m_targetSceneName);

                // 이지 트랜지션 설정이 있고, 씬에 매니저가 유효한 경우 안전 연동
                if (m_transitionSettings != null)
                {
                    TransitionManager manager = FindFirstObjectByType<TransitionManager>();
                    if (manager != null)
                    {
                        if (hasSceneName)
                        {
                            TransitionManager.Instance().Transition(m_targetSceneName, m_transitionSettings, m_startDelay);
                        }
                        else
                        {
                            TransitionManager.Instance().Transition(m_targetMapIndex, m_transitionSettings, m_startDelay);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PortalView] 씬에 TransitionManager가 존재하지 않습니다. 즉시 씬 전환을 실행합니다.");
                        if (hasSceneName)
                        {
                            SceneManager.LoadScene(m_targetSceneName);
                        }
                        else
                        {
                            SceneManager.LoadScene(m_targetMapIndex);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[PortalView] 인스펙터에 TransitionSettings가 할당되지 않았습니다. 즉시 씬 전환을 실행합니다.");
                    if (hasSceneName)
                    {
                        SceneManager.LoadScene(m_targetSceneName);
                    }
                    else
                    {
                        SceneManager.LoadScene(m_targetMapIndex);
                    }
                }
            }
            // B. 동일 씬 내부의 다른 맵으로 넘어가는 경우 (맵 인덱스가 0 이하이거나 맵 인덱스를 활용하는 경우)
            else
            {
                Vector2 spawnPos = m_spawnPoint != null ? (Vector2)m_spawnPoint.position : (Vector2)transform.position;
                OnPortalEntered?.Invoke(m_targetMapIndex, spawnPos);
            }
        }
        #endregion
    }
}
