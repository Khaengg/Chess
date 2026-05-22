namespace ChessGame
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { None, White, Black }

    public class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public bool HasMoved { get; set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
            HasMoved = false;
        }

        public Piece Clone() => new Piece(Type, Color) { HasMoved = this.HasMoved };

        public char Symbol => (Type, Color) switch
        {
            (PieceType.King,   PieceColor.White) => '♔',
            (PieceType.Queen,  PieceColor.White) => '♕',
            (PieceType.Rook,   PieceColor.White) => '♖',
            (PieceType.Bishop, PieceColor.White) => '♗',
            (PieceType.Knight, PieceColor.White) => '♘',
            (PieceType.Pawn,   PieceColor.White) => '♙',
            (PieceType.King,   PieceColor.Black) => '♚',
            (PieceType.Queen,  PieceColor.Black) => '♛',
            (PieceType.Rook,   PieceColor.Black) => '♜',
            (PieceType.Bishop, PieceColor.Black) => '♝',
            (PieceType.Knight, PieceColor.Black) => '♞',
            (PieceType.Pawn,   PieceColor.Black) => '♟',
            _ => '·'
        };

        // Base material values (centipawns)
        public int BaseValue => Type switch
        {
            PieceType.Pawn   => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook   => 500,
            PieceType.Queen  => 900,
            PieceType.King   => 20000,
            _ => 0
        };
    }
}
