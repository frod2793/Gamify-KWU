namespace CardGame.Model
{
    public class CardModel
    {
        public int Id { get; }
        public int ShapeId { get; }
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }

        public CardModel(int id, int shapeId)
        {
            Id = id;
            ShapeId = shapeId;
            IsFlipped = false;
            IsMatched = false;
        }
    }
}
