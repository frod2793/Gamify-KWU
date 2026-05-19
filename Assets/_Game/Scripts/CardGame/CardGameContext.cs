using System.Collections.Generic;
using UnityEngine;
using CardGame.ViewModel;
using CardGame.View;
using CardGame.Data;

namespace CardGame.Context
{
    public class CardGameContext : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CardSpriteDataSO m_spriteData;
        [SerializeField] private GameObject m_cardPrefab;
        [SerializeField] private Transform m_cardContainer;

        private CardGameViewModel m_gameViewModel;
        private readonly List<CardView> m_cardViews = new List<CardView>();

        private void Start()
        {
            if (m_spriteData == null)
            {
                Debug.LogError("CardSpriteDataSO is not assigned!");
                return;
            }
            InitializeGame();
        }

        private void InitializeGame()
        {
            m_gameViewModel = new CardGameViewModel();
            
            int pairCount = m_spriteData.GetDataCount();
            List<int> shapeIds = new List<int>();
            for (int i = 0; i < pairCount; i++)
            {
                shapeIds.Add(i);
                shapeIds.Add(i);
            }
            
            // Shuffle
            for (int i = 0; i < shapeIds.Count; i++)
            {
                int temp = shapeIds[i];
                int randomIndex = Random.Range(i, shapeIds.Count);
                shapeIds[i] = shapeIds[randomIndex];
                shapeIds[randomIndex] = temp;
            }

            m_gameViewModel.Initialize(shapeIds);
            m_gameViewModel.OnScoreChanged += score => Debug.Log($"Score: {score}");
            m_gameViewModel.OnGameOver += () => Debug.Log("Game Over!");

            SpawnCards();
        }

        private void SpawnCards()
        {
            foreach (var cardVM in m_gameViewModel.Cards)
            {
                var cardObj = Instantiate(m_cardPrefab, m_cardContainer);
                var cardView = cardObj.GetComponent<CardView>();
                
                if (cardView != null)
                {
                    Sprite frontSprite = m_spriteData.GetSprite(cardVM.ShapeId);
                    cardView.Bind(cardVM, frontSprite);
                    cardView.OnClicked += vm => m_gameViewModel.SelectCard(vm);
                    m_cardViews.Add(cardView);
                }
            }
        }
    }
}
