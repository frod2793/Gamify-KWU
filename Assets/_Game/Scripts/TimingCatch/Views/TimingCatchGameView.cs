using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameArifiction.UI.Common;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임 HUD 바인딩/입력 전달을 담당하는 뷰 컴포넌트.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("HUD")]
        [SerializeField]
        [Tooltip("좌우 게이지 표시 슬라이더.")]
        private Slider m_gaugeSlider;

        [SerializeField]
        [Tooltip("Good 판정 가능 영역을 표시하는 RectTransform.")]
        private RectTransform m_goodZone;

        [SerializeField]
        [Tooltip("Perfect 판정 가능 영역을 표시하는 RectTransform.")]
        private RectTransform m_perfectZone;

        [SerializeField]
        [Tooltip("현재 게이지 위치를 표시하는 포인터 RectTransform.")]
        private RectTransform m_gaugePointer;

        [SerializeField]
        [Tooltip("상태 텍스트(점수, 스테이지).")]
        private TextMeshProUGUI m_stateText;

        [SerializeField]
        [Tooltip("판정 힌트 텍스트(Perfect/Good/Miss).")]
        private TextMeshProUGUI m_judgeText;

        [Header("버튼")]
        [SerializeField]
        [Tooltip("타이밍 입력 버튼(모바일 하단 우측에 배치).")]
        private Button m_timingButton;

        [SerializeField]
        [Tooltip("설정 버튼(팝업 오픈).")]
        private Button m_settingsButton;

        [Header("공통 UI")]
        [SerializeField]
        private CommonSettingsPopupView m_settingsPopupView;
        #endregion

        #region 내부 필드 (Private Fields)
        private TimingCatchGameViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            TryResolveSerializedReferences();

            if (m_timingButton != null)
            {
                m_timingButton.onClick.AddListener(func_OnTimingButtonPressed);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(func_OnOpenSettings);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                func_OnTimingButtonPressed();
            }
        }

        private void OnDestroy()
        {
            if (m_timingButton != null)
            {
                m_timingButton.onClick.RemoveListener(func_OnTimingButtonPressed);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.RemoveListener(func_OnOpenSettings);
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= func_OnStateChanged;
                m_viewModel.OnJudgeEvaluated -= func_OnJudgeEvaluated;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 뷰모델을 바인딩하고 이벤트를 구독하여 즉시 UI를 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
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

        /// <summary>
        /// [기능]: 인스펙터 연결이 누락된 UI 레퍼런스를 이름 기반으로 임시 대체 바인딩합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 대모 씬 구성 중 누락 참조 방어 로직 추가.
        /// </summary>
        private void TryResolveSerializedReferences()
        {
            if (m_gaugeSlider == null)
            {
                m_gaugeSlider = GetComponentInChildren<Slider>(true);
            }

            if (m_gaugeSlider != null)
            {
                if (m_goodZone == null)
                {
                    m_goodZone = m_gaugeSlider.transform.Find("GoodZone") as RectTransform;
                }

                if (m_perfectZone == null)
                {
                    m_perfectZone = m_gaugeSlider.transform.Find("PerfectZone") as RectTransform;
                }

                if (m_gaugePointer == null)
                {
                    m_gaugePointer = m_gaugeSlider.transform.Find("GaugePointer") as RectTransform;
                }
            }

            if (m_stateText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts != null && texts.Length > 0)
                {
                    m_stateText = texts[0];
                }
            }

            if (m_judgeText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts != null && texts.Length > 1)
                {
                    m_judgeText = texts[1];
                }
            }

            if (m_timingButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                if (buttons != null && buttons.Length > 0)
                {
                    m_timingButton = buttons[0];
                }
            }

            if (m_settingsButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i] != m_timingButton)
                    {
                        m_settingsButton = buttons[i];
                        break;
                    }
                }
            }

            if (m_settingsPopupView == null)
            {
                m_settingsPopupView = FindAnyObjectByType<CommonSettingsPopupView>();
            }
        }

        /// <summary>
        /// [기능]: 모바일 버튼 이벤트 바인딩용 메서드(Perfect 판정 입력 전달).
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 키보드 Space와 동기 동작.
        /// </summary>
        public void func_OnTimingButtonPressed()
        {
            if (m_viewModel == null)
            {
                return;
            }

            m_viewModel.EvaluateInput();
        }

        /// <summary>
        /// [기능]: 설정 팝업 버튼을 통해 게임 설정 UI를 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnOpenSettings()
        {
            if (m_settingsPopupView != null)
            {
                m_settingsPopupView.ShowPopup();
            }
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        private void func_OnStateChanged(TimingCatchGameState state)
        {
            if (m_gaugeSlider != null)
            {
                m_gaugeSlider.value = state.Gauge;
            }

            UpdateJudgeZone(m_goodZone, state.GoodWindow);
            UpdateJudgeZone(m_perfectZone, state.PerfectWindow);
            UpdateGaugePointer(state.Gauge);

            if (m_stateText != null)
            {
                m_stateText.text = $"{state.CurrentStage + 1}/{state.MaxStage}  점수:{state.Score}";
            }
        }

        private void func_OnJudgeEvaluated(TimingCatchJudgeType judgeType)
        {
            if (m_judgeText != null)
            {
                if (judgeType == TimingCatchJudgeType.Perfect)
                {
                    m_judgeText.text = "Perfect";
                }
                else if (judgeType == TimingCatchJudgeType.Good)
                {
                    m_judgeText.text = "Good";
                }
                else
                {
                    m_judgeText.text = "Miss";
                }
            }
        }

        /// <summary>
        /// [기능]: 게이지 중앙을 기준으로 판정 허용 오차를 UI 영역 너비로 변환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 스테이지별 Good/Perfect 판정 구간 시각화 추가.
        /// </summary>
        private void UpdateJudgeZone(RectTransform zone, float halfWindow)
        {
            if (zone == null)
            {
                return;
            }

            float clampedHalfWindow = Mathf.Clamp(halfWindow, 0f, 0.5f);
            zone.anchorMin = new Vector2(0.5f - clampedHalfWindow, 0f);
            zone.anchorMax = new Vector2(0.5f + clampedHalfWindow, 1f);
            zone.offsetMin = Vector2.zero;
            zone.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// [기능]: 정규화된 게이지 값을 독립 이동 포인터의 수평 앵커로 반영합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Slider Fill과 분리된 현재 위치 포인터 표시 추가.
        /// </summary>
        private void UpdateGaugePointer(float gauge)
        {
            if (m_gaugePointer == null)
            {
                return;
            }

            float normalizedGauge = Mathf.Clamp01(gauge);
            Vector2 anchor = new Vector2(normalizedGauge, 0.5f);
            m_gaugePointer.anchorMin = anchor;
            m_gaugePointer.anchorMax = anchor;
            m_gaugePointer.anchoredPosition = Vector2.zero;
        }
        #endregion
    }
}
