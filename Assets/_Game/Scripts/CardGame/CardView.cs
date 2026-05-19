using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using CardGame.ViewModel;

namespace CardGame.View
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Transform m_visualTransform;
        [SerializeField] private Image m_frontImage;
        
        private CardViewModel m_viewModel;
        public System.Action<CardViewModel> OnClicked;

        public void Bind(CardViewModel viewModel, Sprite frontSprite)
        {
            m_viewModel = viewModel;
            if (m_frontImage != null)
            {
                m_frontImage.sprite = frontSprite;
            }

            m_viewModel.OnFlipStateChanged += HandleFlipStateChanged;
            m_viewModel.OnMatched += HandleMatched;
            
            // Initial state
            UpdateVisual(m_viewModel.IsFlipped);
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnFlipStateChanged -= HandleFlipStateChanged;
                m_viewModel.OnMatched -= HandleMatched;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_viewModel == null) return;
            OnClicked?.Invoke(m_viewModel);
        }

        private void HandleFlipStateChanged(bool isFlipped)
        {
            float targetRotation = isFlipped ? 180f : 0f;
            m_visualTransform.DORotate(new Vector3(0, targetRotation, 0), 0.5f)
                .SetEase(Ease.InOutBack);
        }

        private void HandleMatched()
        {
            // Visual feedback for match (e.g., scale up/down or change color)
            m_visualTransform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
        }

        private void UpdateVisual(bool isFlipped)
        {
            m_visualTransform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0, 0);
        }
    }
}
