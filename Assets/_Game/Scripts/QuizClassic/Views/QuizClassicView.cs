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
                m_originalQuestionColor = m_questionText.color;
                // [버그 수정]: 씬 배치 프리팹 에셋 상의 알파값 유실 방지. 알파가 0에 가깝다면 강제로 1.0f(완전 불투명)로 원복 보정합니다.
                if (m_originalQuestionColor.a < 0.05f)
                {
                    m_originalQuestionColor.a = 1f;
                }
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

            // 1. 뷰모델 데이터 이벤트 바인딩 (타이머 및 스코어 기능 비활성화로 구독 배제)
            m_viewModel.OnNextQuizLoaded += HandleNextQuizLoaded;
            m_viewModel.OnStateChanged += HandleStateChanged;

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
                Debug.Log($"[QuizClassicView] 플레이어가 {choiceIndex + 1}번 선택지 버튼을 터치했습니다.");
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

            // 결과 및 재수강 창 진입 시 팝업 가이드
            if (state == QuizStateType.Result)
            {
                if (m_resultPopup != null)
                {
                    m_resultPopup.func_ShowPopup(true); // 클리어 성공 패널 오픈
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
            
            // 질문 텍스트 초록색 피드백 및 스케일 튕김(Punch) 효과
            m_questionText.text = "★ 정답입니다! 다음 문제로 넘어갑니다 ★";
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

            // 질문 텍스트 빨간색 피드백 및 좌우 흔들림(Shake) 효과
            m_questionText.text = "⚠ 오답입니다! 스테이지 실패 ⚠";
            m_questionText.DOColor(new Color(0.9f, 0.2f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);
            m_questionText.transform.DOShakePosition(0.5f, new Vector3(8f, 0f, 0f), 12, 90f);
        }

        #endregion
    }
}
