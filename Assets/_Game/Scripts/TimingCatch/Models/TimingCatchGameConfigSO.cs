using UnityEngine;

namespace GameArifiction.TimingCatch
{
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
