using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 씬의 물리적 참조 및 씬 오브젝트 인스턴스들을 관리하고 전달하기 위한 DTO 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawSceneReferencesDTO
    {
        #region 내부 필드 (Private Fields)
        private readonly Transform m_dollsContainer;
        private readonly BoxCollider2D m_spawnAreaCollider;
        private readonly GameObject m_capsulePrefab;
        private readonly GameObject m_clawMachineWorld;
        #endregion

        #region 공개 프로퍼티 (Public Properties)
        public Transform DollsContainer
        {
            get
            {
                return m_dollsContainer;
            }
        }

        public BoxCollider2D SpawnAreaCollider
        {
            get
            {
                return m_spawnAreaCollider;
            }
        }

        public GameObject CapsulePrefab
        {
            get
            {
                return m_capsulePrefab;
            }
        }

        public GameObject ClawMachineWorld
        {
            get
            {
                return m_clawMachineWorld;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 씬 참조 객체들을 주입받아 DTO를 생성하는 생성자입니다.
        /// [작성자]: 윤승종
        /// </summary>
        public ClawSceneReferencesDTO(
            Transform dollsContainer, 
            BoxCollider2D spawnAreaCollider, 
            GameObject capsulePrefab, 
            GameObject clawMachineWorld)
        {
            m_dollsContainer = dollsContainer;
            m_spawnAreaCollider = spawnAreaCollider;
            m_capsulePrefab = capsulePrefab;
            m_clawMachineWorld = clawMachineWorld;
        }
        #endregion
    }
}
