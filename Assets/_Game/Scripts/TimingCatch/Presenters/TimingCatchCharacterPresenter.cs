using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 판정을 캐릭터 성공·실패 반응 명령으로 변환합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchCharacterPresenter : IStartable, IDisposable
    {
        #region 내부 필드 (Private Fields)
        private bool m_isSubscribed;
        #endregion

        #region 주입 프로퍼티 (Injected Properties)
        /// <summary>
        /// [기능]: 타이밍 판정 이벤트를 제공하는 소스입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 이벤트 프로퍼티 주입 계약 추가.
        /// </summary>
        [Inject]
        public ITimingCatchJudgeEventSource JudgeEventSource { get; set; }

        /// <summary>
        /// [기능]: 판정에 대응하는 캐릭터 반응을 실행할 View입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 캐릭터 반응 View 프로퍼티 주입 계약 추가.
        /// </summary>
        [Inject]
        public ITimingCatchCharacterView CharacterView { get; set; }
        #endregion

        #region VContainer 생명주기 (VContainer Lifecycle)
        /// <summary>
        /// [기능]: 판정 이벤트를 구독하여 캐릭터 반응 연결을 시작합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 이벤트 구독과 중복 구독 방어 추가.
        /// </summary>
        public void Start()
        {
            if (m_isSubscribed || JudgeEventSource == null)
            {
                return;
            }

            JudgeEventSource.OnJudgeEvaluated += HandleJudgeEvaluated;
            m_isSubscribed = true;
        }

        /// <summary>
        /// [기능]: 판정 이벤트 구독을 해제하여 캐릭터 반응 연결을 종료합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 이벤트 구독 해제와 중복 해제 방어 추가.
        /// </summary>
        public void Dispose()
        {
            if (m_isSubscribed == false || JudgeEventSource == null)
            {
                return;
            }

            JudgeEventSource.OnJudgeEvaluated -= HandleJudgeEvaluated;
            m_isSubscribed = false;
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: Great은 성공 반응으로, Miss는 실패 반응으로 전달합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 판정별 캐릭터 반응 라우팅 추가.
        /// </summary>
        private void HandleJudgeEvaluated(TimingCatchJudgeType judge)
        {
            if (CharacterView == null)
            {
                return;
            }

            if (judge == TimingCatchJudgeType.Great)
            {
                CharacterView.PlaySuccessReaction();
                return;
            }

            if (judge == TimingCatchJudgeType.Miss)
            {
                CharacterView.PlayFailureReaction();
                return;
            }

            Debug.LogWarning($"[TimingCatchCharacterPresenter] 지원하지 않는 판정이 전달되었습니다: {judge}");
        }
        #endregion
    }
}
