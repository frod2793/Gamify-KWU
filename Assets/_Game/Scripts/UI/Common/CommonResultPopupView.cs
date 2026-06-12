using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameArifiction.Player;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 여러 미니게임의 결과를 공통 DTO 데이터를 기반으로 화면에 출력하고 후속 처리를 위임하는 범용 결과 팝업 View 컴포넌트입니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-08
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 불필요한 단순 확인용 디버그 로그(Debug.Log) 제거/주석화 및 마감 처리
    /// </summary>
    public class CommonResultPopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("텍스트 정보")]
        [SerializeField]
        [Tooltip("팝업 제목을 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_titleText;

        [SerializeField]
        [Tooltip("결과 설명 및 상세 내용을 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_descriptionText;

        [SerializeField]
        [Tooltip("강의명을 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_lectureNameText;

        [Header("등급 이미지 설정")]
        [SerializeField]
        [Tooltip("학점 등급 이미지를 표시할 Image 컴포넌트입니다.")]
        private Image m_resultGradeImage;

        [SerializeField]
        [Tooltip("등급별 스프라이트 관리를 수행할 ScriptableObject 데이터 자산입니다.")]
        private MinigameGradeSpritesSO m_gradeSpritesSO;

        [Header("조작 버튼")]
        [SerializeField]
        [Tooltip("결과 확인 및 다음 단계를 진행하는 확인 버튼입니다.")]
        private Button m_confirmButton;

        [Header("플레이어 데이터 연동")]
        [SerializeField]
        [Tooltip("미니게임 결과를 기록할 플레이어 데이터 에셋입니다.")]
        private PlayerSO m_playerSO;

        #endregion

        #region 내부 필드 (Private Fields)

        private TextMeshProUGUI m_confirmButtonText;
        private Action m_onConfirmAction;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 컴포넌트 초기화 시 버튼 이벤트 등 필요한 리스너를 등록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void Start()
        {
            if (m_confirmButton != null)
            {
                m_confirmButtonText = m_confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                m_confirmButton.onClick.AddListener(func_OnConfirmButtonClick);
            }

            // Debug.Log("[CommonResultPopupView] 공통 결과 팝업 뷰 초기화 완료.");
        }

        /// <summary>
        /// [기능]: 오브젝트가 파괴될 때 메모리 누수 방지를 위해 이벤트 리스너를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        private void OnDestroy()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.RemoveListener(func_OnConfirmButtonClick);
            }
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 전달받은 DTO 데이터를 바인딩하여 팝업 내용을 갱신하고, Outback 슬릭 트윈 효과로 활성화 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Setup(CommonPopupDataDTO data)
        {
            if (data == null)
            {
                return;
            }

            // 플레이어 데이터에 미니게임 성적 기록
            if (m_playerSO != null && data.Grade.HasValue && data.Grade.Value != MinigameGrade.None)
            {
                // DTO에 명시적인 ID가 지정되어 있다면 이를 사용하고, 없으면 현재 활성화된 씬 이름 사용
                string minigameId = string.IsNullOrEmpty(data.MinigameId)
                    ? SceneManager.GetActiveScene().name
                    : data.MinigameId;

                m_playerSO.SetMinigameGrade(minigameId, data.Grade.Value);
            }

            // 1. 텍스트 설정
            if (m_titleText != null)
            {
                m_titleText.text = data.Title;
            }

            if (m_descriptionText != null)
            {
                if (!string.IsNullOrEmpty(data.Description))
                {
                    m_descriptionText.text = data.Description;
                    m_descriptionText.gameObject.SetActive(true);
                }
                else
                {
                    m_descriptionText.gameObject.SetActive(false);
                }
            }

            // 2. 기획서 기반 세부 필드 설정
            if (m_lectureNameText != null)
            {
                if (!string.IsNullOrEmpty(data.LectureName))
                {
                    m_lectureNameText.text = $"강의명[{data.LectureName}]";
                    m_lectureNameText.gameObject.SetActive(true);
                }
                else
                {
                    m_lectureNameText.gameObject.SetActive(false);
                }
            }

            // 2. 등급 이미지 설정
            if (m_resultGradeImage != null)
            {
                if (data.Grade.HasValue)
                {
                    Sprite gradeSprite = GetGradeSprite(data.Grade.Value);
                    if (gradeSprite != null)
                    {
                        m_resultGradeImage.sprite = gradeSprite;
                        m_resultGradeImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        m_resultGradeImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    m_resultGradeImage.gameObject.SetActive(false);
                }
            }

            // 3. 버튼 텍스트 설정
            if (m_confirmButtonText != null)
            {
                m_confirmButtonText.text = data.ConfirmButtonText;
            }

            // 4. 콜백 바인딩
            m_onConfirmAction = data.OnConfirm;

            // 5. 팝업 활성화 및 Outback 트윈 애니메이션
            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);

            // Debug.Log("[CommonResultPopupView] 결과 팝업이 활성화되었으며 데이터를 바인딩했습니다.");
        }

        /// <summary>
        /// [기능]: 결과 팝업 패널을 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_HidePopup()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// [기능]: 확인 버튼 클릭 시 바인딩된 콜백을 실행하고 팝업을 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnConfirmButtonClick()
        {
            // Debug.Log("[CommonResultPopupView] 플레이어가 확인 버튼을 선택했습니다.");
            
            Action callback = m_onConfirmAction;
            func_HidePopup();

            if (callback != null)
            {
                callback.Invoke();
            }
        }

        /// <summary>
        /// [기능]: 학점 등급에 맞춰 미리 연결해 둔 스프라이트를 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private Sprite GetGradeSprite(MinigameGrade grade)
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
