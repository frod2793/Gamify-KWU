using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchGameView : MonoBehaviour
    {
        [Header("Timing Catch UI")]
        [SerializeField] private Slider m_gaugeSlider;
        [SerializeField] private RectTransform m_greatZone;
        [SerializeField] private RectTransform m_gaugePointer;
        [SerializeField] private Image m_cursorImage;
        [SerializeField] private Image m_backgroundImage;
        [SerializeField] private Image m_starImage;
        [SerializeField] private Sprite[] m_slideSprites;
        [SerializeField] private Image m_slideImage;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI m_turnText;
        [SerializeField] private TextMeshProUGUI m_scoreText;
        [SerializeField] private TextMeshProUGUI m_judgeText;
        [SerializeField] private TextMeshProUGUI m_dialogueText;
        [SerializeField] private TextMeshProUGUI m_bonusText;

        [Header("Buttons")]
        [SerializeField] private Button m_timingButton;


        private TimingCatchGameViewModel m_viewModel;

        private void Awake()
        {
            TryResolveSerializedReferences();
            if (m_backgroundImage != null) m_backgroundImage.preserveAspect = false;
            if (m_starImage != null) m_starImage.preserveAspect = true;
            if (m_timingButton != null) m_timingButton.onClick.AddListener(func_OnTimingButtonPressed);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) func_OnTimingButtonPressed();
        }

        private void OnDestroy()
        {
            if (m_timingButton != null) m_timingButton.onClick.RemoveListener(func_OnTimingButtonPressed);
            if (m_viewModel == null) return;
            m_viewModel.OnStateChanged -= func_OnStateChanged;
            m_viewModel.OnJudgeEvaluated -= func_OnJudgeEvaluated;
        }

        public void Initialize(TimingCatchGameViewModel viewModel)
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= func_OnStateChanged;
                m_viewModel.OnJudgeEvaluated -= func_OnJudgeEvaluated;
            }
            m_viewModel = viewModel;
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged += func_OnStateChanged;
                m_viewModel.OnJudgeEvaluated += func_OnJudgeEvaluated;
                m_viewModel.NotifyState();
            }
        }

        public void func_OnTimingButtonPressed()
        {
            if (m_viewModel != null) m_viewModel.EvaluateInput();
        }

        private void func_OnStateChanged(TimingCatchGameState state)
        {
            RectTransform zone = m_greatZone;
            UpdateJudgeZone(zone, state.GreatZoneWidth * .5f);
            UpdateGaugePointer(state.Gauge);
            if (m_timingButton != null) m_timingButton.interactable = state.InputEnabled;
            SetText(m_turnText, $"TURN {state.CurrentTurnTotal}/12");
            SetText(m_scoreText, $"SCORE {state.Score}");
            SetText(m_bonusText, state.GreatBonusCount > 0 ? $"BONUS +{state.GreatBonusCount * 150}" : string.Empty);
            SetText(m_dialogueText, state.IsIntermission ? "준비!" : string.Empty);
            if (m_slideSprites != null && m_slideSprites.Length > 0)
            {
                int slideIndex = GetSlideIndex(state);
                if (m_slideImage != null) m_slideImage.sprite = m_slideSprites[slideIndex];
            }
        }

        private void func_OnJudgeEvaluated(TimingCatchJudgeType judgeType) => SetText(m_judgeText, judgeType == TimingCatchJudgeType.Great ? "Great" : "Miss");

        private static int GetSlideIndex(TimingCatchGameState state)
        {
            if ((state.IsFinished || state.IsIntermission) && state.CurrentTurnTotal >= 12) return 5;
            if (state.CurrentRound <= 1 && state.CurrentTurnTotal <= 1) return 0;
            return Mathf.Clamp(state.CurrentRound, 1, 4);
        }

        private void UpdateJudgeZone(RectTransform zone, float halfWidth)
        {
            if (zone == null) return;
            float half = Mathf.Clamp(halfWidth, 0f, .5f);
            Vector2 min = zone.anchorMin;
            Vector2 max = zone.anchorMax;
            min.x = .5f - half;
            max.x = .5f + half;
            zone.anchorMin = min;
            zone.anchorMax = max;
        }

        private void UpdateGaugePointer(float gauge)
        {
            RectTransform pointer = m_cursorImage != null ? m_cursorImage.rectTransform : m_gaugePointer;
            if (pointer == null) return;
            Vector2 anchor = new Vector2(Mathf.Clamp01(gauge), .5f);
            pointer.anchorMin = anchor;
            pointer.anchorMax = anchor;
            pointer.anchoredPosition = Vector2.zero;
        }

        private void TryResolveSerializedReferences()
        {
            if (m_gaugeSlider == null) m_gaugeSlider = GetComponentInChildren<Slider>(true);
            if (m_gaugeSlider != null)
            {
                if (m_greatZone == null) m_greatZone = m_gaugeSlider.transform.Find("GreatZone") as RectTransform;
                if (m_gaugePointer == null) m_gaugePointer = m_gaugeSlider.transform.Find("Cursor") as RectTransform;
                if (m_gaugePointer == null) m_gaugePointer = m_gaugeSlider.transform.Find("GaugePointer") as RectTransform;
            }
            if (m_cursorImage == null && m_gaugePointer != null) m_cursorImage = m_gaugePointer.GetComponent<Image>();
            if (m_timingButton == null) m_timingButton = GetComponentInChildren<Button>(true);
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
