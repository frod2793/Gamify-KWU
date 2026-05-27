using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.ClawMachine;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 4지선다 객관식 버튼 조작과 문제 출제 시 시각 피드백 연출을 전담하는 클래식 퀴즈 뷰 컴포넌트
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizClassicView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("텍스트 매쉬 프로 (TMPro)")]
        [SerializeField]
        [Tooltip("출제될 퀴즈 문제를 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField]
        [Tooltip("남은 시간을 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_timeText;

        [Header("선택지 조작 버튼군")]
        [SerializeField]
        [Tooltip("4지선다 객관식 선택지 버튼 배열입니다 (반드시 4개 할당 필요).")]
        private Button[] m_choiceButtons;

        [Header("결과 연출용 팝업")]
        [SerializeField]
        [Tooltip("정답 클리어 및 최종 오답 실패 결과를 연출할 결과 패널 뷰입니다.")]
        private ClawGameResultPopupView m_resultPopup;

        #endregion

        #region 내부 필드 (Private Fields)

        private QuizClassicViewModel m_viewModel;
        private Color m_originalQuestionColor;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            if (m_questionText != null)
            {
                // [버그 수정]: 에셋 프리팹 결함으로 알파가 0인 상태를 완전히 복원하기 위해 강제 1.0f 불투명 적용
                Color color = m_questionText.color;
                color.a = 1f;
                m_questionText.color = color;
                m_originalQuestionColor = color;
            }
            else
            {
                m_originalQuestionColor = Color.white;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        public void Initialize(QuizClassicViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 1. 뷰모델 데이터 이벤트 바인딩 (타이머 및 스코어 기능 연쇄 업데이트 재활성화)
            m_viewModel.OnNextQuizLoaded += HandleNextQuizLoaded;
            m_viewModel.OnStateChanged += HandleStateChanged;
            m_viewModel.OnTimeChanged += UpdateTimeUI;

            // 2. 피드백 연쇄 시각 효과 연동
            m_viewModel.OnQuizSuccess += HandleQuizSuccess;
            m_viewModel.OnQuizFailed += HandleQuizFailed;

            // 3. 버튼 리스너 바인딩 (인덱스 캡처 방지용 내부 Scope 변수 사용)
            if (m_choiceButtons != null && m_choiceButtons.Length == 4)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        int index = i; // Closure 복사
                        m_choiceButtons[i].onClick.AddListener(() => func_OnChoiceButtonClick(index));
                    }
                }
            }
            else
            {
                Debug.LogError("[QuizClassicView] m_choiceButtons 크기가 4가 아닙니다! 인스펙터를 확인하세요.");
            }

            // 4. 결과 연계 공유 패널 초기화 (DIP 컨텍스트 인터페이스 주입)
            if (m_resultPopup != null)
            {
                m_resultPopup.Initialize(m_viewModel);
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnNextQuizLoaded -= HandleNextQuizLoaded;
                m_viewModel.OnStateChanged -= HandleStateChanged;
                m_viewModel.OnTimeChanged -= UpdateTimeUI;
                m_viewModel.OnQuizSuccess -= HandleQuizSuccess;
                m_viewModel.OnQuizFailed -= HandleQuizFailed;
                m_viewModel.Dispose();
            }

            if (m_choiceButtons != null)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].onClick.RemoveAllListeners();
                    }
                }
            }

            // DOTween 정리
            DOTween.Kill(m_questionText);
        }

        #endregion



        #region 버튼 클릭 이벤트 핸들러 (Public Methods)

        /// <summary>
        /// [기능]: 사용자가 객관식 선택 버튼을 터치했을 때 뷰모델로 인덱스 데이터를 전달합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnChoiceButtonClick(int choiceIndex)
        {
            if (m_viewModel != null)
            {
                m_viewModel.func_SelectAnswer(choiceIndex);
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        private void HandleNextQuizLoaded(QuizData quiz, List<string> choices)
        {
            // A. 질문 텍스트 출력 복구
            if (m_questionText != null)
            {
                m_questionText.text = quiz.Question;
                m_questionText.color = m_originalQuestionColor;
                m_questionText.transform.localScale = Vector3.one;
            }

            // B. 4개 선택지 버튼에 텍스트 주입 및 활성화
            if (m_choiceButtons != null && m_choiceButtons.Length == 4 && choices.Count == 4)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].interactable = true;
                        
                        TextMeshProUGUI btnTxt = m_choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (btnTxt != null)
                        {
                            btnTxt.text = choices[i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [기능]: 뷰모델의 상태 변화에 따라 객관식 버튼들의 상호작용 및 팝업 표시 여부를 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 마지막 문제 정답 시 OnQuizSuccess와 Result 상태 진입에 의해 발생하던 이중 팝업 호출 방지를 위해 Result 진입 시의 중복 팝업 호출 로직 제거
        /// </summary>
        private void HandleStateChanged(QuizStateType state)
        {
            // 게임 가동 상태에 따라 버튼 인터랙션 제어
            bool isPlayable = (state == QuizStateType.Playing);
            
            if (m_choiceButtons != null)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].interactable = isPlayable;
                    }
                }
            }
        }

        private void HandleQuizSuccess()
        {
            if (m_questionText == null)
            {
                return;
            }

            DOTween.Kill(m_questionText);
            
            // 질문 텍스트 초록색 피드백 및 스케일 튕김(Punch) 효과 (지문 내용 보존)
            m_questionText.DOColor(new Color(0.2f, 0.9f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);
            m_questionText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.5f, 8, 1f);
        }

        private void HandleQuizFailed()
        {
            if (m_questionText == null)
            {
                return;
            }

            DOTween.Kill(m_questionText);

            // 질문 텍스트 빨간색 피드백 및 좌우 흔들림(Shake) 효과 (지문 내용 보존)
            m_questionText.DOColor(new Color(0.9f, 0.2f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);
            m_questionText.transform.DOShakePosition(0.5f, new Vector3(8f, 0f, 0f), 12, 90f);
        }

        /// <summary>
        /// [기능]: 뷰모델로부터 실시간 남은 제한시간을 전달받아 클래식 퀴즈 UI 텍스트에 출력합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateTimeUI(float timeLeft)
        {
            if (m_timeText != null)
            {
                m_timeText.text = $"남은 시간: {Mathf.CeilToInt(timeLeft)}초";
            }
        }

        #endregion
    }
}
