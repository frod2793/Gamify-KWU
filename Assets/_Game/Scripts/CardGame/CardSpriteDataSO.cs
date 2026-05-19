using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Data
{
    [Serializable]
    public class CardImageData
    {
        public int ShapeId;
        public Sprite FrontSprite;
    }

    [CreateAssetMenu(fileName = "CardSpriteData", menuName = "CardGame/CardSpriteData")]
    public class CardSpriteDataSO : ScriptableObject
    {
        [SerializeField] private List<CardImageData> m_cardImages = new List<CardImageData>();

        public Sprite GetSprite(int shapeId)
        {
            var data = m_cardImages.Find(x => x.ShapeId == shapeId);
            if (data != null)
            {
                return data.FrontSprite;
            }
            return null;
        }

        public int GetDataCount() => m_cardImages.Count;
    }
}
