using DG.Tweening;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 플레이어를 향해 코드를 떨어뜨리는 교수 캐릭터의 뷰(View) 컴포넌트.
///         페이즈(Phase 1/2)에 따른 시각적 형상 교체 및 장애물 투사 위치 추적 이동 연출을 처리합니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class ProfessorView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("페이즈별 교수님 비주얼")]
        [SerializeField]
        [Tooltip("1페이즈(기본/온화) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase1Visual;

        [SerializeField]
        [Tooltip("2페이즈 시작(분노 변신 연출) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase2StartVisual;

        [SerializeField]
        [Tooltip("2페이즈 실제 진행(열정/공격) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase2Visual;

        [Header("대사 UI 참조")]
        [SerializeField]
        [Tooltip("교수 대사를 출력할 말풍선 UI CanvasGroup 컴포넌트입니다.")]
        private CanvasGroup m_dialogueBubble;

        [SerializeField]
        [Tooltip("말풍선 내 텍스트를 출력할 TextMeshProUGUI 컴포넌트입니다.")]
        private TMPro.TextMeshProUGUI m_dialogueText;

        [Header("대사 연출 속도 설정")]
        [SerializeField]
        [Range(0.01f, 0.2f)]
        [Tooltip("타이핑 효과 시 글자 하나가 출력되는 시간 간격(초)입니다. 기본값 0.04초.")]
        private float m_typingSpeed = 0.04f;

        [SerializeField]
        [Range(0.5f, 5.0f)]
        [Tooltip("타이핑 완료 후 말풍선이 화면에 완전히 머무르는 시간(초)입니다. 기본값 1.5초.")]
        private float m_dialogueHoldDuration = 1.5f;

        [Header("이동 연출 설정")]
        [SerializeField]
        [Tooltip("공격 지점으로 이동할 때의 도트윈 이징 방식입니다.")]
        private Ease m_moveEase = Ease.OutQuad;

        [SerializeField]
        [Tooltip("교수님이 좌우로 이동할 수 있는 X좌표 최소/최대 범위 제한 설정입니다.")]
        private Vector2 m_movementRangeX = new Vector2(-7.5f, 7.5f);

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private float m_startPositionX; // 게임 시작 시점의 최초 X좌표

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void OnDestroy()
        {
            UnsubscribeEvents();
            transform.DOKill();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 뷰모델 의존성을 주입하고 페이즈 교체 및 컷씬 이벤트를 연동하며 초기 비주얼 상태를 세팅합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_startPositionX = transform.position.x; // 최초 스폰 당시 시작 X좌표 기록

            if (m_viewModel != null)
            {
                m_viewModel.OnPhaseChanged += HandlePhaseChanged;
                m_viewModel.OnIntroCutsceneStarted += HandleIntroCutscene;
                m_viewModel.OnPhase2CutsceneStarted += HandlePhase2Cutscene;
            }

            // 초기 비주얼 세팅: 말풍선 숨김 및 1페이즈 기본 상태 준비 (도입부에서 페이드인 예정이므로 일단 꺼두거나 투명하게 대기)
            if (m_dialogueBubble != null)
            {
                m_dialogueBubble.gameObject.SetActive(false);
            }

            SetVisualActiveOnly(m_phase1Visual);
            Debug.Log("[ProfessorView] 교수님 공격 캐릭터 뷰 초기화 성공.");
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPhaseChanged -= HandlePhaseChanged;
                m_viewModel.OnIntroCutsceneStarted -= HandleIntroCutscene;
                m_viewModel.OnPhase2CutsceneStarted -= HandlePhase2Cutscene;
            }
        }

        #endregion

        #region 공개 연출 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 스포너가 특정 X좌표에서 코드 장애물을 스폰하려 할 때, 설정된 X 범위 내로 한계를 보장(Clamping)하며 신속하게 이동시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_MoveTo(float targetX, float duration = 0.2f)
        {
            // 인스펙터의 좌우 X이동 한계 범위를 명확히 적용
            float clampedX = Mathf.Clamp(targetX, m_movementRangeX.x, m_movementRangeX.y);

            transform.DOKill();
            transform.DOMoveX(clampedX, duration).SetEase(m_moveEase);
        }

        #endregion

        #region 이벤트 핸들러 및 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 도입부 컷씬 트리거 시 호출되며, 교수님이 서서히 등장(Fade-In) 후 첫 대사를 타이핑합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleIntroCutscene()
        {
            SetVisualActiveOnly(m_phase1Visual);

            // 교수님 비주얼의 모든 스프라이트 렌더러를 0 alpha로 리셋 후 서서히 페이드인
            GameObject activeVis = m_phase1Visual;
            if (activeVis != null)
            {
                SpriteRenderer[] sprites = activeVis.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                    {
                        Color c = sprites[i].color;
                        c.a = 0f;
                        sprites[i].color = c;
                        sprites[i].DOFade(1f, 1.0f);
                    }
                }
            }

            // 등장 페이드인이 끝날 즈음 말풍선 대사 연출 시작
            DOVirtual.DelayedCall(1.0f, () =>
            {
                if (m_viewModel != null)
                {
                    TypeDialogue(m_viewModel.IntroDialogue, () =>
                    {
                        if (m_viewModel != null)
                        {
                            m_viewModel.CompleteIntroCutscene();
                        }
                    });
                }
            });
        }

        /// <summary>
        /// [기능]: 2페이즈 전환 컷씬 트리거 시 호출되며, 교수님 형상을 연출용(Phase 2 Start)으로 교체하고 분노 진동 후 대사를 치고 본 2페이즈 비주얼로 바꿉니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePhase2Cutscene()
        {
            // 모든 연출 스케일/이동 중지 후 즉시 최초 시작 X위치로 신속 이동
            transform.DOKill();
            transform.DOMoveX(m_startPositionX, 0.4f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                // 원위치 도착 완료 시점부터 본격 2페이즈 변신 연출 시작
                // 2페이즈 시작용 임시 연출 비주얼 활성화
                SetVisualActiveOnly(m_phase2StartVisual);

                // 위압감 넘치는 분노 진동 연출
                transform.DOShakePosition(0.6f, 0.4f, 15);
                transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.5f, 8, 1.0f);

                DOVirtual.DelayedCall(0.7f, () =>
                {
                    if (m_viewModel != null)
                    {
                        TypeDialogue(m_viewModel.Phase2Dialogue, () =>
                        {
                            // 대사가 끝나면 최종 2페이즈 공격형태 비주얼로 교체하고 게임 재개
                            SetVisualActiveOnly(m_phase2Visual);
                            if (m_viewModel != null)
                            {
                                m_viewModel.CompletePhase2Cutscene();
                            }
                        });
                    }
                });
            });
        }

        /// <summary>
        /// [기능]: 일반 페이즈 전환 이벤트가 호출될 때 시각 보완을 진행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePhaseChanged(GradeRunnerPhase phase)
        {
            if (phase == GradeRunnerPhase.Phase2)
            {
                // 변신 전환 효과 증대 Punch 연출
                transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.5f, 6, 1f);
            }
        }

        /// <summary>
        /// [기능]: 3가지 상태(1페이즈, 2페이즈 시작 연출, 2페이즈 실제 진행) 중 지정한 하나만 켜고 나머지는 끄는 안전한 전환 메서드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SetVisualActiveOnly(GameObject activeVisual)
        {
            if (m_phase1Visual != null)
            {
                m_phase1Visual.SetActive(m_phase1Visual == activeVisual);
            }
            if (m_phase2StartVisual != null)
            {
                m_phase2StartVisual.SetActive(m_phase2StartVisual == activeVisual);
            }
            if (m_phase2Visual != null)
            {
                m_phase2Visual.SetActive(m_phase2Visual == activeVisual);
            }
        }

        /// <summary>
        /// [기능]: 말풍선을 활성화하고 텍스트를 타자기 방식으로 쳐준 뒤 부드럽게 퇴출 후 콜백을 실행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void TypeDialogue(string text, System.Action onComplete)
        {
            TypeDialogueAsync(text, onComplete, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid TypeDialogueAsync(string text, System.Action onComplete, CancellationToken cancellationToken)
        {
            if (m_dialogueBubble != null)
            {
                m_dialogueBubble.gameObject.SetActive(true);
                m_dialogueBubble.alpha = 0f;
                m_dialogueBubble.DOFade(1f, 0.25f);
            }

            if (m_dialogueText != null)
            {
                m_dialogueText.text = "";

                // 타자기 효과: 글자 단위로 차례차례 출력
                for (int i = 0; i <= text.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    m_dialogueText.text = text.Substring(0, i);
                    await UniTask.Delay(System.TimeSpan.FromSeconds(m_typingSpeed), cancellationToken: cancellationToken).SuppressCancellationThrow();
                }

                // 다 출력된 후 설정된 대기 시간 동안 머무름
                await UniTask.Delay(System.TimeSpan.FromSeconds(m_dialogueHoldDuration), cancellationToken: cancellationToken).SuppressCancellationThrow();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (m_dialogueBubble != null)
                {
                    m_dialogueBubble.DOFade(0f, 0.25f).OnComplete(() =>
                    {
                        m_dialogueBubble.gameObject.SetActive(false);
                        if (onComplete != null)
                        {
                            onComplete.Invoke();
                        }
                    });
                }
                else
                {
                    if (onComplete != null)
                    {
                        onComplete.Invoke();
                    }
                }
            }
            else
            {
                if (onComplete != null)
                {
                    onComplete.Invoke();
                }
            }
        }

        #endregion
    }
}
