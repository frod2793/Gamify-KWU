using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 하늘에서 하강하는 개별 장애물(코드) 및 아이템(족보)의 시각 연출, 이동 및 소멸을 관리하는 View 컴포넌트.
///         CodePrefab의 경우 SpriteRenderer 대신 TextMeshPro(TMP)를 사용해 가독성과 기획 컨셉을 살립니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class FallingObjectView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)

        private FallingObjectType m_objectType;
        private CodeColorType m_codeColor;
        private float m_targetY = 0f;
        private float m_fallDuration = 0f;

        private SpriteRenderer m_spriteRenderer;
        private TextMeshPro m_textMeshPro;

        // 스포너로부터 할당받은 낙하용 단어
        private string m_assignedWord;
        private TweenCallback m_deactivateCallback;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public FallingObjectType ObjectType => m_objectType;
        public CodeColorType CodeColor => m_codeColor;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            m_textMeshPro = GetComponent<TextMeshPro>();
            m_deactivateCallback = func_Deactivate;
        }

        private void OnDisable()
        {
            // DOTween 애니메이션 자원 누수 방지를 위한 정리
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.DOKill();
            }
            if (m_textMeshPro != null)
            {
                m_textMeshPro.DOKill();
            }
            transform.DOKill();
        }

        #endregion

        #region 초기화 및 데이터 바인딩 (Initialization)

        /// <summary>
        /// [기능]: 오브젝트의 성격, 색상 카테고리, 낙하 속도 및 텍스트 단어를 설정하고 연출을 가동합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(FallingObjectType type, CodeColorType colorType, float targetY, float duration, string word = "")
        {
            m_objectType = type;
            m_codeColor = colorType;
            m_targetY = targetY;
            m_fallDuration = duration;
            m_assignedWord = word;

            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (m_textMeshPro == null)
            {
                m_textMeshPro = GetComponent<TextMeshPro>();
            }

            ApplyVisuals();

            // DOTween을 사용하여 개별 Update 호출 없이 하강 구현
            transform.DOMoveY(m_targetY, m_fallDuration)
                .SetEase(Ease.Linear)
                .OnComplete(m_deactivateCallback);
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 족보와 코드의 디자인 및 색상 가중치를 적용합니다. 
        ///         코드는 SpriteRenderer가 아닌 TextMeshPro 컴포넌트를 이용해 텍스트 색상 및 무작위 코드 어휘를 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void ApplyVisuals()
        {
            transform.localScale = Vector3.one;

            if (m_objectType == FallingObjectType.Code)
            {
                // 코드 오브젝트: TextMeshPro 시각 효과 우선 적용
                if (m_textMeshPro != null)
                {
                    m_textMeshPro.gameObject.SetActive(true);
                    
                    // 스포너로부터 부여받은 단어 매핑
                    m_textMeshPro.text = m_assignedWord;

                    // HSL 기반의 세련된 조화 톤 적용
                    Color textColor = GradeRunnerColorPalette.CodeRed; // 기본 Red
                    switch (m_codeColor)
                    {
                        case CodeColorType.Red:
                            textColor = GradeRunnerColorPalette.CodeRed;
                            break;
                        case CodeColorType.Purple:
                            textColor = GradeRunnerColorPalette.CodePurple;
                            break;
                        case CodeColorType.Yellow:
                            textColor = GradeRunnerColorPalette.CodeYellow;
                            break;
                        case CodeColorType.SkyBlue:
                            textColor = GradeRunnerColorPalette.CodeSkyBlue;
                            break;
                        case CodeColorType.Green:
                            textColor = GradeRunnerColorPalette.CodeGreen;
                            break;
                    }
                    m_textMeshPro.color = textColor;
                }

                if (m_spriteRenderer != null)
                {
                    m_spriteRenderer.gameObject.SetActive(false); // 스프라이트는 숨김
                }
            }
            else if (m_objectType == FallingObjectType.CheatSheet)
            {
                // 족보 아이템: SpriteRenderer 시각 효과 우선 적용
                if (m_spriteRenderer != null)
                {
                    m_spriteRenderer.gameObject.SetActive(true);
                    m_spriteRenderer.DOKill();
                    m_spriteRenderer.color = GradeRunnerColorPalette.CheatSheetGold; // 찬란한 황금빛
                }

                if (m_textMeshPro != null)
                {
                    m_textMeshPro.gameObject.SetActive(false); // 텍스트는 숨김
                }

                // 프리미엄 반짝임 펄스 연출
                transform.DOKill();
                transform.DOScale(new Vector3(1.25f, 1.25f, 1f), 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        #endregion

        #region 공개 공용 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 충돌이 발생하거나 화면 이탈 시 오브젝트 풀로 안전하게 회수하기 위해 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_Deactivate()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// [기능]: 일시정지 상태에 따라 현재 진행 중인 DOTween 하강 및 크기 연출을 멈추거나 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void SetPauseState(bool isPaused)
        {
            if (isPaused)
            {
                transform.DOPause();
            }
            else
            {
                transform.DOPlay();
            }
        }

        #endregion
    }
}
