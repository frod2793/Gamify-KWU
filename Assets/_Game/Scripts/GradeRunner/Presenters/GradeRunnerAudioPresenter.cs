using System;
using UnityEngine;
using VContainer.Unity;
using GameArifiction.Core.Audio;

namespace GameArifiction.GradeRunner
{
    /// <summary>
    /// [기능]: GradeRunner 미니게임의 플레이 상태에 따른 배경음 및 효과음 재생을 총괄 관리하는 중개 오디오 프레젠터 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-08
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 불필요한 단순 확인용 디버그 로그(Debug.Log) 제거/주석화 및 마감 처리
    /// </summary>
    public class GradeRunnerAudioPresenter : IStartable, IDisposable
    {
        #region 내부 의존 필드

        private readonly GradeRunnerViewModel m_viewModel;
        private readonly ISoundService m_soundService;

        #endregion

        #region 초기화 및 생성자

        public GradeRunnerAudioPresenter(GradeRunnerViewModel viewModel, ISoundService soundService)
        {
            m_viewModel = viewModel;
            m_soundService = soundService;
        }

        /// <summary>
        /// [기능]: 프레젠터 기동 시 뷰모델의 상태/사운드 관련 이벤트를 바인딩 구독합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameStateChanged += HandleGameStateChanged;
                m_viewModel.OnPhaseChanged += HandlePhaseChanged;
                m_viewModel.OnScoreFeedback += HandleScoreFeedback;
                // IStartable 실행 순서(레이스 컨디션)에 의해 초기 튜토리얼 전환 이벤트를 놓친 경우를 대비해 수동으로 재생을 보장합니다.
                if (m_viewModel.CurrentState == GradeRunnerState.Tutorial)
                {
                    HandleGameStateChanged(GradeRunnerState.Tutorial);
                }
            }
        }

        /// <summary>
        /// [기능]: 프레젠터 해제 시 구독했던 모든 이벤트를 해제하여 메모리 누수를 방지합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Dispose()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameStateChanged -= HandleGameStateChanged;
                m_viewModel.OnPhaseChanged -= HandlePhaseChanged;
                m_viewModel.OnScoreFeedback -= HandleScoreFeedback;
                m_viewModel.OnPauseStateChanged -= HandlePauseStateChanged;
            }

            if (m_soundService != null)
            {
                m_soundService.StopBGM();
            }
        }

        #endregion

        #region 사운드 이벤트 핸들러

        /// <summary>
        /// [기능]: 뷰모델의 게임 진행 상태에 맞춰 BGM 재생/정지를 제어합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleGameStateChanged(GradeRunnerState state)
        {
            if (m_soundService == null || m_viewModel == null)
            {
                return;
            }

            // 1페이즈 플레이 시작 시 1페이즈 BGM 재생
            if (state == GradeRunnerState.Playing && m_viewModel.CurrentPhase == GradeRunnerPhase.Phase1)
            {
                m_soundService.PlayBGM(SoundDefine.Bgm_graderunner_phase1);
            }
        }

        /// <summary>
        /// [기능]: 공격 페이즈가 변경(2페이즈 진입)될 때 BGM을 2페이즈 BGM으로 교체합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePhaseChanged(GradeRunnerPhase phase)
        {
            if (m_soundService == null)
            {
                return;
            }

            if (phase == GradeRunnerPhase.Phase2)
            {
                // 2페이즈 플레이 진입 시 2페이즈 BGM 재생
                m_soundService.PlayBGM(SoundDefine.Bgm_graderunner_phase2);
            }
        }

        /// <summary>
        /// [기능]: 플레이어의 피격/족보 획득 점수 변동에 따라 상응하는 효과음(SFX)을 재생합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleScoreFeedback(float delta, Vector2 hitWorldPos)
        {
            if (m_soundService == null)
            {
                return;
            }

            if (delta > 0f)
            {
                // 족보 아이템 획득 효과음 재생
                m_soundService.PlaySFX(SoundDefine.Sfx_graderunner_cheatsheet);
            }
            else if (delta < 0f)
            {
                // 코드 장애물 피격 효과음 재생
                m_soundService.PlaySFX(SoundDefine.Sfx_graderunner_hit);
            }
        }

        /// <summary>
        /// [기능]: 일시정지 상태에 따라 BGM을 일시정지시키거나 재개합니다.
        ///         (유저 수동 일시정지 시에만 BGM을 일시정지합니다)
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePauseStateChanged(bool isPaused, bool isUserPause)
        {
            if (m_soundService == null)
            {
                return;
            }

            if (isPaused && isUserPause)
            {
                m_soundService.PauseBGM();
            }
            else if (!isPaused)
            {
                m_soundService.ResumeBGM();
            }
        }

        #endregion
    }
}
