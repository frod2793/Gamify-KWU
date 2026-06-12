using System;
using GameArifiction.Player;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 최종 결과 모델의 데이터를 가공하여 뷰에 제공하고, 확인 커맨드를 처리하는 ViewModel 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultViewModel
    {
        #region 내부 필드 (Private Fields)
        private readonly FinalResultModel m_model;
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: 계산된 최종 등급과 교수 멘트를 뷰에 전달하는 이벤트입니다.
        /// </summary>
        public event Action<MinigameGrade, string> OnGradeUpdated;

        /// <summary>
        /// [기능]: 사용자가 결과 확인을 마쳤을 때(엔딩 연출 진입) 발생하는 이벤트입니다.
        /// </summary>
        public event Action OnEndingRequested;
        #endregion

        #region 초기화 (Initialization)
        public FinalResultViewModel(FinalResultModel model)
        {
            m_model = model;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 모델을 통해 최종 등급을 산출하고, 뷰가 표시할 수 있도록 이벤트를 발생시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void CalculateAndPublishGrade()
        {
            MinigameGrade finalGrade = m_model.CalculateFinalGrade();
            string professorMessage = GetProfessorMessage(finalGrade);
            
            OnGradeUpdated?.Invoke(finalGrade, professorMessage);
        }

        /// <summary>
        /// [기능]: 결과 팝업에서 확인 버튼을 클릭했을 때 호출되는 커맨드 메서드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ConfirmResultCommand()
        {
            OnEndingRequested?.Invoke();
        }
        #endregion

        #region 내부 로직 (Private Methods)
        /// <summary>
        /// [기능]: 최종 등급에 따른 교수 멘트를 반환합니다.
        /// </summary>
        private string GetProfessorMessage(MinigameGrade grade)
        {
            switch (grade)
            {
                case MinigameGrade.A: return "자네, 대학원 생각은 없나?";
                case MinigameGrade.B: return "오, 제법 잘했는걸?";
                case MinigameGrade.C: return "조금 더 노력하시게.";
                case MinigameGrade.D: return "음.. 공부는 한 건가?";
                case MinigameGrade.F: return "자네는 학사경고일세.";
                default: return "수고했네.";
            }
        }
        #endregion
    }
}
