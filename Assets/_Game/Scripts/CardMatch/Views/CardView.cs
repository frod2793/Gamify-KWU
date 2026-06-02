using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 1장의 시각적 연출(뒤집기 애니메이션, 성공 이펙트 등)을 담당하는 View 클래스입니다.
    ///         DOTween을 활용한 X축 스케일 기반 뒤집기 연출을 수행합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardView : MonoBehaviour
    {
        #region SerializeField
        [Header("카드 이미지 참조")]
        [SerializeField] private Image m_backImage;
        [SerializeField] private Image m_frontImage;
        [SerializeField] private Image m_logoImage;

        [Header("뒤집기 연출 설정")]
        [SerializeField] private float m_flipDuration = 0.15f;
        #endregion

        #region Private Fields
        private int m_cardIndex;
        private bool m_isFaceUp;
        private Action<int> m_onCardClicked;
        private Button m_button;
        #endregion

        #region Properties
        public int CardIndex => m_cardIndex;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            m_button = GetComponent<Button>();
            if (m_button != null)
            {
                m_button.onClick.AddListener(func_OnCardClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(func_OnCardClicked);
            }
            transform.DOKill();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 카드 뷰를 초기화합니다. 로고 스프라이트 및 클릭 콜백을 설정합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="cardIndex">카드 배열 내 인덱스</param>
        /// <param name="logoSprite">카드 앞면에 표시할 로고 스프라이트</param>
        /// <param name="onCardClicked">카드 클릭 시 호출될 콜백</param>
        public void Initialize(int cardIndex, Sprite logoSprite, Action<int> onCardClicked)
        {
            m_cardIndex = cardIndex;
            m_onCardClicked = onCardClicked;
            m_isFaceUp = false;

            if (m_logoImage != null)
            {
                m_logoImage.sprite = logoSprite;
            }

            ShowBack();
            Debug.Log($"[CardView] 카드 초기화 완료: 인덱스 {m_cardIndex}");
        }

        /// <summary>
        /// [기능]: DOTween 애니메이션과 함께 카드를 앞면으로 뒤집습니다.
        /// [작성자]: 김지연
        /// </summary>
        public void FlipToFront()
        {
            if (m_isFaceUp)
            {
                return;
            }
            m_isFaceUp = true;
            PlayFlipAnimation(true);
        }

        /// <summary>
        /// [기능]: DOTween 애니메이션과 함께 카드를 뒷면으로 뒤집습니다.
        /// [작성자]: 김지연
        /// </summary>
        public void FlipToBack()
        {
            if (!m_isFaceUp)
            {
                return;
            }
            m_isFaceUp = false;
            PlayFlipAnimation(false);
        }

        /// <summary>
        /// [기능]: 애니메이션 없이 즉시 앞면을 표시합니다. 미리보기 시 사용됩니다.
        /// [작성자]: 김지연
        /// </summary>
        public void ShowFrontImmediate()
        {
            m_isFaceUp = true;
            if (m_frontImage != null)
            {
                m_frontImage.gameObject.SetActive(true);
            }
            if (m_backImage != null)
            {
                m_backImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// [기능]: 애니메이션 없이 즉시 뒷면을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void ShowBackImmediate()
        {
            m_isFaceUp = false;
            ShowBack();
        }

        /// <summary>
        /// [기능]: 매칭 성공 시 카드 바운스 이펙트를 재생합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void PlayMatchSuccessEffect()
        {
            transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
        }

        /// <summary>
        /// [기능]: 카드 버튼의 상호작용 가능 여부를 설정합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (m_button != null)
            {
                m_button.interactable = interactable;
            }
        }
        #endregion

        #region UI Event Callbacks
        /// <summary>
        /// [기능]: 카드 버튼 클릭 시 호출되는 UI 이벤트 콜백입니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnCardClicked()
        {
            if (m_onCardClicked != null)
            {
                m_onCardClicked.Invoke(m_cardIndex);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// [기능]: X축 스케일 기반 카드 뒤집기 애니메이션을 재생합니다.
        ///         스케일 1→0 (축소) → 이미지 교체 → 스케일 0→1 (확대)
        /// [작성자]: 김지연
        /// </summary>
        private void PlayFlipAnimation(bool toFront)
        {
            transform.DOKill();

            transform.DOScaleX(0f, m_flipDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (toFront)
                    {
                        if (m_frontImage != null)
                        {
                            m_frontImage.gameObject.SetActive(true);
                        }
                        if (m_backImage != null)
                        {
                            m_backImage.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        ShowBack();
                    }

                    transform.DOScaleX(1f, m_flipDuration)
                        .SetEase(Ease.OutQuad);
                });
        }

        /// <summary>
        /// [기능]: 뒷면 이미지를 표시하고 앞면을 숨깁니다.
        /// [작성자]: 김지연
        /// </summary>
        private void ShowBack()
        {
            if (m_frontImage != null)
            {
                m_frontImage.gameObject.SetActive(false);
            }
            if (m_backImage != null)
            {
                m_backImage.gameObject.SetActive(true);
            }
        }
        #endregion
    }
}
