/// <summary>
/// [기능]: ClawMachineDollView가 UI 시각화 및 아이덴티티 판별에 필요한 데이터만 안전하게 넘겨받기 위한 데이터 전송 객체 (DTO)
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.ClawMachine
{
    public struct DollStateDTO
    {
        #region 공개 필드 (Public Fields)

        public string DollId;
        public string AnswerText;
        public bool IsDisagree;
        public bool IsCorrect;

        #endregion

        #region 초기화 (Initialization)

        public DollStateDTO(DollModel model)
        {
            if (model != null)
            {
                DollId = model.DollId;
                AnswerText = model.AnswerText;
                IsDisagree = model.IsDisagree;
                IsCorrect = model.IsCorrect;
            }
            else
            {
                DollId = string.Empty;
                AnswerText = string.Empty;
                IsDisagree = false;
                IsCorrect = false;
            }
        }

        #endregion
    }
}
