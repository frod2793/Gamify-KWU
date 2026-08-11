using UnityEngine;

namespace GameArifiction.TimingCatch
{
    [System.Serializable]
    public sealed class TimingCatchRoundPresentation
    {
        [SerializeField] private string[] m_greatDialogueByTurn = new string[3];

        public string GetGreatDialogue(int turnIndex)
        {
            return m_greatDialogueByTurn != null && turnIndex >= 0 && turnIndex < m_greatDialogueByTurn.Length
                ? m_greatDialogueByTurn[turnIndex] ?? string.Empty
                : string.Empty;
        }
    }

    [CreateAssetMenu(fileName = "TimingCatchConfigSO", menuName = "Gamify-KWU/TimingCatchConfigSO")]
    public sealed class TimingCatchGameConfigSO : ScriptableObject
    {
        private const float SafeDefaultRoundTrip = 2f;
        private const float SafeDefaultGreatWidth = .2f;

        [Header("PDF rules")]
        [SerializeField] private int m_roundCount = 4;
        [SerializeField] private int m_turnsPerRound = 3;
        [SerializeField] private float[] m_difficultyRoundTripSeconds = { 2f, 1.5f, 1f };
        [SerializeField] private float[] m_difficultyGreatZoneWidths = { .2f, .15f, .1f };
        [SerializeField] private float m_turnTimeoutSeconds = 6f;
        [SerializeField] private float m_easyNormalIntermissionSeconds = 1f;
        [SerializeField] private float m_hardIntermissionSeconds = 2f;
        [SerializeField] private int[] m_difficultyGreatScores = { 50, 100, 200 };
        [SerializeField] private int m_threeGreatBonus = 150;
        [SerializeField] private float m_gradeAThresholdRatio = .9f;
        [SerializeField] private float m_gradeBThresholdRatio = .75f;
        [SerializeField] private float m_gradeCThresholdRatio = .6f;
        [SerializeField] private float m_gradeDThresholdRatio = .45f;

        [Header("Presentation")]
        [SerializeField] private float m_introLineDuration = 2f;
        [SerializeField] private float m_roundStartDuration = 1f;
        [SerializeField] private float m_judgeResultDuration = 1f;
        [SerializeField] private float m_outroLineDuration = 2f;
        [SerializeField] private float[] m_starScales = { .2f, .5f, 1f };
        [SerializeField] private string m_greatSfxPath = string.Empty;
        [SerializeField] private string[] m_introDialogue =
        {
            "안녕하세요 발표자 우니입니다.",
            "발표 시작하겠습니다."
        };
        [SerializeField] private TimingCatchRoundPresentation[] m_roundPresentations =
        {
            new TimingCatchRoundPresentation(), new TimingCatchRoundPresentation(),
            new TimingCatchRoundPresentation(), new TimingCatchRoundPresentation()
        };
        [SerializeField] private string m_missDialogue = "아.. 이건 어.. @!#$% 입니다..";
        [SerializeField] private string[] m_outroDialogue =
        {
            "이상으로 발표를 마치겠습니다.",
            "감사합니다."
        };

        public int RoundCount => Mathf.Max(1, m_roundCount);
        public int TurnsPerRound => Mathf.Max(1, m_turnsPerRound);
        public int TotalTurnCount => RoundCount * TurnsPerRound;
        public float TurnTimeoutSeconds => m_turnTimeoutSeconds > 0f ? m_turnTimeoutSeconds : 6f;
        public float EasyNormalIntermissionSeconds => Mathf.Max(0f, m_easyNormalIntermissionSeconds);
        public float HardIntermissionSeconds => Mathf.Max(0f, m_hardIntermissionSeconds);
        public int ThreeGreatBonus => Mathf.Max(0, m_threeGreatBonus);
        public float GradeAThresholdRatio => m_gradeAThresholdRatio;
        public float GradeBThresholdRatio => m_gradeBThresholdRatio;
        public float GradeCThresholdRatio => m_gradeCThresholdRatio;
        public float GradeDThresholdRatio => m_gradeDThresholdRatio;
        public float IntroLineDuration => Mathf.Max(0f, m_introLineDuration);
        public float RoundStartDuration => Mathf.Max(0f, m_roundStartDuration);
        public float JudgeResultDuration => Mathf.Max(0f, m_judgeResultDuration);
        public float OutroLineDuration => Mathf.Max(0f, m_outroLineDuration);
        public string GreatSfxPath => m_greatSfxPath ?? string.Empty;
        public string MissDialogue => m_missDialogue ?? string.Empty;

        public int MaxPossibleScore
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < 3; i++) sum += GetDifficultyGreatScore((TimingCatchDifficulty)i);
                return RoundCount * (sum + ThreeGreatBonus);
            }
        }

        public float[] CreateDifficultyRoundTripSecondsSnapshot()
        {
            return CopyDifficultyValues(m_difficultyRoundTripSeconds, SafeDefaultRoundTrip);
        }

        public float[] CreateDifficultyGreatZoneWidthsSnapshot()
        {
            var values = CopyDifficultyValues(m_difficultyGreatZoneWidths, SafeDefaultGreatWidth);
            for (int i = 0; i < values.Length; i++) values[i] = Mathf.Clamp(values[i], 0f, 1f);
            return values;
        }

        public int[] CreateDifficultyGreatScoresSnapshot()
        {
            var result = new int[3];
            for (int i = 0; i < result.Length; i++) result[i] = GetDifficultyGreatScore((TimingCatchDifficulty)i);
            return result;
        }

        public int GetDifficultyGreatScore(TimingCatchDifficulty difficulty)
        {
            int index = Mathf.Clamp((int)difficulty, 0, 2);
            if (m_difficultyGreatScores == null || m_difficultyGreatScores.Length <= index) return 50 + index * 50;
            return Mathf.Max(0, m_difficultyGreatScores[index]);
        }

        public float GetGreatZoneWidth(TimingCatchDifficulty difficulty)
        {
            int index = Mathf.Clamp((int)difficulty, 0, 2);
            float value = m_difficultyGreatZoneWidths != null && m_difficultyGreatZoneWidths.Length > index
                ? m_difficultyGreatZoneWidths[index]
                : SafeDefaultGreatWidth;
            return Mathf.Clamp(value, 0f, 1f);
        }

        public float GetStarScale(int consecutiveGreat)
        {
            int index = consecutiveGreat - 1;
            if (m_starScales == null || index < 0 || index >= m_starScales.Length) return 0f;
            return Mathf.Max(0f, m_starScales[index]);
        }

        public string GetIntroDialogue(int index)
        {
            return GetDialogue(m_introDialogue, index);
        }

        public string GetGreatDialogue(int roundIndex, int turnIndex)
        {
            return m_roundPresentations != null && roundIndex >= 0 && roundIndex < m_roundPresentations.Length && m_roundPresentations[roundIndex] != null
                ? m_roundPresentations[roundIndex].GetGreatDialogue(turnIndex)
                : string.Empty;
        }

        public string GetOutroDialogue(int index)
        {
            return GetDialogue(m_outroDialogue, index);
        }

        private static string GetDialogue(string[] dialogue, int index)
        {
            return dialogue != null && index >= 0 && index < dialogue.Length ? dialogue[index] ?? string.Empty : string.Empty;
        }

        private static float[] CopyDifficultyValues(float[] source, float fallback)
        {
            var result = new float[3];
            for (int i = 0; i < result.Length; i++)
            {
                float value = source != null && source.Length > i ? source[i] : fallback;
                result[i] = value > 0f ? value : fallback;
            }
            return result;
        }
    }
}
