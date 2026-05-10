namespace ChessEngine.game.chess;

public readonly struct Piece
{
    public PieceType Type { get; }
    public PieceColor Color { get; }

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    public override string ToString()
    {
        if (Color == PieceColor.White)
        {
            if (Type == PieceType.King) return "K";
            if (Type == PieceType.Queen) return "Q";
            if (Type == PieceType.Rook) return "R";
            if (Type == PieceType.Bishop) return "B";
            if (Type == PieceType.Knight) return "N";
            if (Type == PieceType.Pawn) return "P";
        }
        else
        {
            if (Type == PieceType.King) return "k";
            if (Type == PieceType.Queen) return "q";
            if (Type == PieceType.Rook) return "r";
            if (Type == PieceType.Bishop) return "b";
            if (Type == PieceType.Knight) return "n";
            if (Type == PieceType.Pawn) return "p";
        }

        return "?";
    }
}