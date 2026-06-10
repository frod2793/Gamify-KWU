using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 세션 데이터(마지막 위치 등)를 유지하고 공유하기 위한 ScriptableObject 데이터 에셋 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-28
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 인트로 연출 시청 여부 플래그 추가
    /// </summary>
    public enum MinigameGrade
    {
        None = 0,
        A,
        B,
        C,
        D,
        F
    }

    [System.Serializable]
    public struct MinigameRecord
    {
        public string MinigameId;
        public MinigameGrade Grade;
    }

    [CreateAssetMenu(fileName = "PlayerSO", menuName = "Gamify-KWU/PlayerSO")]
    public class PlayerSO : ScriptableObject
    {
        #region 내부 필드 (Private Fields)
        [SerializeField]
        [Tooltip("플레이어가 마지막으로 기록한 위치 좌표입니다.")]
        private Vector2 m_lastPosition = Vector2.zero;

        [SerializeField]
        [Tooltip("마지막 위치 정보의 유효 여부입니다.")]
        private bool m_hasSavedPosition = false;

        [Header("미니게임 데이터")]
        [SerializeField]
        [Tooltip("각 미니게임의 플레이 결과(등급)를 저장하는 리스트입니다.")]
        private List<MinigameRecord> m_minigameRecords = new List<MinigameRecord>();

        [SerializeField]
        [Tooltip("미니게임 시작 후 현재까지 누적 소요된 총 플레이 시간(초)입니다.")]
        private float m_totalMinigamePlayTime = 0f;

        [SerializeField]
        [Tooltip("최초 플레이 시 노출되는 인트로 연출을 감상했는지 여부입니다.")]
        private bool m_isIntroPlayed = false;
        #endregion

        #region 공개 프로퍼티 (Public Properties)
        public float TotalMinigamePlayTime
        {
            get => m_totalMinigamePlayTime;
            set => m_totalMinigamePlayTime = value;
        }

        public Vector2 LastPosition
        {
            get => m_lastPosition;
            set
            {
                m_lastPosition = value;
                m_hasSavedPosition = true;
            }
        }

        public bool HasSavedPosition
        {
            get => m_hasSavedPosition;
            set
            {
                m_hasSavedPosition = value;
            }
        }

        public bool IsIntroPlayed
        {
            get => m_isIntroPlayed;
            set
            {
                m_isIntroPlayed = value;
            }
        }

        public IReadOnlyList<MinigameRecord> MinigameRecords => m_minigameRecords;
        #endregion

        #region 공개 메서드 (Public Methods)
        public void ResetData()
        {
            m_lastPosition = Vector2.zero;
            m_hasSavedPosition = false;
            m_minigameRecords.Clear();
            m_totalMinigamePlayTime = 0f;
            m_isIntroPlayed = false;
        }

        /// <summary>
        /// [기능]: 미니게임 성적을 저장합니다. 기존 기록이 있고 새 성적이 더 높은 경우에만 갱신됩니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 반복 플레이 시 더 높은 성적으로만 갱신되도록 비교 로직 추가
        /// </summary>
        public void SetMinigameGrade(string minigameId, MinigameGrade grade)
        {
            for (int i = 0; i < m_minigameRecords.Count; i++)
            {
                if (m_minigameRecords[i].MinigameId == minigameId)
                {
                    MinigameGrade currentGrade = m_minigameRecords[i].Grade;
                    bool isNewGradeBetter = false;

                    // 기존 성적이 없는 상태(None)이면 어떤 유효 성적이든 즉각 갱신
                    if (currentGrade == MinigameGrade.None)
                    {
                        isNewGradeBetter = (grade != MinigameGrade.None);
                    }
                    // 기존 성적과 새 성적이 둘 다 존재하면 등급 수치 비교 (A=1이 F=5보다 크므로 정수값이 작을수록 우수)
                    else if (grade != MinigameGrade.None)
                    {
                        isNewGradeBetter = ((int)grade < (int)currentGrade);
                    }

                    if (isNewGradeBetter)
                    {
                        MinigameRecord record = m_minigameRecords[i];
                        record.Grade = grade;
                        m_minigameRecords[i] = record;
                        Debug.Log($"[PlayerSO] 미니게임 '{minigameId}' 성적이 더 높은 성적으로 갱신되었습니다: {currentGrade} -> {grade}");
                    }
                    else
                    {
                        Debug.Log($"[PlayerSO] 미니게임 '{minigameId}' 기존 성적({currentGrade})이 새 성적({grade})보다 높거나 같아 갱신을 생략합니다.");
                    }
                    return;
                }
            }

            m_minigameRecords.Add(new MinigameRecord { MinigameId = minigameId, Grade = grade });
            Debug.Log($"[PlayerSO] 미니게임 '{minigameId}'의 첫 성적이 기록되었습니다: {grade}");
        }

        public MinigameGrade GetMinigameGrade(string minigameId)
        {
            for (int i = 0; i < m_minigameRecords.Count; i++)
            {
                if (m_minigameRecords[i].MinigameId == minigameId)
                {
                    return m_minigameRecords[i].Grade;
                }
            }
            return MinigameGrade.None;
        }
        #endregion
    }
}
