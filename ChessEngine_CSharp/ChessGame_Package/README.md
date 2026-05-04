# ♔ Chess Engine — C# MIT AI

## Tính năng
- **AI thông minh**: Minimax + Alpha-Beta Pruning + Iterative Deepening
- **Transposition Table**: Bảng hash Zobrist để tránh tính toán lặp
- **Move Ordering**: MVV-LVA + Killer Moves + History Heuristic
- **Null Move Pruning**: Cắt nhánh thông minh
- **Late Move Reduction (LMR)**: Giảm độ sâu cho nước đi kém
- **Quiescence Search**: Tìm kiếm ổn định sau nước capture
- **Piece-Square Tables**: Vị trí quân cờ theo Stockfish style
- **Pawn Structure**: Đánh giá cấu trúc tốt (doubled, isolated)
- **Bishop Pair Bonus**: Thưởng cho cặp tượng
- **Castling + En Passant**: Đầy đủ luật cờ vua
- **Promotion**: Phong cấp tốt

## Cách chạy
```bash
cd ChessGame
dotnet run
```

## Nhập nước đi
- `e2e4` — Di chuyển từ e2 đến e4
- `e7e8q` — Phong cấp thành Hậu
- `hint` — Gợi ý nước đi tốt nhất
- `undo` — Hoàn tác nước đi
- `resign` — Đầu hàng

## Cấu trúc code
- `Piece.cs` — Định nghĩa quân cờ
- `Move.cs` — Biểu diễn nước đi
- `Board.cs` — Bàn cờ + luật chơi đầy đủ
- `PieceTables.cs` — Bảng đánh giá vị trí quân cờ
- `Evaluator.cs` — Hàm đánh giá thế cờ
- `ChessAI.cs` — Engine AI (Minimax + tối ưu hóa)
- `ConsoleRenderer.cs` — Hiển thị bàn cờ Unicode
- `Program.cs` — Game loop chính

## Yêu cầu
- .NET 8.0 SDK
