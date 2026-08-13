using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [기능]: TextMeshProUGUI 컴포넌트에 타이핑 연출을 제공하고, 선택적으로 지정된 월드 타겟의 머리 위에 말풍선 위치와 고정 축(Pivot)을 실시간 추적 동기화하는 공통 UI 컴포넌트입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class TypewriterComponent : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("텍스트 컴포넌트")]
        [SerializeField]
        [Tooltip("타이핑 효과를 적용할 텍스트 컴포넌트입니다. 비워둘 경우 본인에게서 찾습니다.")]
        private TextMeshProUGUI m_textMesh;

        [Header("레이아웃 설정")]
        [SerializeField]
        [Tooltip("텍스트 변경 시 크기를 실시간 리빌드할 RectTransform입니다. 말풍선 배경 패널을 지정하십시오.")]
        private RectTransform m_layoutToRebuild;

        [Header("말풍선 고정 축 및 월드 추적 설정")]
        [SerializeField]
        [Tooltip("활성화 시 지정된 Target Transform의 머리 위 월드 좌표를 추적하여 말풍선 UI 위치를 동기화합니다.")]
        private bool m_enableWorldTracking = false;

        [SerializeField]
        [Tooltip("위치 동기화의 대상이 될 월드 트랜스폼(예: 플레이어)입니다.")]
        private Transform m_targetTransform;

        [SerializeField]
        [Tooltip("말풍선 꼬리표 기준 확장 및 동기화 기준이 될 피벗 값입니다. (예: 하단 중앙 = 0.5, 0)")]
        private Vector2 m_bubblePivot = new Vector2(0.5f, 0f);

        [SerializeField]
        [Tooltip("대상 오브젝트 머리 위에 띄우기 위한 Y축 월드 보정 오프셋(m)입니다.")]
        private float m_worldOffsetY = 2.3f;

        #endregion

        #region 내부 필드 (Private Fields)

        private RectTransform m_rectTransform;
        private RectTransform m_canvasRect;
        private Camera m_mainCamera;
        private CancellationTokenSource m_cts;
        private string m_targetFullText;
        private bool m_isTyping = false;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public bool IsTyping => m_isTyping;

        public RectTransform LayoutToRebuild
        {
            get => m_layoutToRebuild;
            set => m_layoutToRebuild = value;
        }

        public Transform TargetTransform
        {
            get => m_targetTransform;
            set => m_targetTransform = value;
        }

        public bool EnableWorldTracking
        {
            get => m_enableWorldTracking;
            set => m_enableWorldTracking = value;
        }

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 텍스트 컴포넌트와 부모 RectTransform을 조기 캐싱합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
            
            if (m_textMesh == null)
            {
                m_textMesh = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            m_mainCamera = Camera.main;
        }

        /// <summary>
        /// [기능]: 부모 캔버스 RectTransform 정보를 초기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void Start()
        {
            // 부모 캔버스 Rect 캐싱
            if (m_rectTransform != null && m_rectTransform.parent != null)
            {
                m_canvasRect = m_rectTransform.parent as RectTransform;
            }
        }

        /// <summary>
        /// [기능]: 월드 트래킹이 활성화되어 있을 경우, 타겟의 머리 위 위치로 UI를 매 프레임 실시간 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void LateUpdate()
        {
            if (m_enableWorldTracking)
            {
                UpdateBubblePosition();
            }
        }

        /// <summary>
        /// [기능]: 오브젝트가 파괴될 때 실행 중인 비동기 타이핑 작업을 안전하게 취소 및 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void OnDestroy()
        {
            CleanUpCts();
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 전달받은 텍스트를 지정된 속도로 타이핑하는 연출을 개시합니다. 기존에 진행 중인 타이핑이 있다면 취소합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이핑 텍스트 널 체크 방어 코드 추가
        /// </summary>
        public async UniTask PlayTypingEffectAsync(string text, float typingSpeed = 0.05f)
        {
            CleanUpCts();
            m_cts = new CancellationTokenSource();
            
            m_targetFullText = text;
            m_isTyping = true;

            if (m_textMesh != null)
            {
                m_textMesh.text = "";
            }

            try
            {
                await StartTypingLoopAsync(typingSpeed, m_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 취소 시에는 에러 로그 없이 정상 종료로 처리합니다.
            }
            finally
            {
                m_isTyping = false;
            }
        }

        /// <summary>
        /// [기능]: 타이핑 연출을 즉시 취소하고, 최종 타겟 텍스트를 화면에 전부 표시하여 완료 상태로 만듭니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void CompleteTypingImmediate()
        {
            if (!m_isTyping)
            {
                return;
            }

            CleanUpCts();
            if (m_textMesh != null)
            {
                m_textMesh.text = m_targetFullText;
            }
            m_isTyping = false;
            
            RebuildLayout();
            Debug.Log($"[TypewriterComponent] 타이핑이 강제 완료 처리되었습니다. 전체 텍스트 표시 및 레이아웃 갱신.");
        }

        /// <summary>
        /// [기능]: 타이핑 연출을 즉시 정지하고 텍스트 창을 비웁니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StopTyping()
        {
            CleanUpCts();
            if (m_textMesh != null)
            {
                m_textMesh.text = string.Empty;
            }
            m_isTyping = false;
            RebuildLayout();
        }

        #endregion

        #region 내부 로직 (Private Methods)

        /// <summary>
        /// [기능]: 비동기로 지정된 속도에 맞춰 문자열을 한 글자씩 표시하며 레이아웃을 갱신하는 타이핑 루프 코어 로직입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private async UniTask StartTypingLoopAsync(float speed, CancellationToken token)
        {
            if (m_textMesh == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_targetFullText.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                sb.Append(m_targetFullText[i]);
                m_textMesh.text = sb.ToString();

                RebuildLayout();

                await UniTask.Delay(TimeSpan.FromSeconds(speed), cancellationToken: token);
            }
        }

        /// <summary>
        /// [기능]: 말풍선의 고정 축(Pivot)과 월드 타겟 오브젝트 간의 좌표를 매칭하여 UI 상의 위치를 실시간으로 갱신합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateBubblePosition()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
                if (m_rectTransform == null)
                {
                    return;
                }
            }

            if (m_targetTransform == null)
            {
                return;
            }

            // 고정 피벗 설정 반영
            m_rectTransform.pivot = m_bubblePivot;

            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
                if (m_mainCamera == null)
                {
                    return;
                }
            }

            Vector3 worldPos = m_targetTransform.position;
            worldPos.y += m_worldOffsetY;

            Vector2 screenPoint = m_mainCamera.WorldToScreenPoint(worldPos);

            if (m_canvasRect == null && m_rectTransform.parent != null)
            {
                m_canvasRect = m_rectTransform.parent as RectTransform;
            }

            if (m_canvasRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvasRect, screenPoint, null, out localPoint))
                {
                    m_rectTransform.anchoredPosition = localPoint;
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// [기능]: 에디터 상에서 값이 수정될 때(피벗, 오프셋 등) 씬 뷰 및 게임 뷰에 위치와 설정을 실시간으로 즉시 반영합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void OnValidate()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }

            if (m_rectTransform != null)
            {
                m_rectTransform.pivot = m_bubblePivot;
            }

            if (m_enableWorldTracking && m_targetTransform != null)
            {
                UpdateBubblePosition();
            }
        }
#endif

        /// <summary>
        /// [기능]: 지정된 UI 오브젝트 및 상위 레이아웃 구성요소의 크기를 강제로 재계산하여 말풍선의 고정 축(Pivot) 방향으로 레이아웃을 바르게 확장시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void RebuildLayout()
        {
            if (m_layoutToRebuild != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_layoutToRebuild);
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }

        /// <summary>
        /// [기능]: 기존에 등록된 취소 토큰 소스(CTS)가 있다면 취소하고 메모리를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void CleanUpCts()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }
        }

        #endregion
    }
}
