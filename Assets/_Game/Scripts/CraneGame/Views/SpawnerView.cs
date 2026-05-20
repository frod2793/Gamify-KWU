using System.Collections.Generic;
using GamifyKWU.CraneGame.Data;
using GamifyKWU.CraneGame.ViewModel;
using GameArifiction.Claw;
using UnityEngine;

namespace GamifyKWU.CraneGame.View
{
    /// <summary>
    /// 퀴즈 데이터에 맞춰 캡슐을 생성 및 관리하는 Spawner
    /// </summary>
    public class SpawnerView : MonoBehaviour
    {
        #region Fields
        [Header("Prefabs")]
        [SerializeField] 
        [Tooltip("스폰할 캡슐의 프리팹입니다.")]
        private CapsuleView m_capsulePrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] 
        [Tooltip("캡슐이 생성될 영역의 최소 범위를 지정하는 트랜스폼입니다.")]
        private Transform m_spawnAreaMin;

        [SerializeField] 
        [Tooltip("캡슐이 생성될 영역의 최대 범위를 지정하는 트랜스폼입니다.")]
        private Transform m_spawnAreaMax;
        
        private CraneGameViewModel m_viewModel;
        private List<CapsuleView> m_activeCapsules = new List<CapsuleView>();
        #endregion

        #region Public Methods
        public void BindViewModel(CraneGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnQuizLoaded += SpawnQuizCapsules;
        }
        #endregion

        #region Private Methods
        private void SpawnQuizCapsules(QuizData quiz)
        {
            ClearExistingCapsules();

            // 1. 정답 캡슐 생성
            CreateCapsule(quiz.CorrectAnswer);

            // 2. 오답 캡슐들 생성 (GC 최적화를 위해 for 루프 사용)
            int wrongCount = quiz.WrongAnswers.Count;
            for (int i = 0; i < wrongCount; i++)
            {
                CreateCapsule(quiz.WrongAnswers[i]);
            }
            
            Debug.Log($"[Spawner] 총 {wrongCount + 1}개의 캡슐 생성 완료.");
        }

        private void CreateCapsule(string text)
        {
            if (m_capsulePrefab == null)
            {
                return;
            }

            Vector3 spawnPos = GetRandomSpawnPosition();
            CapsuleView capsule = Instantiate(m_capsulePrefab, spawnPos, Quaternion.identity, transform);
            capsule.Setup(text);
            
            m_activeCapsules.Add(capsule);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(m_spawnAreaMin.position.x, m_spawnAreaMax.position.x);
            float y = Random.Range(m_spawnAreaMin.position.y, m_spawnAreaMax.position.y);
            return new Vector3(x, y, 0f);
        }

        private void ClearExistingCapsules()
        {
            // 리스트 역순 순회하며 제거 (for 루프 준수)
            for (int i = m_activeCapsules.Count - 1; i >= 0; i--)
            {
                if (m_activeCapsules[i] != null)
                {
                    m_activeCapsules[i].DestroySelf();
                }
            }
            m_activeCapsules.Clear();
        }
        #endregion
    }
}
