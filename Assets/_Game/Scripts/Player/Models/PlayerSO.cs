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

        public void SetMinigameGrade(string minigameId, MinigameGrade grade)
        {
            for (int i = 0; i < m_minigameRecords.Count; i++)
            {
                if (m_minigameRecords[i].MinigameId == minigameId)
                {
                    MinigameRecord record = m_minigameRecords[i];
                    record.Grade = grade;
                    m_minigameRecords[i] = record;
                    return;
                }
            }

            m_minigameRecords.Add(new MinigameRecord { MinigameId = minigameId, Grade = grade });
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
