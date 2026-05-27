using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 세션 데이터(마지막 위치 등)를 유지하고 공유하기 위한 ScriptableObject 데이터 에셋 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 추후 확장성을 고려하여 미니게임 플레이 결과(등급) 저장 로직 추가

    /// <summary>
    /// 미니게임 플레이 결과 등급입니다.
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

    /// <summary>
    /// 미니게임 고유 ID와 등급을 매핑하는 직렬화용 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct MinigameRecord
    {
        public string MinigameId;
        public MinigameGrade Grade;
    }

    /// <summary>
    /// [기능]: 플레이어의 세션 데이터(마지막 위치 등)를 유지하고 공유하기 위한 ScriptableObject 데이터 에셋 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 최초 생성 및 마지막 저장 위치 관리 프로퍼티 구현 및 미니게임 결과 저장 기능 추가
    /// </summary>
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
        [Tooltip("각 미니게임의 플레이 결과(등급)를 저장하는 리스트입니다. (추후 확장을 고려해 ID 문자열 기반으로 관리)")]
        private List<MinigameRecord> m_minigameRecords = new List<MinigameRecord>();

        [SerializeField]
        [Tooltip("미니게임 시작 후 현재까지 누적 소요된 총 플레이 시간(초)입니다.")]
        private float m_totalMinigamePlayTime = 0f;
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

        public IReadOnlyList<MinigameRecord> MinigameRecords => m_minigameRecords;
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 보관된 마지막 플레이어 위치 데이터 정보를 클리어(초기화)합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ResetData()
        {
            m_lastPosition = Vector2.zero;
            m_hasSavedPosition = false;
            m_minigameRecords.Clear();
            m_totalMinigamePlayTime = 0f;
        }

        /// <summary>
        /// [기능]: 특정 미니게임의 결과를 기록하거나 업데이트합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="minigameId">미니게임의 고유 문자열 ID (예: "ClawMachine", "Quiz" 등)</param>
        /// <param name="grade">달성한 랭크 (A, B, C, D)</param>
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

            // 존재하지 않으면 새로 추가
            m_minigameRecords.Add(new MinigameRecord { MinigameId = minigameId, Grade = grade });
        }

        /// <summary>
        /// [기능]: 특정 미니게임의 저장된 등급을 반환합니다. 플레이한 적 없으면 None을 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
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
