namespace ChessGame
{
    public static class Evaluator
    {
        // Returns score in centipawns from White's perspective
        public static int Evaluate(Board board)
        {
            int whiteMaterial = 0, blackMaterial = 0;
            int whitePST = 0, blackPST = 0;
            int whitePieceCount = 0, blackPieceCount = 0;

            // Count material for endgame detection
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.Grid[r, c];
                    if (p == null) continue;
                    if (p.Type != PieceType.King && p.Type != PieceType.Pawn)
                    {
                        if (p.Color == PieceColor.White) whitePieceCount++;
                        else blackPieceCount++;
                    }
                }

            bool endGame = whitePieceCount + blackPieceCount <= 6;

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.Grid[r, c];
                    if (p == null) continue;
                    int val = p.BaseValue + PieceTables.GetPST(p.Type, p.Color, r, c, endGame);
                    if (p.Color == PieceColor.White) { whiteMaterial += p.BaseValue; whitePST += val - p.BaseValue; }
                    else { blackMaterial += p.BaseValue; blackPST += val - p.BaseValue; }
                }

            int score = (whiteMaterial - blackMaterial) + (whitePST - blackPST);

            // Mobility bonus
            int whiteMobility = board.GeneratePseudoLegalMoves(PieceColor.White).Count;
            int blackMobility = board.GeneratePseudoLegalMoves(PieceColor.Black).Count;
            score += (whiteMobility - blackMobility) * 5;

            // Pawn structure: doubled/isolated penalty
            score += EvaluatePawnStructure(board, PieceColor.White);
            score -= EvaluatePawnStructure(board, PieceColor.Black);

            // Bishop pair bonus
            int whiteBishops = 0, blackBishops = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (board.Grid[r, c]?.Type == PieceType.Bishop && board.Grid[r, c]?.Color == PieceColor.White) whiteBishops++;
                    if (board.Grid[r, c]?.Type == PieceType.Bishop && board.Grid[r, c]?.Color == PieceColor.Black) blackBishops++;
                }
            if (whiteBishops >= 2) score += 30;
            if (blackBishops >= 2) score -= 30;

            return score;
        }

        private static int EvaluatePawnStructure(Board board, PieceColor color)
        {
            int score = 0;
            int[] pawnCountByFile = new int[8];
            bool[] pawnOnFile = new bool[8];

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (board.Grid[r, c]?.Type == PieceType.Pawn && board.Grid[r, c]?.Color == color)
                    {
                        pawnCountByFile[c]++;
                        pawnOnFile[c] = true;
                    }

            for (int c = 0; c < 8; c++)
            {
                if (pawnCountByFile[c] == 0) continue;
                // Doubled pawns
                if (pawnCountByFile[c] > 1) score -= 15 * (pawnCountByFile[c] - 1);
                // Isolated pawns
                bool leftEmpty = c == 0 || !pawnOnFile[c - 1];
                bool rightEmpty = c == 7 || !pawnOnFile[c + 1];
                if (leftEmpty && rightEmpty) score -= 10;
            }

            return score;
        }
    }
}
