using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameArifiction.Player;
using GameArifiction.Interaction;
using GameArifiction.UI.Common;
using EasyTransition;
using VContainer;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 포탈의 충돌 범위를 통해 플레이어의 진입을 허용하고, 이지 트랜지션 연동을 통해 연출과 함께 씬 전환을 실행하는 뷰 클래스 (최초 플레이 유도용 느낌표 이미지 노출 기능 포함)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: MVVM 패턴 준수를 위해 PlayerSO 직접 참조 제거 및 MapViewModel 우회 연동, OnEnable 시각적 갱신 적용
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

        [Header("미니게임 성적 연동")]
        [SerializeField]
        [Tooltip("연동할 미니게임 ID입니다. 비워둘 경우 타겟 씬 이름을 ID로 사용합니다.")]
        private string m_minigameId;

        [SerializeField]
        [Tooltip("플레이 기록이 없을 때 특수 연출 스프라이트(느낌표)를 노출할지 여부입니다.")]
        private bool m_showSpecialSpriteIfNoRecord = true;

        [SerializeField]
        [Tooltip("성적 이미지를 표시할 자식 SpriteRenderer입니다. 미지정 시 자식 오브젝트에서 자동으로 검색합니다.")]
        private SpriteRenderer m_gradeSpriteRenderer;

        [SerializeField]
        [Tooltip("등급별 스프라이트 관리를 수행할 ScriptableObject 데이터 자산입니다.")]
        private MinigameGradeSpritesSO m_gradeSpritesSO;

        #endregion

        #region 내부 필드 (Private Fields)
        private SpriteRenderer m_cachedGradeSpriteRenderer;
        private MapViewModel m_mapViewModel;
        private string m_cachedMinigameId;
        #endregion

        #region 의존성 주입
        /// <summary>
        /// [기능]: VContainer 수명주기 컨테이너로부터 MapViewModel 인스턴스를 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(MapViewModel mapViewModel)
        {
            m_mapViewModel = mapViewModel;
            Debug.Log("[PortalView] VContainer를 통해 MapViewModel 의존성이 자동으로 주입되었습니다.");
        }
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
        /// <summary>
        /// [기능]: 컴포넌트 초기화 시 콜라이더 트리거 설정, 자식 렌더러 캐싱 및 타겟 미니게임 ID를 조기에 분석합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 미니게임 ID 캐싱 로직을 Awake로 이관
        /// </summary>
        private void Awake()
        {
            Collider2D portalCollider = GetComponent<Collider2D>();
            if (portalCollider != null)
            {
                portalCollider.isTrigger = true;
                Debug.Log("[PortalView] 포탈의 Collider2D를 트리거(isTrigger = true)로 자동으로 설정했습니다.");
            }

            // [최적화 및 수동지정 예외처리] 지정된 렌더러가 없을 경우 자식 오브젝트에서 안전하게 자동 탐색
            if (m_gradeSpriteRenderer != null)
            {
                m_cachedGradeSpriteRenderer = m_gradeSpriteRenderer;
            }
            else
            {
                SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < childRenderers.Length; i++)
                {
                    if (childRenderers[i].gameObject != gameObject)
                    {
                        m_cachedGradeSpriteRenderer = childRenderers[i];
                        break;
                    }
                }
            }

            // 미니게임 고유 ID 결정 및 캐싱
            m_cachedMinigameId = string.IsNullOrEmpty(m_minigameId) ? GetTargetSceneName() : m_minigameId;
        }

        /// <summary>
        /// [기능]: 포탈 오브젝트 활성화 시점에 최신 플레이 데이터를 뷰모델로부터 실시간으로 반영합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Start 대신 OnEnable 갱신 연동
        /// </summary>
        private void OnEnable()
        {
            UpdateGradeDisplay();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 상호작용 버튼 클릭 시 호출되며, 씬 이름이 있으면 씬 전환을, 없으면 동일 씬 내부 맵 전환을 실행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 포탈 이동 시 리플렉션 비용이 큰 FindFirstObjectByType 중복 조사를 배제하고 TransitionManager.Instance() 싱글톤 인터페이스로 구조화
        /// </summary>
        /// <param name="user">상호작용을 실행한 플레이어 오브젝트</param>
        public void Interact(GameObject user)
        {
            Debug.Log($"[PortalView] 플레이어와 포탈의 상호작용을 시작합니다. 대상 빌드 인덱스: {m_targetMapIndex}");

            // A. 다른 씬으로 넘어가는 씬 포탈인 경우 (빌드 인덱스가 0보다 큰 경우)
            if (m_targetMapIndex > 0)
            {
                PlayerView playerView = user.GetComponent<PlayerView>();
                if (playerView != null)
                {
                    Vector2 backupPosition = (Vector2)user.transform.position;
                    playerView.SavePosition(backupPosition);
                }

                // 이지 트랜지션 설정이 있고, 씬에 매니저가 유효한 경우 안전 연동
                if (m_transitionSettings != null)
                {
                    TransitionManager manager = TransitionManager.Instance();
                    if (manager != null)
                    {
                        manager.Transition(m_targetMapIndex, m_transitionSettings, m_startDelay);
                    }
                    else
                    {
                        Debug.LogWarning("[PortalView] 씬에 TransitionManager가 존재하지 않습니다. 즉시 씬 전환을 실행합니다.");
                        SceneManager.LoadScene(m_targetMapIndex);
                    }
                }
                else
                {
                    Debug.LogWarning("[PortalView] 인스펙터에 TransitionSettings가 할당되지 않았습니다. 즉시 씬 전환을 실행합니다.");
                    SceneManager.LoadScene(m_targetMapIndex);
                }
            }
            // B. 동일 씬 내부의 다른 맵으로 넘어가는 경우 (맵 인덱스가 0 이하인 경우)
            else
            {
                Vector2 spawnPos = m_spawnPoint != null ? (Vector2)m_spawnPoint.position : (Vector2)transform.position;
                OnPortalEntered?.Invoke(m_targetMapIndex, spawnPos);
            }
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 타겟 빌드 인덱스(m_targetMapIndex) 정보로부터 해당 씬의 순수 파일 이름을 역추적하여 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private string GetTargetSceneName()
        {
            if (m_targetMapIndex > 0)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(m_targetMapIndex);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(scenePath);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// [기능]: MapViewModel을 통해 성적 기록을 확인하여 포탈 앞에 성적 이미지를 갱신 표시합니다. 최초 플레이 시에는 느낌표 이미지를 띄웁니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: PlayerSO 의존성을 배제하고 MapViewModel로부터 갱신 상태 획득하도록 개선
        /// </summary>
        private void UpdateGradeDisplay()
        {
            if (m_mapViewModel == null)
            {
                Debug.LogWarning($"[PortalView] '{gameObject.name}'에 MapViewModel 레퍼런스가 할당되지 않아 성적 표시를 생략합니다.");
                if (m_cachedGradeSpriteRenderer != null)
                {
                    m_cachedGradeSpriteRenderer.gameObject.SetActive(false);
                }
                return;
            }

            if (m_gradeSpritesSO == null)
            {
                Debug.LogWarning($"[PortalView] '{gameObject.name}'에 MinigameGradeSpritesSO 레퍼런스가 할당되지 않아 성적 표시를 생략합니다.");
                if (m_cachedGradeSpriteRenderer != null)
                {
                    m_cachedGradeSpriteRenderer.gameObject.SetActive(false);
                }
                return;
            }

            // 보정 및 데이터 획득은 뷰모델에서 캡슐화 처리
            MinigameGrade grade = m_mapViewModel.GetMinigameGrade(m_cachedMinigameId);

            if (m_cachedGradeSpriteRenderer != null)
            {
                if (grade == MinigameGrade.None)
                {
                    if (m_showSpecialSpriteIfNoRecord)
                    {
                        Sprite exclamation = m_gradeSpritesSO.ExclamationSprite;
                        if (exclamation != null)
                        {
                            m_cachedGradeSpriteRenderer.sprite = exclamation;
                            m_cachedGradeSpriteRenderer.gameObject.SetActive(true);
                            Debug.Log($"[PortalView] '{gameObject.name}' 문앞에 최초 플레이 유도를 위한 느낌표 이미지를 표시했습니다.");
                        }
                        else
                        {
                            m_cachedGradeSpriteRenderer.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        m_cachedGradeSpriteRenderer.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Sprite targetSprite = m_gradeSpritesSO.GetSprite(grade);
                    if (targetSprite != null)
                    {
                        m_cachedGradeSpriteRenderer.sprite = targetSprite;
                        m_cachedGradeSpriteRenderer.gameObject.SetActive(true);
                        Debug.Log($"[PortalView] '{gameObject.name}' 문앞에 미니게임 '{m_cachedMinigameId}' 성적 이미지({grade})를 갱신 표시했습니다.");
                    }
                    else
                    {
                        m_cachedGradeSpriteRenderer.gameObject.SetActive(false);
                        Debug.LogWarning($"[PortalView] '{gameObject.name}' 성적({grade})에 할당된 스프라이트 에셋이 없거나 부족합니다.");
                    }
                }
            }
        }
        #endregion
    }
}
