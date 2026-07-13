using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: SPUM 캐릭터에 타이밍 성공·실패 애니메이션과 피격 효과를 출력합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimingCatchCharacterView : MonoBehaviour, ITimingCatchCharacterView
    {
        #region SPUM 참조 (Inspector)
        [SerializeField] private SPUM_Prefabs m_spumPrefab;
        [SerializeField] private AnimationClip m_successClip;
        #endregion

        #region 피격 효과 참조 (Inspector)
        [SerializeField] private GameObject m_hitEffectPrefab;
        [SerializeField] private Transform m_hitEffectAnchor;
        [SerializeField, Min(0.05f)] private float m_hitEffectLifetime = 0.35f;
        #endregion

        #region 내부 필드 (Private Fields)
        private CancellationTokenSource m_reactionCancellationTokenSource;
        private GameObject m_activeHitEffect;
        private int m_successAnimationIndex = -1;
        private int m_reactionVersion;
        private bool m_isInitialized;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 직렬화된 SPUM 참조와 성공 클립을 런타임 오버라이드 컨트롤러에 등록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 캐릭터 반응 View 초기화 추가.
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// [기능]: 오브젝트 파괴 시 진행 중인 반응과 생성된 피격 효과를 정리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 비동기 반응 취소와 런타임 효과 정리 추가.
        /// </summary>
        private void OnDestroy()
        {
            CancelActiveReaction();
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 피격 클립을 기반으로 제작한 전용 성공 애니메이션을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: OTHER 슬롯 성공 반응 재생 추가.
        /// </summary>
        public void PlaySuccessReaction()
        {
            if (Initialize() == false || m_successAnimationIndex < 0)
            {
                return;
            }

            PlayReaction(PlayerState.OTHER, m_successAnimationIndex, m_successClip.length, false);
        }

        /// <summary>
        /// [기능]: SPUM 피격 애니메이션과 Hit Effect를 함께 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: DAMAGED 슬롯 실패 반응과 피격 효과 출력 추가.
        /// </summary>
        public void PlayFailureReaction()
        {
            if (Initialize() == false || m_spumPrefab.DAMAGED_List.Count == 0)
            {
                return;
            }

            AnimationClip damagedClip = m_spumPrefab.DAMAGED_List[0];
            if (damagedClip == null)
            {
                Debug.LogWarning("[TimingCatchCharacterView] 피격 애니메이션 클립이 비어 있어 실패 반응을 재생할 수 없습니다.");
                return;
            }

            PlayReaction(PlayerState.DAMAGED, 0, damagedClip.length, true);
        }

        /// <summary>
        /// [기능]: 진행 중인 반응을 취소하고 SPUM 캐릭터를 기본 대기 상태로 복귀시킵니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 외부 대기 상태 복귀 명령 추가.
        /// </summary>
        public void ResetToIdle()
        {
            CancelActiveReaction();
            ApplyIdleAnimation();
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: SPUM Animator를 검증하고 성공 클립을 OTHER 목록에 중복 없이 등록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 오버라이드 컨트롤러 안전 초기화 추가.
        /// </summary>
        private bool Initialize()
        {
            if (m_isInitialized)
            {
                return true;
            }

            if (m_spumPrefab == null)
            {
                m_spumPrefab = GetComponentInChildren<SPUM_Prefabs>(true);
            }

            if (m_spumPrefab == null || m_successClip == null)
            {
                Debug.LogError("[TimingCatchCharacterView] SPUM 캐릭터 또는 성공 애니메이션 참조가 할당되지 않았습니다.");
                return false;
            }

            if (m_spumPrefab._anim == null)
            {
                m_spumPrefab._anim = m_spumPrefab.GetComponentInChildren<Animator>(true);
            }

            if (m_spumPrefab._anim == null || m_spumPrefab._anim.runtimeAnimatorController == null)
            {
                Debug.LogError("[TimingCatchCharacterView] SPUM Animator 또는 Runtime Animator Controller가 없습니다.");
                return false;
            }

            EnsureAnimationCollections();
            m_successAnimationIndex = m_spumPrefab.OTHER_List.IndexOf(m_successClip);
            if (m_successAnimationIndex < 0)
            {
                m_spumPrefab.OTHER_List.Add(m_successClip);
                m_successAnimationIndex = m_spumPrefab.OTHER_List.Count - 1;
            }

            m_spumPrefab.OverrideControllerInit();
            m_isInitialized = true;
            ApplyIdleAnimation();
            return true;
        }

        /// <summary>
        /// [기능]: SPUM의 상태별 애니메이션 컬렉션이 런타임에 사용할 수 있도록 보장합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 누락 컬렉션 방어 초기화 추가.
        /// </summary>
        private void EnsureAnimationCollections()
        {
            if (m_spumPrefab.StateAnimationPairs == null)
            {
                m_spumPrefab.StateAnimationPairs = new Dictionary<string, List<AnimationClip>>();
            }

            if (m_spumPrefab.OTHER_List == null)
            {
                m_spumPrefab.OTHER_List = new List<AnimationClip>();
            }

            if (m_spumPrefab.IDLE_List == null)
            {
                m_spumPrefab.IDLE_List = new List<AnimationClip>();
            }

            if (m_spumPrefab.DAMAGED_List == null)
            {
                m_spumPrefab.DAMAGED_List = new List<AnimationClip>();
            }
        }
        #endregion

        #region 반응 제어 (Reaction Control)
        /// <summary>
        /// [기능]: 최신 판정만 유효하도록 기존 반응을 취소한 뒤 지정 SPUM 상태를 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 반응 선점과 자동 IDLE 복귀 추가.
        /// </summary>
        private void PlayReaction(PlayerState state, int animationIndex, float duration, bool isHitEffectRequired)
        {
            CancelActiveReaction();

            if (PlayAnimation(state, animationIndex) == false)
            {
                return;
            }

            if (isHitEffectRequired)
            {
                SpawnHitEffect();
            }

            m_reactionCancellationTokenSource = new CancellationTokenSource();
            int reactionVersion = m_reactionVersion;
            float reactionDuration = Mathf.Max(duration, isHitEffectRequired ? m_hitEffectLifetime : 0.05f);
            CompleteReactionAsync(
                reactionDuration,
                reactionVersion,
                m_reactionCancellationTokenSource.Token
            ).Forget();
        }

        /// <summary>
        /// [기능]: 반응 재생 시간이 지난 뒤 최신 반응에 한해 IDLE 상태로 복귀합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: CancellationToken 기반 반응 수명 관리 추가.
        /// </summary>
        private async UniTaskVoid CompleteReactionAsync(
            float duration,
            int reactionVersion,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (reactionVersion != m_reactionVersion)
            {
                return;
            }

            if (m_reactionCancellationTokenSource != null)
            {
                m_reactionCancellationTokenSource.Dispose();
                m_reactionCancellationTokenSource = null;
            }

            CleanupHitEffect();
            ApplyIdleAnimation();
        }

        /// <summary>
        /// [기능]: 진행 중인 반응의 비동기 대기와 피격 효과를 취소·정리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 연속 판정 시 이전 반응 선점 처리 추가.
        /// </summary>
        private void CancelActiveReaction()
        {
            m_reactionVersion++;

            if (m_reactionCancellationTokenSource != null)
            {
                m_reactionCancellationTokenSource.Cancel();
                m_reactionCancellationTokenSource.Dispose();
                m_reactionCancellationTokenSource = null;
            }

            CleanupHitEffect();
        }

        /// <summary>
        /// [기능]: SPUM 상태 목록의 지정 클립을 오버라이드하고 Animator 상태를 처음부터 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 반응 애니메이션 공통 실행 추가.
        /// </summary>
        private bool PlayAnimation(PlayerState state, int animationIndex)
        {
            try
            {
                m_spumPrefab.PlayAnimation(state, animationIndex);
                m_spumPrefab._anim.Play(state.ToString(), 0, 0f);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TimingCatchCharacterView] {state} 애니메이션 재생에 실패했습니다: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// [기능]: 캐릭터가 초기화된 경우 첫 번째 IDLE 애니메이션을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 반응 종료 후 안전한 대기 상태 복귀 추가.
        /// </summary>
        private void ApplyIdleAnimation()
        {
            if (m_isInitialized == false || m_spumPrefab.IDLE_List.Count == 0)
            {
                return;
            }

            PlayAnimation(PlayerState.IDLE, 0);
        }
        #endregion

        #region 피격 효과 (Hit Effect)
        /// <summary>
        /// [기능]: 지정 앵커에 피격 효과 프리팹을 하나만 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 실패 판정 피격 효과 생성 추가.
        /// </summary>
        private void SpawnHitEffect()
        {
            if (m_hitEffectPrefab == null)
            {
                return;
            }

            Transform effectAnchor = m_hitEffectAnchor;
            if (effectAnchor == null)
            {
                effectAnchor = transform;
            }

            m_activeHitEffect = Instantiate(
                m_hitEffectPrefab,
                effectAnchor.position,
                Quaternion.identity,
                effectAnchor
            );
        }

        /// <summary>
        /// [기능]: 현재 생성된 피격 효과를 실행 환경에 맞는 방식으로 제거합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: PlayMode와 EditMode 공용 효과 정리 추가.
        /// </summary>
        private void CleanupHitEffect()
        {
            if (m_activeHitEffect == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(m_activeHitEffect);
            }
            else
            {
                DestroyImmediate(m_activeHitEffect);
            }

            m_activeHitEffect = null;
        }
        #endregion
    }
}
