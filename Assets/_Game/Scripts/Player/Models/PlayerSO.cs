using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 세션 데이터(마지막 위치 등)를 유지하고 공유하기 위한 ScriptableObject 데이터 에셋 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-12
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: PlayerPrefs 영구 저장 입출력 로직 전면 배제 및 성적 갱신 방어 코드 적용
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
            set
            {
                m_totalMinigamePlayTime = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public Vector2 LastPosition
        {
            get => m_lastPosition;
            set
            {
                m_lastPosition = value;
                m_hasSavedPosition = true;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool HasSavedPosition
        {
            get => m_hasSavedPosition;
            set
            {
                m_hasSavedPosition = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool IsIntroPlayed
        {
            get => m_isIntroPlayed;
            set
            {
                m_isIntroPlayed = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// [기능]: 등록된 유효한 3개 미니게임("CardMatch", "CraneGame", "GradeRunner")이 모두 클리어(None이 아님)되었는지 여부를 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public bool IsAllMinigamesCleared
        {
            get
            {
                bool isCardMatchCleared = GetMinigameGrade("CardMatch") != MinigameGrade.None;
                bool isCraneGameCleared = GetMinigameGrade("CraneGame") != MinigameGrade.None;
                bool isGradeRunnerCleared = GetMinigameGrade("GradeRunner") != MinigameGrade.None;
                return isCardMatchCleared && isCraneGameCleared && isGradeRunnerCleared;
            }
        }

        public IReadOnlyList<MinigameRecord> MinigameRecords => m_minigameRecords;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 오브젝트 로딩 시 로직 (더 이상 로컬 디스크 복구를 실행하지 않음)
        /// [작성자]: 윤승종
        /// </summary>
        private void OnEnable()
        {
            // [삭제]: LoadFromLocal() 영구 불러오기 배제
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 플레이어 데이터를 모두 초기화하고 메모리 데이터를 리셋합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void ResetData()
        {
            m_lastPosition = Vector2.zero;
            m_hasSavedPosition = false;
            m_minigameRecords.Clear();
            m_totalMinigamePlayTime = 0f;
            m_isIntroPlayed = false;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// [기능]: 미니게임 성적을 저장합니다. 기존 기록이 있고 새 성적이 더 높은 경우에만 갱신됩니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 대소문자 구분 없는 비교 및 중복 제거 적용하되 영구 저장(Prefs) 배제
        /// </summary>
        public void SetMinigameGrade(string minigameId, MinigameGrade grade)
        {
            if (string.IsNullOrEmpty(minigameId) || grade == MinigameGrade.None)
            {
                return;
            }

            MinigameRecord targetRecord = default;
            bool hasRecord = false;
            int targetIndex = -1;

            // 1. 대소문자 무시 비교 및 중복 레코드 제거 (방어적 설계)
            for (int i = 0; i < m_minigameRecords.Count; i++)
            {
                if (string.Equals(m_minigameRecords[i].MinigameId, minigameId, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasRecord)
                    {
                        targetRecord = m_minigameRecords[i];
                        hasRecord = true;
                        targetIndex = i;
                    }
                    else
                    {
                        m_minigameRecords.RemoveAt(i);
                        i--;
                    }
                }
            }

            // 2. 최고 학점 보존 여부 판단
            if (hasRecord)
            {
                MinigameGrade currentGrade = targetRecord.Grade;
                bool isNewGradeBetter = false;

                if (currentGrade == MinigameGrade.None)
                {
                    isNewGradeBetter = true;
                }
                else
                {
                    isNewGradeBetter = ((int)grade < (int)currentGrade);
                }

                if (isNewGradeBetter)
                {
                    targetRecord.Grade = grade;
                    m_minigameRecords[targetIndex] = targetRecord;
                    Debug.Log($"[PlayerSO] 미니게임 '{minigameId}' 성적이 더 높은 성적으로 갱신되었습니다: {currentGrade} -> {grade}");
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
                else
                {
                    Debug.Log($"[PlayerSO] 미니게임 '{minigameId}' 기존 성적({currentGrade})이 새 성적({grade})보다 높거나 같아 갱신을 생략합니다.");
                }
            }
            else
            {
                m_minigameRecords.Add(new MinigameRecord { MinigameId = minigameId, Grade = grade });
                Debug.Log($"[PlayerSO] 미니게임 '{minigameId}'의 첫 성적이 기록되었습니다: {grade}");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// [기능]: 특정 미니게임의 성적을 조회합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public MinigameGrade GetMinigameGrade(string minigameId)
        {
            for (int i = 0; i < m_minigameRecords.Count; i++)
            {
                if (string.Equals(m_minigameRecords[i].MinigameId, minigameId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return m_minigameRecords[i].Grade;
                }
            }
            return MinigameGrade.None;
        }

        /// <summary>
        /// [기능]: 로컬 저장소 세이브 기능 무력화 (더는 사용하지 않음)
        /// [작성자]: 윤승종
        /// </summary>
        public void SaveToLocal()
        {
            // [삭제]: 로컬 저장 기능 배제
        }

        /// <summary>
        /// [기능]: 로컬 저장소 로드 기능 무력화 (더는 사용하지 않음)
        /// [작성자]: 윤승종
        /// </summary>
        public void LoadFromLocal()
        {
            // [삭제]: 로컬 불러오기 기능 배제
        }
        #endregion

        #region 내부 클래스 (Private Class)
        /// <summary>
        /// [기능]: PlayerSO의 데이터를 로컬 디스크에 직렬화하기 위해 속성들을 감싸는 데이터 구조 클래스
        /// </summary>
        [System.Serializable]
        private class PlayerSODataWrapper
        {
            public List<MinigameRecord> MinigameRecords;
            public float TotalMinigamePlayTime;
            public bool IsIntroPlayed;
            public Vector2 LastPosition;
            public bool HasSavedPosition;
        }
        #endregion
    }
}
