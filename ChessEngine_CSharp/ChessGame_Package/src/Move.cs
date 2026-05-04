namespace ChessGame
{
    public class Move
    {
        public int FromRow { get; }
        public int FromCol { get; }
        public int ToRow { get; }
        public int ToCol { get; }
        public PieceType Promotion { get; }
        public bool IsEnPassant { get; set; }
        public bool IsCastle { get; set; }

        public Move(int fromRow, int fromCol, int toRow, int toCol, PieceType promotion = PieceType.None)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            Promotion = promotion;
        }

        public string ToAlgebraic()
        {
            string from = $"{(char)('a' + FromCol)}{8 - FromRow}";
            string to   = $"{(char)('a' + ToCol)}{8 - ToRow}";
            string promo = Promotion != PieceType.None ? Promotion.ToString()[0].ToString().ToLower() : "";
            return from + to + promo;
        }

        public override string ToString() => ToAlgebraic();

        public override bool Equals(object? obj) =>
            obj is Move m && m.FromRow == FromRow && m.FromCol == FromCol &&
            m.ToRow == ToRow && m.ToCol == ToCol && m.Promotion == Promotion;

        public override int GetHashCode() =>
            HashCode.Combine(FromRow, FromCol, ToRow, ToCol, Promotion);
    }
}
