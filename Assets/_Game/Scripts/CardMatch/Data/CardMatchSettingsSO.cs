using UnityEngine;
using GameArifiction.Player;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 미니게임의 설정값(학점 기준, 미리보기 시간, 그리드 크기 등)을 관리하는 ScriptableObject입니다.
    /// [작성자]: 김지연
    /// </summary>
    [CreateAssetMenu(fileName = "CardMatchSettings", menuName = "GamifyKWU/CardMatch/CardMatchSettings")]
    public class CardMatchSettingsSO : ScriptableObject
    {
        #region 게임 기본 설정 (Game Settings)
        [Header("게임 기본 설정")]
        [Tooltip("미리보기 시간 (초). 게임 시작 시 모든 카드 앞면을 공개하는 시간")]
        [SerializeField] private float m_previewDuration = 3f;

        [Tooltip("총 카드 쌍 수")]
        [SerializeField] private int m_totalPairs = 12;

        [Tooltip("그리드 열 수")]
        [SerializeField] private int m_columns = 3;

        [Tooltip("그리드 행 수")]
        [SerializeField] private int m_rows = 8;

        [Tooltip("매칭 실패 시 카드를 보여주는 시간 (초)")]
        [SerializeField] private float m_matchFailDelay = 1f;

        [Tooltip("매칭 성공 시 이펙트 표시 후 대기 시간 (초)")]
        [SerializeField] private float m_matchSuccessDelay = 0.5f;
        #endregion

        #region 학점 판정 기준 (Grade Thresholds)
        [Header("학점 판정 기준 (뒤집기 횟수 상한)")]
        [Tooltip("A 등급 최대 뒤집기 횟수 (이하이면 A 등급)")]
        [SerializeField] private int m_gradeA_MaxFlips = 40;

        [Tooltip("B 등급 최대 뒤집기 횟수")]
        [SerializeField] private int m_gradeB_MaxFlips = 48;

        [Tooltip("C 등급 최대 뒤집기 횟수")]
        [SerializeField] private int m_gradeC_MaxFlips = 56;

        [Tooltip("D 등급 최대 뒤집기 횟수")]
        [SerializeField] private int m_gradeD_MaxFlips = 60;
        #endregion

        #region 학점별 멘트 (Grade Messages)
        [Header("학점별 멘트")]
        [SerializeField] private string m_gradeA_Message = "자네, 대학원 생각 없나?";
        [SerializeField] private string m_gradeB_Message = "오, 제법 잘했는걸?";
        [SerializeField] private string m_gradeC_Message = "조금 더 노력하시게.";
        [SerializeField] private string m_gradeD_Message = "음... 공부는 한 건가?";
        [SerializeField] private string m_gradeF_Message = "자네는 학사경고일세.";
        #endregion

        #region Properties
        public float PreviewDuration => m_previewDuration;
        public int TotalPairs => m_totalPairs;
        public int Columns => m_columns;
        public int Rows => m_rows;
        public float MatchFailDelay => m_matchFailDelay;
        public float MatchSuccessDelay => m_matchSuccessDelay;
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 뒤집기 횟수를 기반으로 학점 및 멘트를 산출합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="flipCount">총 뒤집기 횟수</param>
        /// <returns>학점 등급 및 대응 멘트 튜플</returns>
        public (MinigameGrade grade, string message) EvaluateGrade(int flipCount)
        {
            if (flipCount <= m_gradeA_MaxFlips)
            {
                return (MinigameGrade.A, m_gradeA_Message);
            }
            if (flipCount <= m_gradeB_MaxFlips)
            {
                return (MinigameGrade.B, m_gradeB_Message);
            }
            if (flipCount <= m_gradeC_MaxFlips)
            {
                return (MinigameGrade.C, m_gradeC_Message);
            }
            if (flipCount <= m_gradeD_MaxFlips)
            {
                return (MinigameGrade.D, m_gradeD_Message);
            }
            return (MinigameGrade.F, m_gradeF_Message);
        }
        #endregion
    }
}
