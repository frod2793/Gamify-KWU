using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 퀴즈 질문을 출력하고, 정답/오답 시 DOTween을 활용한 극적 시각 연출을 전담하는 UI View
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizUI_View : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("퀴즈 질문을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField]
        [Tooltip("정답/오답 시 텍스트 연출이 일어날 텍스트 컨테이너 RectTransform입니다.")]
        private RectTransform m_textContainer;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private Color m_originalTextColor;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            if (m_questionText != null)
            {
                m_originalTextColor = m_questionText.color;
            }
            else
            {
                m_originalTextColor = Color.white;
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnQuizSuccess -= HandleQuizSuccess;
                m_viewModel.OnQuizFailed -= HandleQuizFailed;
                m_viewModel.OnStateChanged -= HandleStateChanged;
            }

            if (m_textContainer != null)
            {
                DOTween.Kill(m_textContainer);
            }
            if (m_questionText != null)
            {
                DOTween.Kill(m_questionText);
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 이벤트 구독
            m_viewModel.OnQuizSuccess += HandleQuizSuccess;
            m_viewModel.OnQuizFailed += HandleQuizFailed;
            m_viewModel.OnStateChanged += HandleStateChanged;

            // 초기 문제 세팅
            UpdateQuizUI();
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 뷰모델에 등록된 현재 퀴즈 질문을 텍스트 컴포넌트에 출력하고 시각 요소를 리셋합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateQuizUI()
        {
            if (m_viewModel == null || m_questionText == null)
            {
                return;
            }

            var quiz = m_viewModel.CurrentQuiz;
            if (quiz != null)
            {
                m_questionText.text = quiz.Question;
                m_questionText.color = m_originalTextColor;
                m_questionText.transform.localScale = Vector3.one;
            }
            else
            {
                m_questionText.text = "인형뽑기 문제를 준비 중입니다...";
            }
        }

        private void HandleStateChanged(ClawStateType state)
        {
            // 재수강 등으로 게임이 리셋되어 Idle 상태로 복귀했을 때 퀴즈 UI를 다시 안전하게 업데이트해줍니다.
            if (state == ClawStateType.Idle)
            {
                UpdateQuizUI();
            }
        }

        /// <summary>
        /// [기능]: 정답을 맞췄을 때 텍스트를 초록색으로 물들이며 통통 튕기는(Punch) 애니메이션 연출을 수행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleQuizSuccess()
        {
            if (m_questionText == null)
            {
                return;
            }

            // 트윈 킬
            DOTween.Kill(m_questionText);
            if (m_textContainer != null)
            {
                DOTween.Kill(m_textContainer);
            }

            m_questionText.text = "★ 정답입니다! 스테이지 클리어 ★";
            m_questionText.DOColor(new Color(0.2f, 0.9f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);

            if (m_textContainer != null)
            {
                m_textContainer.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.6f, 10, 1f);
            }
        }

        /// <summary>
        /// [기능]: 오답을 냈을 때 텍스트를 빨간색으로 물들이며 좌우로 심하게 흔들리는(Shake) 애니메이션 연출을 수행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleQuizFailed()
        {
            if (m_questionText == null)
            {
                return;
            }

            // 트윈 킬
            DOTween.Kill(m_questionText);
            if (m_textContainer != null)
            {
                DOTween.Kill(m_textContainer);
            }

            m_questionText.text = "⚠ 오답입니다! 재수강을 고려하세요. ⚠";
            m_questionText.DOColor(new Color(0.9f, 0.2f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);

            if (m_textContainer != null)
            {
                m_textContainer.DOShakePosition(0.6f, new Vector3(15f, 0f, 0f), 15, 90f);
            }
        }
        #endregion
    }
}
