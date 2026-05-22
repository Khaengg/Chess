namespace ChessGame
{
    public class Board
    {
        public Piece?[,] Grid { get; private set; } = new Piece?[8, 8];
        public PieceColor CurrentTurn { get; set; } = PieceColor.White;
        public (int row, int col)? EnPassantTarget { get; private set; }
        public int HalfMoveClock { get; private set; }
        public int FullMoveNumber { get; private set; } = 1;
        public ulong ZobristHash { get; private set; }

        private static readonly ulong[,,] ZobristTable = new ulong[8, 8, 12];
        private static readonly ulong ZobristBlackTurn;
        private static readonly Random rng = new Random(42);

        static Board()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    for (int p = 0; p < 12; p++)
                        ZobristTable[r, c, p] = NextRandom();
            ZobristBlackTurn = NextRandom();
        }

        private static ulong NextRandom()
        {
            var buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf);
        }

        private int PieceIndex(Piece p)
        {
            int t = (int)p.Type - 1; // 0-5
            int c = p.Color == PieceColor.White ? 0 : 6;
            return t + c;
        }

        public Board() { SetupStartPosition(); }

        public void SetupStartPosition()
        {
            Grid = new Piece?[8, 8];
            PieceType[] backRow = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
                                    PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };
            for (int c = 0; c < 8; c++)
            {
                Grid[0, c] = new Piece(backRow[c], PieceColor.Black);
                Grid[1, c] = new Piece(PieceType.Pawn, PieceColor.Black);
                Grid[6, c] = new Piece(PieceType.Pawn, PieceColor.White);
                Grid[7, c] = new Piece(backRow[c], PieceColor.White);
            }
            CurrentTurn = PieceColor.White;
            EnPassantTarget = null;
            HalfMoveClock = 0;
            FullMoveNumber = 1;
            RecomputeZobrist();
        }

        private void RecomputeZobrist()
        {
            ZobristHash = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (Grid[r, c] != null)
                        ZobristHash ^= ZobristTable[r, c, PieceIndex(Grid[r, c]!)];
            if (CurrentTurn == PieceColor.Black)
                ZobristHash ^= ZobristBlackTurn;
        }

        public Board Clone()
        {
            var b = new Board();
            b.Grid = new Piece?[8, 8];
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    b.Grid[r, c] = Grid[r, c]?.Clone();
            b.CurrentTurn = CurrentTurn;
            b.EnPassantTarget = EnPassantTarget;
            b.HalfMoveClock = HalfMoveClock;
            b.FullMoveNumber = FullMoveNumber;
            b.ZobristHash = ZobristHash;
            return b;
        }

        public bool IsInBounds(int r, int c) => r >= 0 && r < 8 && c >= 0 && c < 8;

        public bool IsSquareAttackedBy(int r, int c, PieceColor attacker)
        {
            // Knight attacks
            int[] kdr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] kdc = { -1,  1, -2,  2, -2, 2, -1, 1 };
            for (int i = 0; i < 8; i++)
            {
                int nr = r + kdr[i], nc = c + kdc[i];
                if (IsInBounds(nr, nc) && Grid[nr, nc]?.Type == PieceType.Knight && Grid[nr, nc]?.Color == attacker)
                    return true;
            }

            // Pawn attacks
            int pawnDir = attacker == PieceColor.White ? 1 : -1;
            int pr = r + pawnDir;
            if (IsInBounds(pr, c - 1) && Grid[pr, c - 1]?.Type == PieceType.Pawn && Grid[pr, c - 1]?.Color == attacker) return true;
            if (IsInBounds(pr, c + 1) && Grid[pr, c + 1]?.Type == PieceType.Pawn && Grid[pr, c + 1]?.Color == attacker) return true;

            // Sliding pieces (rook/queen - straight)
            int[][] dirs = { new[]{0,1}, new[]{0,-1}, new[]{1,0}, new[]{-1,0} };
            foreach (var d in dirs)
            {
                int nr = r + d[0], nc = c + d[1];
                while (IsInBounds(nr, nc))
                {
                    var p = Grid[nr, nc];
                    if (p != null)
                    {
                        if (p.Color == attacker && (p.Type == PieceType.Rook || p.Type == PieceType.Queen)) return true;
                        break;
                    }
                    nr += d[0]; nc += d[1];
                }
            }

            // Sliding pieces (bishop/queen - diagonal)
            int[][] diagDirs = { new[]{1,1}, new[]{1,-1}, new[]{-1,1}, new[]{-1,-1} };
            foreach (var d in diagDirs)
            {
                int nr = r + d[0], nc = c + d[1];
                while (IsInBounds(nr, nc))
                {
                    var p = Grid[nr, nc];
                    if (p != null)
                    {
                        if (p.Color == attacker && (p.Type == PieceType.Bishop || p.Type == PieceType.Queen)) return true;
                        break;
                    }
                    nr += d[0]; nc += d[1];
                }
            }

            // King attacks
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = r + dr, nc = c + dc;
                    if (IsInBounds(nr, nc) && Grid[nr, nc]?.Type == PieceType.King && Grid[nr, nc]?.Color == attacker)
                        return true;
                }

            return false;
        }

        public (int row, int col) FindKing(PieceColor color)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (Grid[r, c]?.Type == PieceType.King && Grid[r, c]?.Color == color)
                        return (r, c);
            return (-1, -1);
        }

        public bool IsInCheck(PieceColor color)
        {
            var king = FindKing(color);
            return IsSquareAttackedBy(king.row, king.col, Opponent(color));
        }

        public PieceColor Opponent(PieceColor c) => c == PieceColor.White ? PieceColor.Black : PieceColor.White;

        public List<Move> GeneratePseudoLegalMoves(PieceColor color)
        {
            var moves = new List<Move>(60);
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = Grid[r, c];
                    if (p == null || p.Color != color) continue;
                    AddMovesForPiece(r, c, p, moves);
                }
            return moves;
        }

        private void AddMovesForPiece(int r, int c, Piece p, List<Move> moves)
        {
            switch (p.Type)
            {
                case PieceType.Pawn:   AddPawnMoves(r, c, p.Color, moves); break;
                case PieceType.Knight: AddLeaperMoves(r, c, p.Color, moves, new int[,]{{-2,-1},{-2,1},{-1,-2},{-1,2},{1,-2},{1,2},{2,-1},{2,1}}); break;
                case PieceType.Bishop: AddSlidingMoves(r, c, p.Color, moves, new int[,]{{1,1},{1,-1},{-1,1},{-1,-1}}); break;
                case PieceType.Rook:   AddSlidingMoves(r, c, p.Color, moves, new int[,]{{0,1},{0,-1},{1,0},{-1,0}}); break;
                case PieceType.Queen:  AddSlidingMoves(r, c, p.Color, moves, new int[,]{{0,1},{0,-1},{1,0},{-1,0},{1,1},{1,-1},{-1,1},{-1,-1}}); break;
                case PieceType.King:   AddKingMoves(r, c, p.Color, moves); break;
            }
        }

        private void AddPawnMoves(int r, int c, PieceColor color, List<Move> moves)
        {
            int dir = color == PieceColor.White ? -1 : 1;
            int startRow = color == PieceColor.White ? 6 : 1;
            int promRow  = color == PieceColor.White ? 0 : 7;

            // Forward one step
            int nr = r + dir;
            if (IsInBounds(nr, c) && Grid[nr, c] == null)
            {
                if (nr == promRow)
                {
                    moves.Add(new Move(r, c, nr, c, PieceType.Queen));
                    moves.Add(new Move(r, c, nr, c, PieceType.Rook));
                    moves.Add(new Move(r, c, nr, c, PieceType.Bishop));
                    moves.Add(new Move(r, c, nr, c, PieceType.Knight));
                }
                else moves.Add(new Move(r, c, nr, c));

                // Two steps from start
                if (r == startRow && Grid[r + 2 * dir, c] == null)
                    moves.Add(new Move(r, c, r + 2 * dir, c));
            }

            // Captures
            foreach (int dc in new[] { -1, 1 })
            {
                int nc = c + dc;
                if (!IsInBounds(nr, nc)) continue;
                bool isEnPassant = EnPassantTarget.HasValue && EnPassantTarget.Value.row == nr && EnPassantTarget.Value.col == nc;
                if ((Grid[nr, nc] != null && Grid[nr, nc]!.Color != color) || isEnPassant)
                {
                    if (nr == promRow)
                    {
                        moves.Add(new Move(r, c, nr, nc, PieceType.Queen));
                        moves.Add(new Move(r, c, nr, nc, PieceType.Rook));
                        moves.Add(new Move(r, c, nr, nc, PieceType.Bishop));
                        moves.Add(new Move(r, c, nr, nc, PieceType.Knight));
                    }
                    else
                    {
                        var m = new Move(r, c, nr, nc) { IsEnPassant = isEnPassant };
                        moves.Add(m);
                    }
                }
            }
        }

        private void AddLeaperMoves(int r, int c, PieceColor color, List<Move> moves, int[,] deltas)
        {
            for (int i = 0; i < deltas.GetLength(0); i++)
            {
                int nr = r + deltas[i, 0], nc = c + deltas[i, 1];
                if (IsInBounds(nr, nc) && Grid[nr, nc]?.Color != color)
                    moves.Add(new Move(r, c, nr, nc));
            }
        }

        private void AddSlidingMoves(int r, int c, PieceColor color, List<Move> moves, int[,] dirs)
        {
            for (int i = 0; i < dirs.GetLength(0); i++)
            {
                int nr = r + dirs[i, 0], nc = c + dirs[i, 1];
                while (IsInBounds(nr, nc))
                {
                    if (Grid[nr, nc] != null)
                    {
                        if (Grid[nr, nc]!.Color != color) moves.Add(new Move(r, c, nr, nc));
                        break;
                    }
                    moves.Add(new Move(r, c, nr, nc));
                    nr += dirs[i, 0]; nc += dirs[i, 1];
                }
            }
        }

        private void AddKingMoves(int r, int c, PieceColor color, List<Move> moves)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = r + dr, nc = c + dc;
                    if (IsInBounds(nr, nc) && Grid[nr, nc]?.Color != color)
                        moves.Add(new Move(r, c, nr, nc));
                }

            // Castling
            var king = Grid[r, c]!;
            if (!king.HasMoved && !IsInCheck(color))
            {
                int backRank = color == PieceColor.White ? 7 : 0;
                // King-side
                var kRook = Grid[backRank, 7];
                if (kRook?.Type == PieceType.Rook && !kRook.HasMoved &&
                    Grid[backRank, 5] == null && Grid[backRank, 6] == null &&
                    !IsSquareAttackedBy(backRank, 5, Opponent(color)) &&
                    !IsSquareAttackedBy(backRank, 6, Opponent(color)))
                    moves.Add(new Move(r, c, backRank, 6) { IsCastle = true });

                // Queen-side
                var qRook = Grid[backRank, 0];
                if (qRook?.Type == PieceType.Rook && !qRook.HasMoved &&
                    Grid[backRank, 1] == null && Grid[backRank, 2] == null && Grid[backRank, 3] == null &&
                    !IsSquareAttackedBy(backRank, 3, Opponent(color)) &&
                    !IsSquareAttackedBy(backRank, 2, Opponent(color)))
                    moves.Add(new Move(r, c, backRank, 2) { IsCastle = true });
            }
        }

        public List<Move> GenerateLegalMoves(PieceColor color)
        {
            var pseudo = GeneratePseudoLegalMoves(color);
            var legal = new List<Move>(pseudo.Count);
            foreach (var m in pseudo)
            {
                var b = Clone();
                b.ApplyMove(m);
                if (!b.IsInCheck(color)) legal.Add(m);
            }
            return legal;
        }

        public void ApplyMove(Move move)
        {
            var piece = Grid[move.FromRow, move.FromCol]!;
            var opponent = Opponent(piece.Color);

            // Update Zobrist
            ZobristHash ^= ZobristTable[move.FromRow, move.FromCol, PieceIndex(piece)];
            if (Grid[move.ToRow, move.ToCol] != null)
                ZobristHash ^= ZobristTable[move.ToRow, move.ToCol, PieceIndex(Grid[move.ToRow, move.ToCol]!)];

            // En passant capture
            if (move.IsEnPassant)
            {
                int capturedRow = move.FromRow;
                var ep = Grid[capturedRow, move.ToCol]!;
                ZobristHash ^= ZobristTable[capturedRow, move.ToCol, PieceIndex(ep)];
                Grid[capturedRow, move.ToCol] = null;
            }

            // Castling: move rook
            if (move.IsCastle)
            {
                int backRank = move.ToRow;
                bool kingSide = move.ToCol == 6;
                int rookFromCol = kingSide ? 7 : 0;
                int rookToCol   = kingSide ? 5 : 3;
                var rook = Grid[backRank, rookFromCol]!;
                ZobristHash ^= ZobristTable[backRank, rookFromCol, PieceIndex(rook)];
                Grid[backRank, rookToCol] = rook;
                Grid[backRank, rookFromCol] = null;
                rook.HasMoved = true;
                ZobristHash ^= ZobristTable[backRank, rookToCol, PieceIndex(rook)];
            }

            // Move piece
            Grid[move.ToRow, move.ToCol] = piece;
            Grid[move.FromRow, move.FromCol] = null;
            piece.HasMoved = true;

            // Promotion
            if (move.Promotion != PieceType.None)
            {
                ZobristHash ^= ZobristTable[move.ToRow, move.ToCol, PieceIndex(piece)];
                piece.Type = move.Promotion;
                ZobristHash ^= ZobristTable[move.ToRow, move.ToCol, PieceIndex(piece)];
            }
            else
            {
                ZobristHash ^= ZobristTable[move.ToRow, move.ToCol, PieceIndex(piece)];
            }

            // En passant target
            EnPassantTarget = null;
            if (piece.Type == PieceType.Pawn && Math.Abs(move.ToRow - move.FromRow) == 2)
                EnPassantTarget = ((move.FromRow + move.ToRow) / 2, move.ToCol);

            // Half-move clock
            if (piece.Type == PieceType.Pawn || Grid[move.ToRow, move.ToCol] != null)
                HalfMoveClock = 0;
            else HalfMoveClock++;

            if (CurrentTurn == PieceColor.Black) FullMoveNumber++;
            CurrentTurn = opponent;
            ZobristHash ^= ZobristBlackTurn;
        }

        public GameStatus GetStatus()
        {
            var legalMoves = GenerateLegalMoves(CurrentTurn);
            if (legalMoves.Count > 0) return GameStatus.Playing;
            return IsInCheck(CurrentTurn) ? GameStatus.Checkmate : GameStatus.Stalemate;
        }
    }

    public enum GameStatus { Playing, Checkmate, Stalemate, Draw }
}
