using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using GameArifiction.Player;
using GameArifiction.UI.Common;
using GamifyKWU.UI.Utils;
using Cysharp.Threading.Tasks;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 최종 결과 팝업 UI를 바인딩하고 표시하는 View 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultPopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("텍스트")]
        [SerializeField]
        [Tooltip("교수님의 피드백 멘트가 출력될 텍스트입니다.")]
        private TextMeshProUGUI m_professorMessageText;

        [Header("이미지")]
        [SerializeField]
        [Tooltip("최종 등급을 나타낼 이미지 컴포넌트입니다.")]
        private Image m_gradeImage;

        [SerializeField]
        [Tooltip("교수님의 표정/얼굴 이미지 컴포넌트입니다.")]
        private Image m_professorImage;

        [Header("리소스")]
        [SerializeField]
        [Tooltip("등급별 스프라이트 관리를 수행할 ScriptableObject 데이터 자산입니다.")]
        private MinigameGradeSpritesSO m_gradeSpritesSO;
        #endregion

        #region 내부 필드 (Private Fields)
        private FinalResultViewModel m_viewModel;
        private TypewriterComponent m_typewriter;
        #endregion

        #region VContainer 주입 (Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 FinalResultViewModel을 주입받고 이벤트를 구독합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 의존성 주입 및 이벤트 바인딩 설정
        /// </summary>
        [Inject]
        public void Construct(FinalResultViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 이벤트 구독
            m_viewModel.OnGradeUpdated += HandleGradeUpdated;
        }
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 초기화 시 텍스트 콤포넌트에서 TypewriterComponent를 캐싱하거나 추가합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: TypewriterComponent 동적 확보
        /// </summary>
        private void Start()
        {
            if (m_professorMessageText != null)
            {
                m_typewriter = m_professorMessageText.GetComponent<TypewriterComponent>();
                if (m_typewriter == null)
                {
                    m_typewriter = m_professorMessageText.gameObject.AddComponent<TypewriterComponent>();
                }
            }
        }

        /// <summary>
        /// [기능]: 오브젝트 파괴 시 이벤트를 안전하게 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 이벤트 해제
        /// </summary>
        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGradeUpdated -= HandleGradeUpdated;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 팝업을 활성화하고 ViewModel을 통해 등급 계산을 요청합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        public void ShowPopup()
        {
            gameObject.SetActive(true);
            m_viewModel.CalculateAndPublishGrade();
        }

        /// <summary>
        /// [기능]: 외부 디버그/테스트 모듈 등에서 등급과 피드백 멘트를 임의로 주입하여 즉시 결과 팝업을 보여줍니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        /// <param name="grade">출력할 학점 등급</param>
        /// <param name="message">출력할 교수님 피드백 메시지</param>
        public void ShowPopup(MinigameGrade grade, string message)
        {
            gameObject.SetActive(true);
            HandleGradeUpdated(grade, message);
        }

        /// <summary>
        /// [기능]: 팝업을 비활성화하고 타이핑을 중지합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        public void HidePopup()
        {
            if (m_typewriter != null)
            {
                m_typewriter.StopTyping();
            }
            gameObject.SetActive(false);
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: UI 확인 버튼 클릭 시 호출되어 ViewModel에 커맨드를 전달하고 팝업을 닫습니다. 타이핑 연출 중일 경우 타이핑을 즉시 강제 완료시킵니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이핑 도중 클릭 시 스킵 기능 및 즉시 완료 연계 처리
        /// </summary>
        public void func_OnConfirmClick()
        {
            if (m_typewriter != null && m_typewriter.IsTyping)
            {
                m_typewriter.CompleteTypingImmediate();
                return;
            }

            HidePopup();
            m_viewModel.ConfirmResultCommand();
        }

        /// <summary>
        /// [기능]: ViewModel로부터 갱신된 등급과 멘트 데이터를 받아 UI를 업데이트합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이핑 효과 컴포넌트를 연계하여 출력을 연출식으로 전환
        /// </summary>
        private void HandleGradeUpdated(MinigameGrade grade, string message)
        {
            if (m_typewriter != null)
            {
                UniTaskVoid PlayEffect()
                {
                    async UniTask Play()
                    {
                        await m_typewriter.PlayTypingEffectAsync(message);
                    }
                    Play().Forget();
                    return default;
                }
                PlayEffect();
            }
            else if (m_professorMessageText != null)
            {
                m_professorMessageText.text = message;
            }

            if (m_gradeImage != null && m_gradeSpritesSO != null)
            {
                Sprite targetSprite = GetSpriteForGrade(grade);
                if (targetSprite != null)
                {
                    m_gradeImage.sprite = targetSprite;
                }
            }
        }
        #endregion

        #region 내부 로직 (Private Methods)
        /// <summary>
        /// [기능]: MinigameGrade에 해당하는 스프라이트를 반환합니다.
        /// </summary>
        private Sprite GetSpriteForGrade(MinigameGrade grade)
        {
            if (m_gradeSpritesSO != null)
            {
                return m_gradeSpritesSO.GetSprite(grade);
            }
            return null;
        }
        #endregion
    }
}
