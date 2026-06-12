using System;
using UnityEngine;
using GameArifiction.Player;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 3종 미니게임(CardMatch, CraneGame, GradeRunner)의 성적을 기반으로 최종 등급을 산출하는 도메인 모델 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultModel
    {
        #region 내부 필드 (Private Fields)
        private readonly PlayerSO m_playerSO;
        private readonly string[] m_targetMinigameIds = { "CardMatch", "CraneGame", "GradeRunner" };
        #endregion

        #region 초기화 (Initialization)
        public FinalResultModel(PlayerSO playerSO)
        {
            m_playerSO = playerSO;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 대상 미니게임 3종의 등급을 환산하여 평균을 산출하고 반올림하여 최종 MinigameGrade를 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public MinigameGrade CalculateFinalGrade()
        {
            if (m_playerSO == null)
            {
                Debug.LogError("[FinalResultModel] PlayerSO가 존재하지 않아 기본값(F)을 반환합니다.");
                return MinigameGrade.F;
            }

            int totalScore = 0;

            for (int i = 0; i < m_targetMinigameIds.Length; i++)
            {
                MinigameGrade grade = m_playerSO.GetMinigameGrade(m_targetMinigameIds[i]);
                totalScore += ConvertGradeToScore(grade);
            }

            // 평균 계산 및 반올림
            float averageScore = (float)totalScore / m_targetMinigameIds.Length;
            int finalScore = Mathf.RoundToInt(averageScore);

            return ConvertScoreToGrade(finalScore);
        }
        #endregion

        #region 내부 로직 (Private Methods)
        /// <summary>
        /// [기능]: 등급을 점수(숫자)로 환산합니다. (A=1, B=2, C=3, D=4, F=5, 기록없음=5)
        /// </summary>
        private int ConvertGradeToScore(MinigameGrade grade)
        {
            if (grade == MinigameGrade.None)
            {
                return 5; // 미수행은 F로 간주
            }
            else if (grade == MinigameGrade.A)
            {
                return 1;
            }
            else if (grade == MinigameGrade.B)
            {
                return 2;
            }
            else if (grade == MinigameGrade.C)
            {
                return 3;
            }
            else if (grade == MinigameGrade.D)
            {
                return 4;
            }
            else
            {
                return 5;
            }
        }

        /// <summary>
        /// [기능]: 점수(숫자)를 등급으로 환산합니다.
        /// </summary>
        private MinigameGrade ConvertScoreToGrade(int score)
        {
            if (score <= 1)
            {
                return MinigameGrade.A;
            }
            else if (score == 2)
            {
                return MinigameGrade.B;
            }
            else if (score == 3)
            {
                return MinigameGrade.C;
            }
            else if (score == 4)
            {
                return MinigameGrade.D;
            }
            else
            {
                return MinigameGrade.F;
            }
        }
        #endregion
    }
}
