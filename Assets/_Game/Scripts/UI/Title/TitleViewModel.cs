using System;

/// <summary>
/// [기능]: 타이틀 뷰와 모델 간의 상태 및 명령 흐름을 중재하는 뷰모델 클래스입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Title
{
    public class TitleViewModel
    {
        #region 내부 필드 (Private Fields)

        private readonly TitleModel m_model;

        #endregion

        #region 이벤트

        public event Action<TitleToInGameDTO> OnPlayCommandTriggered;

        #endregion

        #region 초기화

        public TitleViewModel(TitleModel model)
        {
            m_model = model;
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 플레이 버튼 클릭 명령을 수신하여 씬 전환 처리를 시작합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-28
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 트랜지션 중복 진입 방지 로직 및 DTO 연계 구현
        /// </summary>
        public void ExecutePlayCommand()
        {
            if (m_model.IsTransitioning)
            {
                return;
            }

            m_model.IsTransitioning = true;

            TitleToInGameDTO transitionData = new TitleToInGameDTO(m_model.TargetSceneName);
            
            if (OnPlayCommandTriggered != null)
            {
                OnPlayCommandTriggered.Invoke(transitionData);
            }
        }

        #endregion
    }
}
