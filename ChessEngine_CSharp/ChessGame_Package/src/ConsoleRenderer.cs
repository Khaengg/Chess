namespace ChessGame
{
    public class ConsoleRenderer
    {
        public static void DrawBoard(Board board, List<(int r, int c)>? highlights = null)
        {
            Console.Clear();
            DrawHeader();
            Console.WriteLine();

            var hi = highlights ?? new List<(int, int)>();

            // File letters
            Console.Write("      ");
            for (int c = 0; c < 8; c++)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"  {(char)('a' + c)}  ");
            }
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("      " + new string('─', 41));

            for (int r = 0; r < 8; r++)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"  {8 - r}  │");
                Console.ResetColor();

                for (int c = 0; c < 8; c++)
                {
                    bool isHighlight = hi.Any(h => h.r == r && h.c == c);
                    bool isLight = (r + c) % 2 == 0;

                    // Background color
                    Console.BackgroundColor = isHighlight
                        ? ConsoleColor.DarkGreen
                        : isLight ? ConsoleColor.DarkGray : ConsoleColor.Black;

                    var piece = board.Grid[r, c];
                    if (piece == null)
                    {
                        Console.Write("     ");
                    }
                    else
                    {
                        Console.ForegroundColor = piece.Color == PieceColor.White
                            ? ConsoleColor.White : ConsoleColor.Yellow;
                        Console.Write($"  {piece.Symbol}  ");
                    }
                    Console.ResetColor();
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"│  {8 - r}");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine("      " + new string('─', 41));
            Console.Write("      ");
            for (int c = 0; c < 8; c++)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"  {(char)('a' + c)}  ");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void DrawHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ╔══════════════════════════════════════════╗
  ║       ♔  CHESS ENGINE  ♚  by MIT AI     ║
  ╚══════════════════════════════════════════╝");
            Console.ResetColor();
        }

        public static void DrawStatus(Board board, GameStatus status, int aiEval = 0)
        {
            Console.WriteLine();
            if (status == GameStatus.Checkmate)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var winner = board.CurrentTurn == PieceColor.White ? "Black" : "White";
                Console.WriteLine($"  ♟ CHECKMATE! {winner} wins!");
            }
            else if (status == GameStatus.Stalemate)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ½ STALEMATE — Draw!");
            }
            else
            {
                Console.ForegroundColor = board.CurrentTurn == PieceColor.White ? ConsoleColor.White : ConsoleColor.Yellow;
                string check = board.IsInCheck(board.CurrentTurn) ? " ⚠ CHECK!" : "";
                Console.Write($"  {board.CurrentTurn}'s turn{check}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"   |  Move #{board.FullMoveNumber}");
            }
            Console.ResetColor();
        }

        public static void DrawMaterialBalance(Board board)
        {
            int white = 0, black = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.Grid[r, c];
                    if (p == null || p.Type == PieceType.King) continue;
                    if (p.Color == PieceColor.White) white += p.BaseValue;
                    else black += p.BaseValue;
                }

            int diff = white - black;
            Console.Write("  Material: ");
            if (diff > 0) { Console.ForegroundColor = ConsoleColor.White; Console.Write($"White +{diff / 100.0:0.0}"); }
            else if (diff < 0) { Console.ForegroundColor = ConsoleColor.Yellow; Console.Write($"Black +{-diff / 100.0:0.0}"); }
            else { Console.ForegroundColor = ConsoleColor.Gray; Console.Write("Equal"); }
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void DrawHelp()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.WriteLine("  Commands: [e2e4] move  |  [resign] quit  |  [hint] AI suggestion  |  [undo] takeback");
            Console.ResetColor();
        }

        public static void PrintMessage(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"  {msg}");
            Console.ResetColor();
        }
    }
}
