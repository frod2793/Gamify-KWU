using UnityEngine;
using GameArifiction.Player;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 미니게임의 성적(MinigameGrade)에 따른 등급 스프라이트 및 느낌표 스프라이트를 한 곳에서 공통 관리(Flyweight)하기 위한 ScriptableObject 자산입니다.
    /// [작성자]: 윤승종
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameGradeSprites", menuName = "GamifyKWU/MinigameGradeSprites")]
    public class MinigameGradeSpritesSO : ScriptableObject
    {
        #region UI 참조 (Inspector)

        [Header("등급별 스프라이트")]
        [SerializeField]
        [Tooltip("A 학점 등급의 스프라이트입니다.")]
        private Sprite m_spriteA;

        [SerializeField]
        [Tooltip("B 학점 등급의 스프라이트입니다.")]
        private Sprite m_spriteB;

        [SerializeField]
        [Tooltip("C 학점 등급의 스프라이트입니다.")]
        private Sprite m_spriteC;

        [SerializeField]
        [Tooltip("D 학점 등급의 스프라이트입니다.")]
        private Sprite m_spriteD;

        [SerializeField]
        [Tooltip("F 학점 등급의 스프라이트입니다.")]
        private Sprite m_spriteF;

        [Header("특수 연출 스프라이트")]
        [SerializeField]
        [Tooltip("최초 플레이 시 성적 대신 표시할 느낌표 스프라이트입니다.")]
        private Sprite m_exclamationSprite;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public Sprite ExclamationSprite => m_exclamationSprite;

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 학점 등급(MinigameGrade)에 해당하는 스프라이트 에셋을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 최초 작성
        /// </summary>
        public Sprite GetSprite(MinigameGrade grade)
        {
            switch (grade)
            {
                case MinigameGrade.A:
                    return m_spriteA;
                case MinigameGrade.B:
                    return m_spriteB;
                case MinigameGrade.C:
                    return m_spriteC;
                case MinigameGrade.D:
                    return m_spriteD;
                case MinigameGrade.F:
                    return m_spriteF;
                default:
                    return null;
            }
        }

        #endregion
    }
}
