namespace ChessGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "♔ Chess Engine — MIT AI";
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ╔═══════════════════════════════════════════════════╗
  ║                                                   ║
  ║      ♔  CHESS ENGINE  ♚                          ║
  ║      Minimax + Alpha-Beta + PST + Killers         ║
  ║                                                   ║
  ╚═══════════════════════════════════════════════════╝
");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Select Difficulty:");
            Console.ResetColor();
            Console.WriteLine("    [1] Easy     (depth 2)");
            Console.WriteLine("    [2] Medium   (depth 3)");
            Console.WriteLine("    [3] Hard     (depth 4)");
            Console.WriteLine("    [4] Expert   (depth 5)");
            Console.Write("\n  Your choice [1-4]: ");
            int diff = 4;
            var key = Console.ReadKey().KeyChar;
            if (key >= '1' && key <= '4') diff = key - '0';
            Console.WriteLine();

            Console.Write("  Play as [W]hite or [B]lack? ");
            var colorKey = Console.ReadKey().KeyChar;
            Console.WriteLine();
            bool playerIsWhite = colorKey != 'b' && colorKey != 'B';
            PieceColor playerColor = playerIsWhite ? PieceColor.White : PieceColor.Black;
            PieceColor aiColor     = playerIsWhite ? PieceColor.Black : PieceColor.White;

            var ai = new ChessAI(aiColor, diff);
            var board = new Board();
            var history = new Stack<Board>();

            ConsoleRenderer.PrintMessage("Game started! Type moves like e2e4. Good luck!", ConsoleColor.Green);
            System.Threading.Thread.Sleep(800);

            while (true)
            {
                var status = board.GetStatus();
                ConsoleRenderer.DrawBoard(board);
                ConsoleRenderer.DrawStatus(board, status);
                ConsoleRenderer.DrawMaterialBalance(board);

                if (status != GameStatus.Playing)
                {
                    Console.Write("\n  Press [R] to restart or any key to quit: ");
                    var k = Console.ReadKey(true);
                    if (k.KeyChar == 'r' || k.KeyChar == 'R') { board = new Board(); history.Clear(); continue; }
                    break;
                }

                if (board.CurrentTurn == aiColor)
                {
                    ConsoleRenderer.PrintMessage("AI is thinking...", ConsoleColor.Cyan);
                    var aiMove = ai.GetBestMove(board);
                    if (aiMove == null) break;
                    history.Push(board.Clone());
                    board.ApplyMove(aiMove);
                    ConsoleRenderer.PrintMessage($"AI played: {aiMove}", ConsoleColor.Cyan);
                    System.Threading.Thread.Sleep(300);
                    continue;
                }

                ConsoleRenderer.DrawHelp();
                Console.Write("\n  Your move: ");
                string input = Console.ReadLine()?.Trim().ToLower() ?? "";

                if (input == "resign") { ConsoleRenderer.PrintMessage("You resigned.", ConsoleColor.Red); break; }

                if (input == "undo")
                {
                    if (history.Count >= 2) { history.Pop(); board = history.Pop(); ConsoleRenderer.PrintMessage("Undone.", ConsoleColor.Yellow); }
                    else ConsoleRenderer.PrintMessage("Nothing to undo.", ConsoleColor.Red);
                    System.Threading.Thread.Sleep(300);
                    continue;
                }

                if (input == "hint")
                {
                    var hintAI = new ChessAI(playerColor, diff);
                    var hint = hintAI.GetBestMove(board);
                    ConsoleRenderer.PrintMessage($"Hint: try {hint?.ToAlgebraic() ?? "none"}", ConsoleColor.Green);
                    System.Threading.Thread.Sleep(1500);
                    continue;
                }

                var move = ParseMove(input, board, playerColor);
                if (move == null) { ConsoleRenderer.PrintMessage("Invalid! Use e2e4 or e7e8q format.", ConsoleColor.Red); System.Threading.Thread.Sleep(1000); continue; }

                history.Push(board.Clone());
                board.ApplyMove(move);
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  Thanks for playing! Press any key to exit.");
            Console.ReadKey(true);
        }

        static Move? ParseMove(string input, Board board, PieceColor color)
        {
            if (input.Length < 4) return null;
            try
            {
                int fromCol = input[0] - 'a', fromRow = 8 - (input[1] - '0');
                int toCol   = input[2] - 'a', toRow   = 8 - (input[3] - '0');
                if (fromCol < 0 || fromCol > 7 || toCol < 0 || toCol > 7) return null;
                if (fromRow < 0 || fromRow > 7 || toRow < 0 || toRow > 7) return null;
                PieceType promo = input.Length >= 5 ? input[4] switch { 'q' => PieceType.Queen, 'r' => PieceType.Rook, 'b' => PieceType.Bishop, 'n' => PieceType.Knight, _ => PieceType.Queen } : PieceType.None;
                var legal = board.GenerateLegalMoves(color);
                var match = legal.FirstOrDefault(m => m.FromRow == fromRow && m.FromCol == fromCol && m.ToRow == toRow && m.ToCol == toCol && (m.Promotion == promo || (promo == PieceType.None && m.Promotion == PieceType.None)));
                if (match == null && promo == PieceType.None)
                    match = legal.FirstOrDefault(m => m.FromRow == fromRow && m.FromCol == fromCol && m.ToRow == toRow && m.ToCol == toCol && m.Promotion == PieceType.Queen);
                return match;
            }
            catch { return null; }
        }
    }
}
