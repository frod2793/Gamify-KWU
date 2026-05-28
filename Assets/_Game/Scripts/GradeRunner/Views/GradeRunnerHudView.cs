using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 실시간 HUD 요소(ss:SS 타이머바, 성적 프로그래스바 및 등급 텍스트, 플레이어 위치 비례 스코어 피드백)를 렌더링하고 연출하는 UI View
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class GradeRunnerHudView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("타이머 UI")]
        [SerializeField]
        [Tooltip("남은 시간을 시각적으로 채우는 슬라이더 컴포넌트입니다.")]
        private Slider m_timeSlider;

        [SerializeField]
        [Tooltip("남은 시간을 ss:SS 형식으로 출력할 텍스트 메쉬 프로입니다.")]
        private TextMeshProUGUI m_timeText;

        [Header("성적 UI")]
        [SerializeField]
        [Tooltip("현재 학점을 100% 기준으로 채우는 슬라이더 컴포넌트입니다.")]
        private Slider m_gradeSlider;

        [SerializeField]
        [Tooltip("성적 슬라이더의 채우기(Fill) 이미지 컴포넌트입니다 (구간별 색상 변경 목적).")]
        private Image m_gradeFillImage;

        [SerializeField]
        [Tooltip("현재 실시간 학점 알파벳 등급(A/B/C/D/F)을 출력하는 텍스트입니다.")]
        private TextMeshProUGUI m_gradeLetterText;

        [Header("피드백 연출")]
        [SerializeField]
        [Tooltip("점수가 증가(+1) 또는 감소(-0.5)할 때 화면에 띄울 텍스트 프리팹입니다.")]
        private GameObject m_feedbackTextPrefab;

        [SerializeField]
        [Tooltip("피드백 텍스트들이 동적으로 추가되어 렌더링될 HUD 캔버스 내의 부모 Transform입니다.")]
        private Transform m_feedbackContainer;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        
        // 피드백 텍스트 풀링
        private readonly List<TextMeshProUGUI> m_feedbackTextPool = new List<TextMeshProUGUI>(8);

        // 타이머 원본 색상 및 폰트 스타일 보존 필드
        private Color m_originalTimeColor;
        private FontStyles m_originalTimeStyle;
        
        // 캐싱된 메인 카메라
        private UnityEngine.Camera m_mainCamera;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            m_mainCamera = UnityEngine.Camera.main;
            if (m_timeText != null)
            {
                m_originalTimeColor = m_timeText.color;
                m_originalTimeStyle = m_timeText.fontStyle;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 뷰모델 의존성 이벤트들을 바인딩하고 스코어바 구간별 초기 상태 색상을 지정합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;

            if (m_viewModel != null)
            {
                m_viewModel.OnTimeChanged += UpdateTimerUI;
                m_viewModel.OnGradePointChanged += UpdateGradePointUI;
                m_viewModel.OnGradeLetterChanged += UpdateGradeLetterUI;
                m_viewModel.OnScoreFeedback += HandleScoreFeedback;
            }

            Debug.Log("[GradeRunnerHudView] HUD UI 뷰 초기화 및 데이터 바인딩 성공.");
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnTimeChanged -= UpdateTimerUI;
                m_viewModel.OnGradePointChanged -= UpdateGradePointUI;
                m_viewModel.OnGradeLetterChanged -= UpdateGradeLetterUI;
                m_viewModel.OnScoreFeedback -= HandleScoreFeedback;
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// [기능]: 남은 시간을 수신받아 ss:SS (초:밀리초 2자리) 포맷으로 변환 출력하며 슬라이더 밸류를 동기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateTimerUI(float timeLeft)
        {
            if (m_timeSlider != null)
            {
                float duration = m_viewModel != null ? m_viewModel.GameDuration : 30f;
                m_timeSlider.value = timeLeft / duration;
            }

            if (m_timeText != null)
            {
                int seconds = Mathf.FloorToInt(timeLeft);
                int centiseconds = Mathf.FloorToInt((timeLeft - seconds) * 100f);

                // ss:SS (ms 2자리 제한) 출력
                m_timeText.text = string.Format("{0:D2}:{1:D2}", seconds, centiseconds);

                // [기획 표 기믹 연동]: 10초 이하 시 Bold 폰트스타일 및 빨간색 교체
                if (timeLeft <= 10f)
                {
                    m_timeText.color = Color.red;
                    m_timeText.fontStyle = FontStyles.Bold;
                }
                else
                {
                    m_timeText.color = m_originalTimeColor;
                    m_timeText.fontStyle = m_originalTimeStyle;
                }
            }
        }

        /// <summary>
        /// [기능]: 학점 수치를 기준으로 슬라이더를 갱신하며, 등급 구간에 맞는 프리미엄 톤으로 채우기(Fill) 색상을 역동적으로 변화시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateGradePointUI(float gradePoint)
        {
            if (m_gradeSlider != null)
            {
                float max = m_viewModel != null ? m_viewModel.MaxGradePoint : 5f;
                m_gradeSlider.value = gradePoint / max;
            }

            // 성적 구간에 따른 채우기 이미지 색상 변화 (A:골드 / B:에메랄드 / C:시크블루 / D:오렌지 / F:코랄레드)
            if (m_gradeFillImage != null)
            {
                if (gradePoint >= 5f)
                {
                    m_gradeFillImage.color = GradeRunnerColorPalette.GradeA;
                }
                else if (gradePoint >= 3f)
                {
                    m_gradeFillImage.color = GradeRunnerColorPalette.GradeB;
                }
                else if (gradePoint >= 2f)
                {
                    m_gradeFillImage.color = GradeRunnerColorPalette.GradeC;
                }
                else if (gradePoint >= 1f)
                {
                    m_gradeFillImage.color = GradeRunnerColorPalette.GradeD;
                }
                else
                {
                    m_gradeFillImage.color = GradeRunnerColorPalette.GradeF;
                }
            }
        }

        /// <summary>
        /// [기능]: 뷰모델로부터 등급 문자를 전달받아 스크린 화면에 갱신 출력합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateGradeLetterUI(string gradeLetter)
        {
            if (m_gradeLetterText != null)
            {
                m_gradeLetterText.text = gradeLetter;
            }
        }

        /// <summary>
        /// [기능]: 학점이 변경되었을 때 충돌 지점의 스크린 좌표를 얻어 플레이어 주변에 1초간 (+1.0 / -0.5) 텍스트를 위로 띄워 소멸시키는 DOTween 연출을 구현합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleScoreFeedback(float delta, Vector2 hitWorldPos)
        {
            if (m_feedbackTextPrefab == null || m_feedbackContainer == null || m_mainCamera == null)
            {
                return;
            }

            // 충돌한 월드 공간의 좌표를 화면 UI 스크린 공간 좌표로 변환
            Vector3 screenPos = m_mainCamera.WorldToScreenPoint(hitWorldPos);

            // 풀에서 텍스트 컴포넌트 획득
            TextMeshProUGUI textComp = GetOrCreateFeedbackText();
            if (textComp == null)
            {
                return;
            }

            textComp.transform.position = screenPos;
            textComp.gameObject.SetActive(true);

            // 텍스트 출력 문구 및 색상 적용
            if (delta > 0f)
            {
                textComp.text = $"+{delta:F1}";
                textComp.color = GradeRunnerColorPalette.GradeB; // Sleek Green
            }
            else if (delta < 0f)
            {
                textComp.text = $"{delta:F1}";
                textComp.color = GradeRunnerColorPalette.GradeF; // sleek red
            }
            else
            {
                // Full 상태에서 먹었을 시
                textComp.text = "FULL";
                textComp.color = GradeRunnerColorPalette.GradeA; // 골드
            }

            // DOTween 1초 팝업 플로팅 페이드 연출
            textComp.transform.DOKill();
            textComp.DOKill();

            textComp.transform.DOMoveY(screenPos.y + 70f, 1f).SetEase(Ease.OutQuad);
            textComp.DOFade(0f, 1f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                textComp.gameObject.SetActive(false);
            });
        }

        #endregion

        #region 내부 헬퍼 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 가비지(GC) 억제를 위해 이터레이터 루프를 인덱스 형태로 돌아 풀링에서 휴면 상태인 텍스트 객체를 찾아 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private TextMeshProUGUI GetOrCreateFeedbackText()
        {
            int poolSize = m_feedbackTextPool.Count;
            for (int i = 0; i < poolSize; i++)
            {
                TextMeshProUGUI elem = m_feedbackTextPool[i];
                if (elem != null && !elem.gameObject.activeSelf)
                {
                    // 알파 복원 세팅 후 반환
                    Color col = elem.color;
                    col.a = 1f;
                    elem.color = col;
                    return elem;
                }
            }

            // 풀에 없는 경우 새로 인스턴스화
            GameObject newGo = Instantiate(m_feedbackTextPrefab, m_feedbackContainer);
            if (newGo != null)
            {
                TextMeshProUGUI textComp = newGo.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    m_feedbackTextPool.Add(textComp);
                    return textComp;
                }
            }

            return null;
        }

        #endregion
    }
}
