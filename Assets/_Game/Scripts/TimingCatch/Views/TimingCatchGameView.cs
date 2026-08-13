using Cysharp.Threading.Tasks;
using GamifyKWU.UI.Utils;
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

        [Header("Dialogue")]
        [SerializeField] private Image m_dialogueBubble;

        [Header("Buttons")]
        [SerializeField] private Button m_timingButton;


        private TimingCatchGameViewModel m_viewModel;
        private TypewriterComponent m_typewriter;
        private string m_lastDialogue;
        private int m_lastTurnTotal = -1;
        private int m_lastScore = -1;
        private int m_lastBonusScore = -1;
        private int m_lastSlideIndex = -1;
        private float m_lastZoneHalfWidth = -1f;
        private float m_lastStarScale = -1f;
        private TimingCatchJudgeType m_lastJudgeType = (TimingCatchJudgeType)(-1);

        private void Awake()
        {
            TryResolveSerializedReferences();
            if (m_backgroundImage != null) m_backgroundImage.preserveAspect = false;
            if (m_starImage != null) m_starImage.preserveAspect = true;
            if (m_timingButton != null) m_timingButton.onClick.AddListener(func_OnTimingButtonPressed);
            if (m_dialogueText != null)
            {
                m_typewriter = m_dialogueText.GetComponent<TypewriterComponent>();
                if (m_typewriter == null) m_typewriter = m_dialogueText.gameObject.AddComponent<TypewriterComponent>();
                if (m_dialogueBubble != null) m_typewriter.LayoutToRebuild = m_dialogueBubble.rectTransform;
            }
            SetDialogue(string.Empty);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) func_OnTimingButtonPressed();
        }

        private void OnDestroy()
        {
            if (m_timingButton != null) m_timingButton.onClick.RemoveListener(func_OnTimingButtonPressed);
            if (m_typewriter != null) m_typewriter.StopTyping();
            if (m_viewModel == null) return;
            m_viewModel.OnStateChanged -= func_OnStateChanged;
        }

        public void Initialize(TimingCatchGameViewModel viewModel)
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= func_OnStateChanged;
            }
            m_viewModel = viewModel;
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged += func_OnStateChanged;
                m_viewModel.NotifyState();
            }
        }

        public void func_OnTimingButtonPressed()
        {
            if (m_viewModel != null) m_viewModel.EvaluateInput();
        }

        // 게이지 포인터를 제외한 나머지 UI는 값이 바뀔 때만 갱신한다.
        // (매 프레임 문자열 할당·TMP 메시 재생성·레이아웃 dirty 방지 — 모바일 GC/배터리 대응)
        private void func_OnStateChanged(TimingCatchGameState state)
        {
            float zoneHalfWidth = state.GreatZoneWidth * .5f;
            if (zoneHalfWidth != m_lastZoneHalfWidth)
            {
                m_lastZoneHalfWidth = zoneHalfWidth;
                UpdateJudgeZone(m_greatZone, zoneHalfWidth);
            }
            UpdateGaugePointer(state.Gauge);
            if (m_timingButton != null) m_timingButton.interactable = state.InputEnabled;
            if (state.CurrentTurnTotal != m_lastTurnTotal)
            {
                m_lastTurnTotal = state.CurrentTurnTotal;
                SetText(m_turnText, $"TURN {state.CurrentTurnTotal}/{state.TotalTurnCount}");
            }
            if (state.Score != m_lastScore)
            {
                m_lastScore = state.Score;
                SetScore(state.Score);
            }
            if (state.JudgeType != m_lastJudgeType)
            {
                m_lastJudgeType = state.JudgeType;
                SetText(m_judgeText, GetJudgeText(state.JudgeType));
            }
            if (state.BonusScore != m_lastBonusScore)
            {
                m_lastBonusScore = state.BonusScore;
                SetText(m_bonusText, state.BonusScore > 0 ? $"BONUS +{state.BonusScore}" : string.Empty);
            }
            SetDialogue(state.Dialogue);
            SetStarScale(state.StarScale);
            if (m_slideSprites != null && m_slideSprites.Length > 0)
            {
                int slideIndex = GetSlideIndex(state);
                if (slideIndex != m_lastSlideIndex)
                {
                    m_lastSlideIndex = slideIndex;
                    if (m_slideImage != null) m_slideImage.sprite = m_slideSprites[slideIndex];
                }
            }
        }

        private static string GetJudgeText(TimingCatchJudgeType judgeType)
        {
            switch (judgeType)
            {
                case TimingCatchJudgeType.Great: return "GREAT";
                case TimingCatchJudgeType.Miss: return "MISS";
                default: return string.Empty;
            }
        }

        private static int GetSlideIndex(TimingCatchGameState state)
        {
            if (state.Phase == TimingCatchPhase.Intro) return 0;
            if (state.Phase == TimingCatchPhase.Outro || state.Phase == TimingCatchPhase.Completed) return 5;
            return Mathf.Clamp(state.CurrentRound, 1, 4);
        }

        private void UpdateJudgeZone(RectTransform zone, float halfWidth)
        {
            if (zone == null) return;
            float half = Mathf.Clamp(halfWidth, 0f, .5f);
            Vector2 min = zone.anchorMin;
            Vector2 max = zone.anchorMax;
            Vector2 anchoredPosition = zone.anchoredPosition;
            Vector2 sizeDelta = zone.sizeDelta;
            min.x = .5f - half;
            max.x = .5f + half;
            zone.anchorMin = min;
            zone.anchorMax = max;
            zone.anchoredPosition = new Vector2(0f, anchoredPosition.y);
            zone.sizeDelta = new Vector2(0f, sizeDelta.y);
        }

        private float m_lastGauge = -1f;

        private void UpdateGaugePointer(float gauge)
        {
            if (gauge == m_lastGauge) return;
            m_lastGauge = gauge;
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

        private void SetScore(int score)
        {
            if (m_scoreText != null) m_scoreText.SetText("Score {0:0000}", score);
        }

        private void SetStarScale(float scale)
        {
            if (m_starImage == null || scale == m_lastStarScale) return;
            m_lastStarScale = scale;
            bool isVisible = scale > 0f;
            if (m_starImage.gameObject.activeSelf != isVisible) m_starImage.gameObject.SetActive(isVisible);
            if (isVisible) m_starImage.rectTransform.localScale = Vector3.one * scale;
        }

        private void SetDialogue(string dialogue)
        {
            string targetDialogue = dialogue ?? string.Empty;
            if (m_lastDialogue == targetDialogue) return;
            m_lastDialogue = targetDialogue;

            bool isVisible = !string.IsNullOrEmpty(targetDialogue);
            if (m_dialogueBubble != null) m_dialogueBubble.gameObject.SetActive(isVisible);
            if (m_dialogueText != null) m_dialogueText.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                if (m_typewriter != null) m_typewriter.StopTyping();
                return;
            }

            if (m_typewriter != null)
            {
                m_typewriter.StopTyping();
                m_typewriter.PlayTypingEffectAsync(targetDialogue).Forget();
                return;
            }

            SetText(m_dialogueText, targetDialogue);
        }
    }
}
