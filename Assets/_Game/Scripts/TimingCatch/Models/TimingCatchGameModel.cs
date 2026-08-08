using System;

namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchGameModel
    {
        private readonly TimingCatchGameConfigSO m_config;
        private readonly float[] m_roundTripSeconds;
        private readonly float[] m_greatZoneWidths;
        private int m_currentTurn;
        private float m_gaugePosition;
        private float m_currentSpeed;
        private float m_turnElapsed;
        private bool m_isRunning;
        private int m_direction = 1;

        public TimingCatchGameModel(TimingCatchGameConfigSO config)
        {
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_roundTripSeconds = config.CreateDifficultyRoundTripSecondsSnapshot();
            m_greatZoneWidths = config.CreateDifficultyGreatZoneWidthsSnapshot();
            ResetState();
        }

        public int CurrentTurn => m_currentTurn;
        public int MaxTurnCount => m_config.TotalTurnCount;
        public int CurrentRound => m_currentTurn / m_config.TurnsPerRound;
        public int CurrentRoundNumber => CurrentRound + 1;
        public int CurrentTurnInRound => m_currentTurn % m_config.TurnsPerRound;
        public int CurrentTurnNumber => CurrentTurnInRound + 1;
        public TimingCatchDifficulty CurrentDifficulty => (TimingCatchDifficulty)CurrentTurnInRound;
        public float GaugeNormalized => m_gaugePosition;
        public float CurrentSpeed => m_currentSpeed;
        public float CurrentRoundTripSeconds => m_roundTripSeconds[(int)CurrentDifficulty];
        public float CurrentGreatZoneWidth => m_greatZoneWidths[(int)CurrentDifficulty];
        public float CurrentGreatZoneHalfWidth => CurrentGreatZoneWidth * .5f;
        public float TurnElapsed => m_turnElapsed;
        public float TurnTimeoutSeconds => m_config.TurnTimeoutSeconds;
        public bool IsRunning => m_isRunning;
        public bool IsFinished => m_currentTurn >= m_config.TotalTurnCount;
        public bool IsTurnTimeout => m_config.TurnTimeoutSeconds > 0f && m_turnElapsed >= m_config.TurnTimeoutSeconds;

        public void ResetState()
        {
            m_currentTurn = 0;
            m_gaugePosition = 0f;
            m_currentSpeed = GetSpeed(CurrentDifficulty);
            m_turnElapsed = 0f;
            m_isRunning = true;
            m_direction = 1;
        }

        public void UpdateGauge(float deltaTime)
        {
            if (!m_isRunning || deltaTime <= 0f) return;
            m_turnElapsed += deltaTime;
            float remaining = m_currentSpeed * deltaTime;
            while (remaining > 0f)
            {
                if (m_direction > 0)
                {
                    float distance = 1f - m_gaugePosition;
                    if (remaining <= distance) { m_gaugePosition += remaining; break; }
                    m_gaugePosition = 1f;
                    remaining -= distance;
                    m_direction = -1;
                }
                else
                {
                    float distance = m_gaugePosition;
                    if (remaining <= distance) { m_gaugePosition -= remaining; break; }
                    m_gaugePosition = 0f;
                    remaining -= distance;
                    m_direction = 1;
                }
            }
        }

        public void AdvanceToNextTurn()
        {
            m_currentTurn++;
            m_turnElapsed = 0f;
            if (IsFinished)
            {
                m_isRunning = false;
                return;
            }
            m_currentSpeed = GetSpeed(CurrentDifficulty);
            m_gaugePosition = 0f;
            m_direction = 1;
            m_isRunning = true;
        }

        private float GetSpeed(TimingCatchDifficulty difficulty)
        {
            float trip = m_roundTripSeconds[(int)difficulty];
            return trip > 0f ? 2f / trip : 1f;
        }
    }
}
