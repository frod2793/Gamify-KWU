namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 고유 데이터를 관리하는 순수 Model
    /// [작성자]: 윤승종
    /// </summary>
    public class DollModel
    {
        public string DollId { get; }
        public string DollName { get; }
        public float Weight { get; }

        public DollModel(string dollId, string dollName, float weight)
        {
            DollId = dollId;
            DollName = dollName;
            Weight = weight;
        }
    }
}
