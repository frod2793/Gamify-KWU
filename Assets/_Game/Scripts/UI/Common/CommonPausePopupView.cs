using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 미니게임 공통 일시정지 팝업을 관리하는 View 컴포넌트
    /// [작성자]: 윤승종
    /// </summary>
    public class CommonPausePopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("조작 버튼")]
        [SerializeField]
        [Tooltip("계속하기 버튼")]
        private Button m_resumeButton;

        [SerializeField]
        [Tooltip("튜토리얼 다시보기 버튼")]
        private Button m_replayTutorialButton;

        [SerializeField]
        [Tooltip("퀴즈 문제 다시보기 버튼 (퀴즈가 없는 경우 숨겨짐)")]
        private Button m_replayQuizButton;

        #endregion

        #region 내부 필드

        private CommonPausePopupDataDTO m_popupData;

        #endregion

        #region 유니티 생명주기

        /// <summary>
        /// [기능]: 팝업 조작 버튼 리스너 바인딩을 수행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Late Awake 자가 꺼짐 버그 제거를 위해 Awake 내 SetActive(false) 구문 삭제
        /// </summary>
        private void Awake()
        {
            // UI 버튼 리스너 바인딩
            if (m_resumeButton != null)
            {
                m_resumeButton.onClick.AddListener(func_OnResumeClick);
            }

            if (m_replayTutorialButton != null)
            {
                m_replayTutorialButton.onClick.AddListener(func_OnReplayTutorialClick);
            }

            if (m_replayQuizButton != null)
            {
                m_replayQuizButton.onClick.AddListener(func_OnReplayQuizClick);
            }
        }

        private void OnDestroy()
        {
            if (m_resumeButton != null)
            {
                m_resumeButton.onClick.RemoveListener(func_OnResumeClick);
            }

            if (m_replayTutorialButton != null)
            {
                m_replayTutorialButton.onClick.RemoveListener(func_OnReplayTutorialClick);
            }

            if (m_replayQuizButton != null)
            {
                m_replayQuizButton.onClick.RemoveListener(func_OnReplayQuizClick);
            }
        }

        #endregion

        #region 초기 설정 및 데이터 바인딩

        /// <summary>
        /// [기능]: 전달된 DTO 데이터를 기반으로 팝업을 설정하고 퀴즈 버튼의 활성화 상태를 조작합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Setup(CommonPausePopupDataDTO data)
        {
            m_popupData = data;

            if (m_popupData != null)
            {
                if (m_replayQuizButton != null)
                {
                    // 퀴즈 다시보기 액션이 설정되어 있다면 노출, 없다면 숨김
                    bool hasQuiz = m_popupData.OnReplayQuiz != null;
                    m_replayQuizButton.gameObject.SetActive(hasQuiz);
                }
            }
        }

        #endregion

        #region UI 이벤트 핸들러

        /// <summary>
        /// [기능]: 계속하기 버튼 클릭 핸들러
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnResumeClick()
        {
            Debug.Log("[CommonPausePopupView] 계속하기 클릭");
            if (m_popupData != null)
            {
                if (m_popupData.OnResume != null)
                {
                    m_popupData.OnResume.Invoke();
                }
            }
            func_HidePopup();
        }

        /// <summary>
        /// [기능]: 튜토리얼 다시보기 버튼 클릭 핸들러
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnReplayTutorialClick()
        {
            Debug.Log("[CommonPausePopupView] 튜토리얼 다시보기 클릭");
            if (m_popupData != null)
            {
                if (m_popupData.OnReplayTutorial != null)
                {
                    m_popupData.OnReplayTutorial.Invoke();
                }
            }
            func_HidePopup();
        }

        /// <summary>
        /// [기능]: 퀴즈 다시보기 버튼 클릭 핸들러
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnReplayQuizClick()
        {
            Debug.Log("[CommonPausePopupView] 퀴즈 다시보기 클릭");
            if (m_popupData != null)
            {
                if (m_popupData.OnReplayQuiz != null)
                {
                    m_popupData.OnReplayQuiz.Invoke();
                }
            }
            func_HidePopup();
        }

        #endregion

        #region 공개 메서드 (팝업 연출)

        /// <summary>
        /// [기능]: 팝업을 연출 효과와 함께 화면에 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-11
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: timeScale = 0f 상태에서도 팝업이 나타나도록 SetUpdate(true) 추가
        /// </summary>
        public void ShowPopup()
        {
            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        /// <summary>
        /// [기능]: 팝업을 연출 효과와 함께 감춘 후 비활성화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-11
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: timeScale = 0f 상태에서도 팝업이 닫히도록 SetUpdate(true) 추가
        /// </summary>
        public void func_HidePopup()
        {
            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        #endregion
    }
}
