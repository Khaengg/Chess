namespace ChessGame
{
    public class ChessAI
    {
        private const int INF = 1_000_000;
        private const int CHECKMATE_SCORE = 900_000;

        private Dictionary<ulong, TTEntry> transpositionTable = new(1 << 20);
        private int[,] historyTable = new int[64, 64];
        private Move?[] killerMoves = new Move?[64];

        private int nodesSearched;
        private int maxDepth;
        private PieceColor aiColor;

        public int Difficulty { get; set; } = 4; // 1=Easy, 2=Medium, 3=Hard, 4=Expert

        public ChessAI(PieceColor color, int difficulty = 4)
        {
            aiColor = color;
            Difficulty = difficulty;
        }

        private int SearchDepth => Difficulty switch
        {
            1 => 2,
            2 => 3,
            3 => 4,
            _ => 5
        };

        public Move? GetBestMove(Board board)
        {
            transpositionTable.Clear();
            Array.Clear(historyTable, 0, historyTable.Length);
            Array.Clear(killerMoves, 0, killerMoves.Length);
            nodesSearched = 0;

            Move? bestMove = null;
            int depth = SearchDepth;
            maxDepth = depth;

            // Iterative deepening: always return something
            for (int d = 1; d <= depth; d++)
            {
                var result = SearchRoot(board, d);
                if (result.move != null) bestMove = result.move;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [AI] Nodes searched: {nodesSearched:N0} | Depth: {depth}");
            Console.ResetColor();
            return bestMove;
        }

        private (Move? move, int score) SearchRoot(Board board, int depth)
        {
            var moves = board.GenerateLegalMoves(aiColor);
            if (moves.Count == 0) return (null, -CHECKMATE_SCORE);

            OrderMoves(moves, board, 0, null);

            Move? best = null;
            int bestScore = -INF;
            int alpha = -INF, beta = INF;

            foreach (var m in moves)
            {
                var child = board.Clone();
                child.ApplyMove(m);
                int score = -Negamax(child, depth - 1, -beta, -alpha, 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = m;
                }
                alpha = Math.Max(alpha, score);
            }

            return (best, bestScore);
        }

        private int Negamax(Board board, int depth, int alpha, int beta, int ply)
        {
            nodesSearched++;

            // TT lookup
            if (transpositionTable.TryGetValue(board.ZobristHash, out var tt) && tt.Depth >= depth)
            {
                if (tt.Flag == TTFlag.Exact) return tt.Score;
                if (tt.Flag == TTFlag.LowerBound) alpha = Math.Max(alpha, tt.Score);
                if (tt.Flag == TTFlag.UpperBound) beta = Math.Min(beta, tt.Score);
                if (alpha >= beta) return tt.Score;
            }

            if (depth == 0) return QuiescenceSearch(board, alpha, beta);

            var moves = board.GenerateLegalMoves(board.CurrentTurn);
            if (moves.Count == 0)
                return board.IsInCheck(board.CurrentTurn) ? -(CHECKMATE_SCORE - ply) : 0;

            // Null move pruning (avoid in check, endgame)
            if (depth >= 3 && !board.IsInCheck(board.CurrentTurn) && ply > 0)
            {
                var nullBoard = board.Clone();
                nullBoard.CurrentTurn = board.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                int nullScore = -Negamax(nullBoard, depth - 3, -beta, -beta + 1, ply + 1);
                if (nullScore >= beta) return beta;
            }

            OrderMoves(moves, board, ply, tt?.BestMove);

            int originalAlpha = alpha;
            Move? bestMove = null;
            int bestScore = -INF;

            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                var child = board.Clone();
                child.ApplyMove(m);

                int score;
                // Late Move Reduction
                if (i >= 4 && depth >= 3 && !m.IsEnPassant && board.Grid[m.ToRow, m.ToCol] == null &&
                    !board.IsInCheck(board.CurrentTurn))
                {
                    score = -Negamax(child, depth - 2, -alpha - 1, -alpha, ply + 1);
                    if (score > alpha) score = -Negamax(child, depth - 1, -beta, -alpha, ply + 1);
                }
                else
                {
                    score = -Negamax(child, depth - 1, -beta, -alpha, ply + 1);
                }

                if (score > bestScore) { bestScore = score; bestMove = m; }
                if (score > alpha) alpha = score;
                if (alpha >= beta)
                {
                    // Killer move and history heuristic
                    if (board.Grid[m.ToRow, m.ToCol] == null)
                    {
                        killerMoves[ply] = m;
                        historyTable[m.FromRow * 8 + m.FromCol, m.ToRow * 8 + m.ToCol] += depth * depth;
                    }
                    break;
                }
            }

            // Store TT
            var flag = bestScore <= originalAlpha ? TTFlag.UpperBound :
                       bestScore >= beta          ? TTFlag.LowerBound : TTFlag.Exact;
            transpositionTable[board.ZobristHash] = new TTEntry(bestScore, depth, flag, bestMove);

            return bestScore;
        }

        private int QuiescenceSearch(Board board, int alpha, int beta)
        {
            nodesSearched++;
            int stand = EvalFromCurrentPerspective(board);
            if (stand >= beta) return beta;
            if (stand > alpha) alpha = stand;

            // Generate captures only
            var moves = board.GeneratePseudoLegalMoves(board.CurrentTurn);
            var captures = moves.Where(m =>
                board.Grid[m.ToRow, m.ToCol] != null || m.IsEnPassant || m.Promotion != PieceType.None
            ).ToList();

            // MVV-LVA ordering
            captures.Sort((a, b2) =>
            {
                int aVal = board.Grid[a.ToRow, a.ToCol]?.BaseValue ?? 0;
                int bVal = board.Grid[b2.ToRow, b2.ToCol]?.BaseValue ?? 0;
                return bVal - aVal;
            });

            foreach (var m in captures)
            {
                var child = board.Clone();
                child.ApplyMove(m);
                if (child.IsInCheck(board.CurrentTurn)) continue;
                int score = -QuiescenceSearch(child, -beta, -alpha);
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        private int EvalFromCurrentPerspective(Board board)
        {
            int raw = Evaluator.Evaluate(board);
            return board.CurrentTurn == PieceColor.White ? raw : -raw;
        }

        private void OrderMoves(List<Move> moves, Board board, int ply, Move? ttMove)
        {
            moves.Sort((a, b) => ScoreMove(b, board, ply, ttMove) - ScoreMove(a, board, ply, ttMove));
        }

        private int ScoreMove(Move m, Board board, int ply, Move? ttMove)
        {
            if (ttMove != null && m.Equals(ttMove)) return 100_000;
            
            // MVV-LVA for captures
            var victim = board.Grid[m.ToRow, m.ToCol];
            var attacker = board.Grid[m.FromRow, m.FromCol];
            if (victim != null)
                return 10_000 + victim.BaseValue - (attacker?.BaseValue ?? 0) / 100;

            if (m.IsEnPassant) return 9_000;
            if (m.Promotion != PieceType.None) return 9_500;

            // Killer moves
            if (killerMoves[ply] != null && m.Equals(killerMoves[ply])) return 8_000;

            // History heuristic
            return historyTable[m.FromRow * 8 + m.FromCol, m.ToRow * 8 + m.ToCol];
        }
    }

    public enum TTFlag { Exact, LowerBound, UpperBound }

    public record TTEntry(int Score, int Depth, TTFlag Flag, Move? BestMove);
}
