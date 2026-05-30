using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChessBoardUI : MonoBehaviour
{
    private enum GameMode
    {
        PvP,
        EasyAI,
        HardAI
    }

    private struct ChessMove
    {
        public int fromRow;
        public int fromCol;
        public int toRow;
        public int toCol;

        public ChessMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            this.fromRow = fromRow;
            this.fromCol = fromCol;
            this.toRow = toRow;
            this.toCol = toCol;
        }
    }
    private struct BoardSnapshot
    {
        public string[,] board;
        public string currentTurn;
        public int selectedRow;
        public int selectedCol;
        public int promotionRow;
        public int promotionCol;
        public string promotionColor;
        public bool waitingForPromotion;
        public bool gameOver;
        public int enPassantTargetRow;
        public int enPassantTargetCol;
        public int enPassantPawnRow;
        public int enPassantPawnCol;
        public bool whiteKingMoved;
        public bool blackKingMoved;
        public bool whiteLeftRookMoved;
        public bool whiteRightRookMoved;
        public bool blackLeftRookMoved;
        public bool blackRightRookMoved;
    }

    private struct MoveRecord
    {
        public BoardSnapshot previousState;
        public string movedColor;

        public MoveRecord(BoardSnapshot previousState, string movedColor)
        {
            this.previousState = previousState;
            this.movedColor = movedColor;
        }
    }
    private GameMode gameMode = GameMode.PvP;
    private GameMode pendingAiMode = GameMode.EasyAI;

    private string playerColor = "white";
    private string aiColor = "black";

    private const int maxUndoCountPerSide = 3;
    private int whiteUndoRemaining = maxUndoCountPerSide;
    private int blackUndoRemaining = maxUndoCountPerSide;

    private readonly Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();

    private bool undoLockedUntilNextMove = false;

    [Header("Board UI")]
    [SerializeField] private Transform boardParent;
    [SerializeField] private GameObject squarePrefab;

    [Header("Promotion UI")]
    [SerializeField] private GameObject promotionPanel;
    [SerializeField] private Button queenButton;
    [SerializeField] private Button rookButton;
    [SerializeField] private Button bishopButton;
    [SerializeField] private Button knightButton;

    [Header("Game UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button pvpButton;
    [SerializeField] private Button easyAiButton;
    [SerializeField] private Button hardAiButton;
    [SerializeField] private Button exitGameButton;

    [Header("AI Color Choice UI")]
    [SerializeField] private GameObject aiColorChoicePanel;
    [SerializeField] private Button playWhiteButton;
    [SerializeField] private Button playBlackButton;


    [Header("White Pieces")]
    [SerializeField] private Sprite whiteKing;
    [SerializeField] private Sprite whiteQueen;
    [SerializeField] private Sprite whiteRook;
    [SerializeField] private Sprite whiteBishop;
    [SerializeField] private Sprite whiteKnight;
    [SerializeField] private Sprite whitePawn;

    [Header("Black Pieces")]
    [SerializeField] private Sprite blackKing;
    [SerializeField] private Sprite blackQueen;
    [SerializeField] private Sprite blackRook;
    [SerializeField] private Sprite blackBishop;
    [SerializeField] private Sprite blackKnight;
    [SerializeField] private Sprite blackPawn;

    private readonly Button[,] squareButtons = new Button[8, 8];
    private readonly Image[,] pieceImages = new Image[8, 8];

    private readonly Color lightSquare = new Color32(238, 238, 210, 255);
    private readonly Color darkSquare = new Color32(118, 150, 86, 255);
    private readonly Color selectedSquare = new Color32(255, 215, 0, 255);
    private readonly Color legalMoveSquare = new Color32(144, 238, 144, 255);

    private string[,] board = new string[8, 8];

    private int selectedRow = -1;
    private int selectedCol = -1;

    private int promotionRow = -1;
    private int promotionCol = -1;
    private string promotionColor = "";

    private bool waitingForPromotion = false;
    private bool gameOver = false;

    private int enPassantTargetRow = -1;
    private int enPassantTargetCol = -1;
    private int enPassantPawnRow = -1;
    private int enPassantPawnCol = -1;

    private bool whiteKingMoved = false;
    private bool blackKingMoved = false;
    private bool whiteLeftRookMoved = false;
    private bool whiteRightRookMoved = false;
    private bool blackLeftRookMoved = false;
    private bool blackRightRookMoved = false;

    private string currentTurn = "white";
    private const float aiMoveDelay = 0.35f;

    private void Start()
    {
        SetupPromotionButtons();
        SetupGameButtons();
        HideAiColorChoicePanel();
        SetupInitialBoard();
        CreateBoard();
        RenderBoard();

        UpdateGameStateMessage();
        UpdateUndoButtonLabel();
    }

    private void SetupPromotionButtons()
    {
        if (promotionPanel != null)
            promotionPanel.SetActive(false);

        if (queenButton != null)
        {
            queenButton.onClick.RemoveAllListeners();
            queenButton.onClick.AddListener(() => ChoosePromotionPiece("queen"));
        }

        if (rookButton != null)
        {
            rookButton.onClick.RemoveAllListeners();
            rookButton.onClick.AddListener(() => ChoosePromotionPiece("rook"));
        }

        if (bishopButton != null)
        {
            bishopButton.onClick.RemoveAllListeners();
            bishopButton.onClick.AddListener(() => ChoosePromotionPiece("bishop"));
        }

        if (knightButton != null)
        {
            knightButton.onClick.RemoveAllListeners();
            knightButton.onClick.AddListener(() => ChoosePromotionPiece("knight"));
        }
    }

    private void SetupGameButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(NewGame);
        }

        if (undoButton != null)
        {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(UndoMove);
        }

        if (pvpButton != null)
        {
            pvpButton.onClick.RemoveAllListeners();
            pvpButton.onClick.AddListener(StartPvpGame);
        }
        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveAllListeners();
            exitGameButton.onClick.AddListener(ExitGame);
        }
        if (playWhiteButton != null)
        {
            playWhiteButton.onClick.RemoveAllListeners();
            playWhiteButton.onClick.AddListener(() => ChoosePlayerColorAndStartAiGame("white"));
        }

        if (playBlackButton != null)
        {
            playBlackButton.onClick.RemoveAllListeners();
            playBlackButton.onClick.AddListener(() => ChoosePlayerColorAndStartAiGame("black"));
        }

        if (easyAiButton != null)
        {
            easyAiButton.onClick.RemoveAllListeners();
            easyAiButton.onClick.AddListener(StartEasyAiGame);
        }

        if (hardAiButton != null)
        {
            hardAiButton.onClick.RemoveAllListeners();
            hardAiButton.onClick.AddListener(StartHardAiGame);
        }

    }

    private void StartPvpGame()
    {
        HideAiColorChoicePanel();
        gameMode = GameMode.PvP;
        playerColor = "white";
        aiColor = "";
        NewGame();
    }

    private void StartEasyAiGame()
    {
        ShowAiColorChoicePanel(GameMode.EasyAI);
    }

    private void StartHardAiGame()
    {
        ShowAiColorChoicePanel(GameMode.HardAI);
    }

    private void ShowAiColorChoicePanel(GameMode selectedAiMode)
    {
        pendingAiMode = selectedAiMode;

        if (aiColorChoicePanel != null)
        {
            aiColorChoicePanel.SetActive(true);
            UpdateStatusText($"Đã chọn {GetModeLabel(selectedAiMode)}. Chọn màu của bạn: White hoặc Black.");
            return;
        }

        ChoosePlayerColorAndStartAiGame("white");
    }

    private void HideAiColorChoicePanel()
    {
        if (aiColorChoicePanel != null)
            aiColorChoicePanel.SetActive(false);
    }

    private void ChoosePlayerColorAndStartAiGame(string chosenPlayerColor)
    {
        playerColor = chosenPlayerColor;
        aiColor = GetOppositeColor(playerColor);
        gameMode = pendingAiMode;

        HideAiColorChoicePanel();
        NewGame();
    }

    private void NewGame()
    {
        CancelInvoke(nameof(MakeAiMove));

        selectedRow = -1;
        selectedCol = -1;

        promotionRow = -1;
        promotionCol = -1;
        promotionColor = "";

        waitingForPromotion = false;
        gameOver = false;
        currentTurn = "white";

        ResetUndoState();
        ClearMoveHistory();
        ClearEnPassantState();
        ResetCastlingState();

        if (promotionPanel != null)
            promotionPanel.SetActive(false);

        SetupInitialBoard();
        RenderBoard();
        UpdateGameStateMessage();
        UpdateUndoButtonLabel();
        TryMakeAiMoveIfNeeded();
    }

    private void SetupInitialBoard()
    {
        board = new string[8, 8];

        board[0, 0] = "black_rook";
        board[0, 1] = "black_knight";
        board[0, 2] = "black_bishop";
        board[0, 3] = "black_queen";
        board[0, 4] = "black_king";
        board[0, 5] = "black_bishop";
        board[0, 6] = "black_knight";
        board[0, 7] = "black_rook";

        for (int col = 0; col < 8; col++)
            board[1, col] = "black_pawn";

        for (int col = 0; col < 8; col++)
            board[6, col] = "white_pawn";

        board[7, 0] = "white_rook";
        board[7, 1] = "white_knight";
        board[7, 2] = "white_bishop";
        board[7, 3] = "white_queen";
        board[7, 4] = "white_king";
        board[7, 5] = "white_bishop";
        board[7, 6] = "white_knight";
        board[7, 7] = "white_rook";
    }

    private void CreateBoard()
    {
        ClearOldSquares();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int capturedRow = row;
                int capturedCol = col;

                GameObject squareObject = Instantiate(squarePrefab, boardParent);
                squareObject.name = $"Square_{row}_{col}";

                Button button = squareObject.GetComponent<Button>();
                Image squareImage = squareObject.GetComponent<Image>();
                Transform pieceImageTransform = squareObject.transform.Find("PieceImage");

                if (button == null || squareImage == null || pieceImageTransform == null)
                {
                    Debug.LogError("SquarePrefab thiếu Button, Image, hoặc child PieceImage.");
                    return;
                }

                Image pieceImage = pieceImageTransform.GetComponent<Image>();

                if (pieceImage == null)
                {
                    Debug.LogError("PieceImage thiếu component Image.");
                    return;
                }

                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSquareClicked(capturedRow, capturedCol));

                squareButtons[row, col] = button;
                pieceImages[row, col] = pieceImage;
            }
        }
    }

    private void RenderBoard()
    {
        RenderSquares();
        RenderPieces();
    }

    private void RenderSquares()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Image squareImage = squareButtons[row, col].GetComponent<Image>();
                squareImage.color = GetSquareColor(row, col);
            }
        }

        if (selectedRow >= 0 && selectedCol >= 0)
        {
            HighlightLegalMovesForSelectedPiece();

            Image selectedImage = squareButtons[selectedRow, selectedCol].GetComponent<Image>();
            selectedImage.color = selectedSquare;
        }
    }

    private void HighlightLegalMovesForSelectedPiece()
    {
        string selectedPiece = board[selectedRow, selectedCol];

        if (string.IsNullOrEmpty(selectedPiece))
            return;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (IsLegalMove(selectedRow, selectedCol, row, col))
                {
                    Image squareImage = squareButtons[row, col].GetComponent<Image>();
                    squareImage.color = legalMoveSquare;
                }
            }
        }
    }

    private void RenderPieces()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                string pieceName = board[row, col];
                Image pieceImage = pieceImages[row, col];

                if (string.IsNullOrEmpty(pieceName))
                {
                    pieceImage.sprite = null;
                    pieceImage.enabled = false;
                    continue;
                }

                pieceImage.sprite = GetPieceSprite(pieceName);
                pieceImage.enabled = true;
                pieceImage.preserveAspect = true;
                pieceImage.raycastTarget = false;

                ApplyPieceImageSize(pieceImage, pieceName);
            }
        }
    }

    private void ApplyPieceImageSize(Image pieceImage, string pieceName)
    {
        RectTransform rectTransform = pieceImage.rectTransform;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        if (pieceName == "white_pawn" || pieceName == "black_pawn")
        {
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);
        }
        else
        {
            rectTransform.offsetMin = new Vector2(6, 6);
            rectTransform.offsetMax = new Vector2(-6, -6);
        }
    }

    private void OnSquareClicked(int row, int col)
    {
        if (gameOver)
        {
            UpdateStatusText("Game over. Bấm New Game để chơi lại.");
            return;
        }

        if (waitingForPromotion)
        {
            UpdateStatusText("Đang chờ chọn quân phong cấp.");
            return;
        }

        if (IsPveMode() && currentTurn == aiColor)
        {
            UpdateStatusText("Đang chờ AI đi...");
            return;
        }

        string clickedPiece = board[row, col];

        if (selectedRow == -1 || selectedCol == -1)
        {
            TrySelectPiece(row, col, clickedPiece);
            return;
        }

        if (!string.IsNullOrEmpty(clickedPiece) && IsPieceColor(clickedPiece, currentTurn))
        {
            TrySelectPiece(row, col, clickedPiece);
            return;
        }

        TryMoveSelectedPieceTo(row, col);
    }

    private void TrySelectPiece(int row, int col, string pieceName)
    {
        if (string.IsNullOrEmpty(pieceName))
        {
            UpdateStatusText("Ô trống. Chọn quân trước.");
            return;
        }

        if (!IsPieceColor(pieceName, currentTurn))
        {
            UpdateStatusText($"Không phải lượt của {GetPieceColor(pieceName)}. Hiện tại là lượt {currentTurn}.");
            return;
        }

        selectedRow = row;
        selectedCol = col;

        RenderBoard();
        UpdateStatusText($"Selected {pieceName}.");
    }

    private void TryMoveSelectedPieceTo(int targetRow, int targetCol)
    {
        if (selectedRow < 0 || selectedCol < 0)
            return;

        string selectedPiece = board[selectedRow, selectedCol];

        if (string.IsNullOrEmpty(selectedPiece))
        {
            ClearSelection();
            RenderBoard();
            return;
        }

        if (!IsLegalMove(selectedRow, selectedCol, targetRow, targetCol))
        {
            ClearSelection();
            RenderBoard();
            UpdateStatusText("Nước đi không hợp lệ hoặc khiến vua bị chiếu.");
            return;
        }

        ExecuteMove(selectedRow, selectedCol, targetRow, targetCol, false);
    }

    private void ExecuteMove(int fromRow, int fromCol, int targetRow, int targetCol, bool autoPromoteToQueen)
    {
        string selectedPiece = board[fromRow, fromCol];

        if (string.IsNullOrEmpty(selectedPiece))
            return;

        undoLockedUntilNextMove = false;

        string movingColor = GetPieceColor(selectedPiece);
        SaveMoveHistory(movingColor);
        bool isCastlingMove =
            GetPieceType(selectedPiece) == "king" &&
            Mathf.Abs(targetCol - fromCol) == 2;

        bool isEnPassantMove = IsEnPassantMove(fromRow, fromCol, targetRow, targetCol);

        board[targetRow, targetCol] = selectedPiece;
        board[fromRow, fromCol] = null;

        if (isEnPassantMove)
            CaptureEnPassantPawn();

        if (isCastlingMove)
            MoveRookForCastling(targetRow, targetCol);

        UpdateMovedPieceFlags(selectedPiece, fromRow, fromCol);
        UpdateEnPassantStateAfterMove(selectedPiece, fromRow, fromCol, targetRow, targetCol);

        ClearSelection();

        if (IsPawnPromotionSquare(selectedPiece, targetRow))
        {
            if (autoPromoteToQueen)
            {
                board[targetRow, targetCol] = GetPieceColor(selectedPiece) + "_queen";
            }
            else
            {
                RenderBoard();
                StartPromotion(selectedPiece, targetRow, targetCol);
                return;
            }
        }

        RenderBoard();
        SwitchTurn();
        UpdateGameStateMessage();
        UpdateUndoButtonLabel();
        TryMakeAiMoveIfNeeded();
    }
    private void MoveRookForCastling(int kingRow, int kingCol)
    {
        // King-side castling: king tới cột 6, rook từ cột 7 sang cột 5
        if (kingCol == 6)
        {
            board[kingRow, 5] = board[kingRow, 7];
            board[kingRow, 7] = null;
            return;
        }

        // Queen-side castling: king tới cột 2, rook từ cột 0 sang cột 3
        if (kingCol == 2)
        {
            board[kingRow, 3] = board[kingRow, 0];
            board[kingRow, 0] = null;
        }
    }

    private void UpdateMovedPieceFlags(string pieceName, int fromRow, int fromCol)
    {
        if (pieceName == "white_king")
        {
            whiteKingMoved = true;
            return;
        }

        if (pieceName == "black_king")
        {
            blackKingMoved = true;
            return;
        }

        if (pieceName == "white_rook")
        {
            if (fromRow == 7 && fromCol == 0)
                whiteLeftRookMoved = true;

            if (fromRow == 7 && fromCol == 7)
                whiteRightRookMoved = true;

            return;
        }

        if (pieceName == "black_rook")
        {
            if (fromRow == 0 && fromCol == 0)
                blackLeftRookMoved = true;

            if (fromRow == 0 && fromCol == 7)
                blackRightRookMoved = true;
        }
    }
    private bool IsPawnPromotionSquare(string pieceName, int row)
    {
        return (pieceName == "white_pawn" && row == 0) ||
               (pieceName == "black_pawn" && row == 7);
    }

    private void StartPromotion(string pawnName, int row, int col)
    {
        promotionRow = row;
        promotionCol = col;
        promotionColor = GetPieceColor(pawnName);
        waitingForPromotion = true;

        if (promotionPanel != null)
            promotionPanel.SetActive(true);

        UpdateStatusText("Chọn quân để phong cấp.");
    }

    private void ChoosePromotionPiece(string pieceType)
    {
        if (!waitingForPromotion)
            return;

        board[promotionRow, promotionCol] = promotionColor + "_" + pieceType;

        waitingForPromotion = false;
        promotionRow = -1;
        promotionCol = -1;
        promotionColor = "";

        if (promotionPanel != null)
            promotionPanel.SetActive(false);

        RenderBoard();
        SwitchTurn();
        UpdateGameStateMessage();
        UpdateUndoButtonLabel();
        TryMakeAiMoveIfNeeded();
    }

    private bool IsLegalMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (!IsBasicLegalMove(fromRow, fromCol, toRow, toCol))
            return false;

        string movingPiece = board[fromRow, fromCol];
        string movingColor = GetPieceColor(movingPiece);

        return !DoesMoveLeaveKingInCheck(fromRow, fromCol, toRow, toCol, movingColor);
    }
    private bool IsEnPassantMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        string movingPiece = board[fromRow, fromCol];

        if (string.IsNullOrEmpty(movingPiece))
            return false;

        if (GetPieceType(movingPiece) != "pawn")
            return false;

        if (toRow != enPassantTargetRow || toCol != enPassantTargetCol)
            return false;

        if (enPassantPawnRow < 0 || enPassantPawnCol < 0)
            return false;

        string capturedPawn = board[enPassantPawnRow, enPassantPawnCol];

        if (string.IsNullOrEmpty(capturedPawn))
            return false;

        if (GetPieceType(capturedPawn) != "pawn")
            return false;

        if (GetPieceColor(capturedPawn) == GetPieceColor(movingPiece))
            return false;

        return true;
    }

    private void CaptureEnPassantPawn()
    {
        if (enPassantPawnRow < 0 || enPassantPawnCol < 0)
            return;

        board[enPassantPawnRow, enPassantPawnCol] = null;
    }

    private void UpdateEnPassantStateAfterMove(
        string movedPiece,
        int fromRow,
        int fromCol,
        int toRow,
        int toCol)
    {
        ClearEnPassantState();

        if (GetPieceType(movedPiece) != "pawn")
            return;

        int rowDifference = Mathf.Abs(toRow - fromRow);

        if (rowDifference != 2)
            return;

        string color = GetPieceColor(movedPiece);
        int direction = color == "white" ? -1 : 1;

        enPassantTargetRow = fromRow + direction;
        enPassantTargetCol = fromCol;

        enPassantPawnRow = toRow;
        enPassantPawnCol = toCol;
    }

    private void ClearEnPassantState()
    {
        enPassantTargetRow = -1;
        enPassantTargetCol = -1;
        enPassantPawnRow = -1;
        enPassantPawnCol = -1;
    }

    private void ResetCastlingState()
    {
        whiteKingMoved = false;
        blackKingMoved = false;
        whiteLeftRookMoved = false;
        whiteRightRookMoved = false;
        blackLeftRookMoved = false;
        blackRightRookMoved = false;
    }
    private bool IsBasicLegalMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (!IsInsideBoard(toRow, toCol))
            return false;

        if (fromRow == toRow && fromCol == toCol)
            return false;

        string movingPiece = board[fromRow, fromCol];
        string targetPiece = board[toRow, toCol];

        if (string.IsNullOrEmpty(movingPiece))
            return false;

        if (!string.IsNullOrEmpty(targetPiece))
        {
            if (GetPieceColor(targetPiece) == GetPieceColor(movingPiece))
                return false;

            if (GetPieceType(targetPiece) == "king")
                return false;
        }

        string pieceType = GetPieceType(movingPiece);

        return pieceType switch
        {
            "pawn" => IsLegalPawnMove(fromRow, fromCol, toRow, toCol, movingPiece),
            "knight" => IsLegalKnightMove(fromRow, fromCol, toRow, toCol),
            "bishop" => IsLegalBishopMove(fromRow, fromCol, toRow, toCol),
            "rook" => IsLegalRookMove(fromRow, fromCol, toRow, toCol),
            "queen" => IsLegalQueenMove(fromRow, fromCol, toRow, toCol),
            "king" => IsLegalKingMove(fromRow, fromCol, toRow, toCol),
            _ => false
        };
    }

    private bool DoesMoveLeaveKingInCheck(int fromRow, int fromCol, int toRow, int toCol, string kingColor)
    {
        string movingPiece = board[fromRow, fromCol];
        string capturedPiece = board[toRow, toCol];

        board[toRow, toCol] = movingPiece;
        board[fromRow, fromCol] = null;

        bool kingInCheck = IsKingInCheck(kingColor);

        board[fromRow, fromCol] = movingPiece;
        board[toRow, toCol] = capturedPiece;

        return kingInCheck;
    }

    private bool IsLegalPawnMove(int fromRow, int fromCol, int toRow, int toCol, string pieceName)
    {
        string color = GetPieceColor(pieceName);

        int direction = color == "white" ? -1 : 1;
        int startRow = color == "white" ? 6 : 1;

        int rowDifference = toRow - fromRow;
        int colDifference = Mathf.Abs(toCol - fromCol);

        string targetPiece = board[toRow, toCol];

        bool movingForwardOne =
            rowDifference == direction &&
            toCol == fromCol &&
            string.IsNullOrEmpty(targetPiece);

        if (movingForwardOne)
            return true;

        bool movingForwardTwo =
            fromRow == startRow &&
            rowDifference == direction * 2 &&
            toCol == fromCol &&
            string.IsNullOrEmpty(targetPiece) &&
            string.IsNullOrEmpty(board[fromRow + direction, fromCol]);

        if (movingForwardTwo)
            return true;

        bool normalCapture =
            rowDifference == direction &&
            colDifference == 1 &&
            !string.IsNullOrEmpty(targetPiece) &&
            GetPieceColor(targetPiece) != color;

        if (normalCapture)
            return true;

        bool enPassantCapture =
            rowDifference == direction &&
            colDifference == 1 &&
            string.IsNullOrEmpty(targetPiece) &&
            IsEnPassantMove(fromRow, fromCol, toRow, toCol);

        return enPassantCapture;
    }

    private bool IsLegalKnightMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowDifference = Mathf.Abs(toRow - fromRow);
        int colDifference = Mathf.Abs(toCol - fromCol);

        return (rowDifference == 2 && colDifference == 1) ||
               (rowDifference == 1 && colDifference == 2);
    }

    private bool IsLegalBishopMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowDifference = Mathf.Abs(toRow - fromRow);
        int colDifference = Mathf.Abs(toCol - fromCol);

        if (rowDifference != colDifference)
            return false;

        return IsPathClear(fromRow, fromCol, toRow, toCol);
    }

    private bool IsLegalRookMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        bool sameRow = fromRow == toRow;
        bool sameCol = fromCol == toCol;

        if (!sameRow && !sameCol)
            return false;

        return IsPathClear(fromRow, fromCol, toRow, toCol);
    }

    private bool IsLegalQueenMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        return IsLegalRookMove(fromRow, fromCol, toRow, toCol) ||
               IsLegalBishopMove(fromRow, fromCol, toRow, toCol);
    }

    private bool IsLegalKingMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowDifference = Mathf.Abs(toRow - fromRow);
        int colDifference = Mathf.Abs(toCol - fromCol);

        if (rowDifference <= 1 && colDifference <= 1)
            return true;

        if (rowDifference == 0 && colDifference == 2)
            return IsLegalCastlingMove(fromRow, fromCol, toRow, toCol);

        return false;
    }
    private bool IsLegalCastlingMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        string king = board[fromRow, fromCol];

        if (string.IsNullOrEmpty(king))
            return false;

        if (GetPieceType(king) != "king")
            return false;

        string color = GetPieceColor(king);

        if (color == "white")
        {
            if (fromRow != 7 || fromCol != 4)
                return false;

            if (whiteKingMoved)
                return false;

            if (toRow != 7)
                return false;

            if (toCol == 6)
                return CanCastleWhiteKingSide();

            if (toCol == 2)
                return CanCastleWhiteQueenSide();

            return false;
        }

        if (color == "black")
        {
            if (fromRow != 0 || fromCol != 4)
                return false;

            if (blackKingMoved)
                return false;

            if (toRow != 0)
                return false;

            if (toCol == 6)
                return CanCastleBlackKingSide();

            if (toCol == 2)
                return CanCastleBlackQueenSide();

            return false;
        }

        return false;
    }

    private bool CanCastleWhiteKingSide()
    {
        if (whiteRightRookMoved)
            return false;

        if (board[7, 7] != "white_rook")
            return false;

        if (!string.IsNullOrEmpty(board[7, 5]) || !string.IsNullOrEmpty(board[7, 6]))
            return false;

        if (IsKingInCheck("white"))
            return false;

        if (IsSquareAttackedByColor(7, 5, "black"))
            return false;

        if (IsSquareAttackedByColor(7, 6, "black"))
            return false;

        return true;
    }

    private bool CanCastleWhiteQueenSide()
    {
        if (whiteKingMoved)
        {
            UpdateStatusText("Không thể nhập thành trái: white king đã từng di chuyển.");
            return false;
        }

        if (whiteLeftRookMoved)
        {
            UpdateStatusText("Không thể nhập thành trái: rook a1 đã từng di chuyển.");
            return false;
        }

        if (board[7, 0] != "white_rook")
        {
            UpdateStatusText("Không thể nhập thành trái: không còn rook ở a1.");
            return false;
        }

        if (!string.IsNullOrEmpty(board[7, 1]))
        {
            UpdateStatusText("Không thể nhập thành trái: ô b1 chưa trống.");
            return false;
        }

        if (!string.IsNullOrEmpty(board[7, 2]))
        {
            UpdateStatusText("Không thể nhập thành trái: ô c1 chưa trống.");
            return false;
        }

        if (!string.IsNullOrEmpty(board[7, 3]))
        {
            UpdateStatusText("Không thể nhập thành trái: ô d1 chưa trống.");
            return false;
        }

        if (IsKingInCheck("white"))
        {
            UpdateStatusText("Không thể nhập thành trái: vua đang bị chiếu.");
            return false;
        }

        if (IsSquareAttackedByColor(7, 3, "black"))
        {
            UpdateStatusText("Không thể nhập thành trái: ô d1 đang bị tấn công.");
            return false;
        }

        if (IsSquareAttackedByColor(7, 2, "black"))
        {
            UpdateStatusText("Không thể nhập thành trái: ô c1 đang bị tấn công.");
            return false;
        }

        return true;
    }

    private bool CanCastleBlackKingSide()
    {
        if (blackRightRookMoved)
            return false;

        if (board[0, 7] != "black_rook")
            return false;

        if (!string.IsNullOrEmpty(board[0, 5]) || !string.IsNullOrEmpty(board[0, 6]))
            return false;

        if (IsKingInCheck("black"))
            return false;

        if (IsSquareAttackedByColor(0, 5, "white"))
            return false;

        if (IsSquareAttackedByColor(0, 6, "white"))
            return false;

        return true;
    }

    private bool CanCastleBlackQueenSide()
    {
        if (blackLeftRookMoved)
            return false;

        if (board[0, 0] != "black_rook")
            return false;

        if (!string.IsNullOrEmpty(board[0, 1]) ||
            !string.IsNullOrEmpty(board[0, 2]) ||
            !string.IsNullOrEmpty(board[0, 3]))
            return false;

        if (IsKingInCheck("black"))
            return false;

        if (IsSquareAttackedByColor(0, 3, "white"))
            return false;

        if (IsSquareAttackedByColor(0, 2, "white"))
            return false;

        return true;
    }
    private bool IsPathClear(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowStep = GetStep(toRow - fromRow);
        int colStep = GetStep(toCol - fromCol);

        int currentRow = fromRow + rowStep;
        int currentCol = fromCol + colStep;

        while (currentRow != toRow || currentCol != toCol)
        {
            if (!string.IsNullOrEmpty(board[currentRow, currentCol]))
                return false;

            currentRow += rowStep;
            currentCol += colStep;
        }

        return true;
    }

    private bool IsKingInCheck(string kingColor)
    {
        FindKing(kingColor, out int kingRow, out int kingCol);

        if (kingRow == -1 || kingCol == -1)
            return true;

        string enemyColor = kingColor == "white" ? "black" : "white";

        return IsSquareAttackedByColor(kingRow, kingCol, enemyColor);
    }

    private void FindKing(string color, out int kingRow, out int kingCol)
    {
        kingRow = -1;
        kingCol = -1;

        string kingName = color + "_king";

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] == kingName)
                {
                    kingRow = row;
                    kingCol = col;
                    return;
                }
            }
        }
    }

    private bool IsSquareAttackedByColor(int targetRow, int targetCol, string attackerColor)
    {
        return IsAttackedByPawn(targetRow, targetCol, attackerColor) ||
               IsAttackedByKnight(targetRow, targetCol, attackerColor) ||
               IsAttackedByKing(targetRow, targetCol, attackerColor) ||
               IsAttackedBySlidingPiece(targetRow, targetCol, attackerColor);
    }

    private bool IsAttackedByPawn(int targetRow, int targetCol, string attackerColor)
    {
        int pawnDirection = attackerColor == "white" ? -1 : 1;
        int pawnRow = targetRow - pawnDirection;

        return HasPiece(pawnRow, targetCol - 1, attackerColor + "_pawn") ||
               HasPiece(pawnRow, targetCol + 1, attackerColor + "_pawn");
    }

    private bool IsAttackedByKnight(int targetRow, int targetCol, string attackerColor)
    {
        int[,] offsets =
        {
            { -2, -1 }, { -2, 1 },
            { -1, -2 }, { -1, 2 },
            { 1, -2 }, { 1, 2 },
            { 2, -1 }, { 2, 1 }
        };

        for (int i = 0; i < offsets.GetLength(0); i++)
        {
            int row = targetRow + offsets[i, 0];
            int col = targetCol + offsets[i, 1];

            if (HasPiece(row, col, attackerColor + "_knight"))
                return true;
        }

        return false;
    }

    private bool IsAttackedByKing(int targetRow, int targetCol, string attackerColor)
    {
        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int colOffset = -1; colOffset <= 1; colOffset++)
            {
                if (rowOffset == 0 && colOffset == 0)
                    continue;

                int row = targetRow + rowOffset;
                int col = targetCol + colOffset;

                if (HasPiece(row, col, attackerColor + "_king"))
                    return true;
            }
        }

        return false;
    }

    private bool IsAttackedBySlidingPiece(int targetRow, int targetCol, string attackerColor)
    {
        int[,] rookDirections =
        {
            { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }
        };

        int[,] bishopDirections =
        {
            { -1, -1 }, { -1, 1 }, { 1, -1 }, { 1, 1 }
        };

        if (IsAttackedInDirections(targetRow, targetCol, attackerColor, rookDirections, "rook", "queen"))
            return true;

        if (IsAttackedInDirections(targetRow, targetCol, attackerColor, bishopDirections, "bishop", "queen"))
            return true;

        return false;
    }

    private bool IsAttackedInDirections(
        int targetRow,
        int targetCol,
        string attackerColor,
        int[,] directions,
        string mainAttacker,
        string secondaryAttacker)
    {
        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int rowStep = directions[i, 0];
            int colStep = directions[i, 1];

            int row = targetRow + rowStep;
            int col = targetCol + colStep;

            while (IsInsideBoard(row, col))
            {
                string pieceName = board[row, col];

                if (string.IsNullOrEmpty(pieceName))
                {
                    row += rowStep;
                    col += colStep;
                    continue;
                }

                if (GetPieceColor(pieceName) != attackerColor)
                    break;

                string pieceType = GetPieceType(pieceName);

                if (pieceType == mainAttacker || pieceType == secondaryAttacker)
                    return true;

                break;
            }
        }

        return false;
    }

    private bool HasPiece(int row, int col, string pieceName)
    {
        if (!IsInsideBoard(row, col))
            return false;

        return board[row, col] == pieceName;
    }

    private bool HasAnyLegalMove(string color)
    {
        for (int fromRow = 0; fromRow < 8; fromRow++)
        {
            for (int fromCol = 0; fromCol < 8; fromCol++)
            {
                string pieceName = board[fromRow, fromCol];

                if (string.IsNullOrEmpty(pieceName))
                    continue;

                if (!IsPieceColor(pieceName, color))
                    continue;

                for (int toRow = 0; toRow < 8; toRow++)
                {
                    for (int toCol = 0; toCol < 8; toCol++)
                    {
                        if (IsLegalMove(fromRow, fromCol, toRow, toCol))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsPveMode()
    {
        return gameMode == GameMode.EasyAI || gameMode == GameMode.HardAI;
    }

    private string GetModeLabel()
    {
        return GetModeLabel(gameMode);
    }

    private string GetModeLabel(GameMode mode)
    {
        return mode switch
        {
            GameMode.EasyAI => "AI Easy",
            GameMode.HardAI => "AI Hard",
            _ => "PvP"
        };
    }

    private void TryMakeAiMoveIfNeeded()
    {
        if (!IsPveMode())
            return;

        if (gameOver || waitingForPromotion)
            return;

        if (currentTurn != aiColor)
            return;

        CancelInvoke(nameof(MakeAiMove));
        Invoke(nameof(MakeAiMove), aiMoveDelay);
    }

    private void MakeAiMove()
    {
        if (!IsPveMode() || gameOver || waitingForPromotion || currentTurn != aiColor)
            return;

        List<ChessMove> legalMoves = GetAllLegalMoves(aiColor);

        if (legalMoves.Count == 0)
        {
            UpdateGameStateMessage();
            return;
        }

        ChessMove selectedMove = gameMode == GameMode.EasyAI
            ? ChooseRandomMove(legalMoves)
            : ChooseBestMove(legalMoves, aiColor);

        ExecuteMove(selectedMove.fromRow, selectedMove.fromCol, selectedMove.toRow, selectedMove.toCol, true);
    }

    private List<ChessMove> GetAllLegalMoves(string color)
    {
        List<ChessMove> legalMoves = new List<ChessMove>();

        for (int fromRow = 0; fromRow < 8; fromRow++)
        {
            for (int fromCol = 0; fromCol < 8; fromCol++)
            {
                string pieceName = board[fromRow, fromCol];

                if (string.IsNullOrEmpty(pieceName))
                    continue;

                if (!IsPieceColor(pieceName, color))
                    continue;

                for (int toRow = 0; toRow < 8; toRow++)
                {
                    for (int toCol = 0; toCol < 8; toCol++)
                    {
                        if (IsLegalMove(fromRow, fromCol, toRow, toCol))
                            legalMoves.Add(new ChessMove(fromRow, fromCol, toRow, toCol));
                    }
                }
            }
        }

        return legalMoves;
    }

    private ChessMove ChooseRandomMove(List<ChessMove> legalMoves)
    {
        int index = Random.Range(0, legalMoves.Count);
        return legalMoves[index];
    }

    private ChessMove ChooseBestMove(List<ChessMove> legalMoves, string aiMoveColor)
    {
        int bestScore = int.MinValue;
        List<ChessMove> bestMoves = new List<ChessMove>();

        foreach (ChessMove move in legalMoves)
        {
            int score = ScoreMove(move, aiMoveColor);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        return ChooseRandomMove(bestMoves);
    }

    private int ScoreMove(ChessMove move, string aiMoveColor)
    {
        string movingPiece = board[move.fromRow, move.fromCol];
        string capturedPiece = board[move.toRow, move.toCol];

        int score = 0;

        if (!string.IsNullOrEmpty(capturedPiece))
            score += PieceValue(capturedPiece) * 10 - PieceValue(movingPiece);

        if (IsPawnPromotionSquare(movingPiece, move.toRow))
            score += PieceValue(aiMoveColor + "_queen");

        score += ScoreMoveByTemporarySimulation(move, aiMoveColor);

        return score;
    }

    private int ScoreMoveByTemporarySimulation(ChessMove move, string aiMoveColor)
    {
        string enemyColor = GetOppositeColor(aiMoveColor);
        string movingPiece = board[move.fromRow, move.fromCol];
        string capturedPiece = board[move.toRow, move.toCol];

        board[move.toRow, move.toCol] = movingPiece;
        board[move.fromRow, move.fromCol] = null;

        int score = 0;

        if (IsKingInCheck(enemyColor))
            score += 60;

        if (IsKingInCheck(enemyColor) && !HasAnyLegalMove(enemyColor))
            score += 100000;

        if (IsSquareAttackedByColor(move.toRow, move.toCol, enemyColor))
            score -= PieceValue(movingPiece) / 2;

        board[move.fromRow, move.fromCol] = movingPiece;
        board[move.toRow, move.toCol] = capturedPiece;

        return score;
    }

    private int PieceValue(string pieceName)
    {
        string pieceType = GetPieceType(pieceName);

        return pieceType switch
        {
            "pawn" => 100,
            "knight" => 320,
            "bishop" => 330,
            "rook" => 500,
            "queen" => 900,
            "king" => 20000,
            _ => 0
        };
    }
    private void SaveMoveHistory(string movedColor)
    {
        moveHistory.Push(new MoveRecord(CaptureBoardSnapshot(), movedColor));
    }

    private BoardSnapshot CaptureBoardSnapshot()
    {
        return new BoardSnapshot
        {
            board = (string[,])board.Clone(),
            currentTurn = currentTurn,
            selectedRow = selectedRow,
            selectedCol = selectedCol,
            promotionRow = promotionRow,
            promotionCol = promotionCol,
            promotionColor = promotionColor,
            waitingForPromotion = waitingForPromotion,
            gameOver = gameOver,
            enPassantTargetRow = enPassantTargetRow,
            enPassantTargetCol = enPassantTargetCol,
            enPassantPawnRow = enPassantPawnRow,
            enPassantPawnCol = enPassantPawnCol,
            whiteKingMoved = whiteKingMoved,
            blackKingMoved = blackKingMoved,
            whiteLeftRookMoved = whiteLeftRookMoved,
            whiteRightRookMoved = whiteRightRookMoved,
            blackLeftRookMoved = blackLeftRookMoved,
            blackRightRookMoved = blackRightRookMoved
        };
    }

    private void RestoreBoardSnapshot(BoardSnapshot snapshot)
    {
        board = (string[,])snapshot.board.Clone();
        currentTurn = snapshot.currentTurn;
        selectedRow = snapshot.selectedRow;
        selectedCol = snapshot.selectedCol;
        promotionRow = snapshot.promotionRow;
        promotionCol = snapshot.promotionCol;
        promotionColor = snapshot.promotionColor;
        waitingForPromotion = snapshot.waitingForPromotion;
        gameOver = snapshot.gameOver;
        enPassantTargetRow = snapshot.enPassantTargetRow;
        enPassantTargetCol = snapshot.enPassantTargetCol;
        enPassantPawnRow = snapshot.enPassantPawnRow;
        enPassantPawnCol = snapshot.enPassantPawnCol;
        whiteKingMoved = snapshot.whiteKingMoved;
        blackKingMoved = snapshot.blackKingMoved;
        whiteLeftRookMoved = snapshot.whiteLeftRookMoved;
        whiteRightRookMoved = snapshot.whiteRightRookMoved;
        blackLeftRookMoved = snapshot.blackLeftRookMoved;
        blackRightRookMoved = snapshot.blackRightRookMoved;

        if (promotionPanel != null)
            promotionPanel.SetActive(waitingForPromotion);
    }

    private void UndoMove()
    {
        CancelInvoke(nameof(MakeAiMove));

        if (waitingForPromotion)
        {
            UpdateStatusText("Đang chờ phong cấp. Hãy chọn quân phong cấp trước rồi mới undo.");
            return;
        }

        if (moveHistory.Count == 0)
        {
            UpdateStatusText("Chưa có nước đi để undo.");
            return;
        }

        if (undoLockedUntilNextMove)
        {
            UpdateStatusText("Đã undo một lần rồi. Phải đi thêm một nước mới được undo tiếp.");
            return;
        }

        if (IsPveMode())
            UndoPveMove();
        else
            UndoPvpMove();
    }

    private void UndoPvpMove()
    {
        MoveRecord record = moveHistory.Peek();

        if (!CanUseUndoForColor(record.movedColor))
        {
            UpdateStatusText($"{GetColorLabel(record.movedColor)} đã hết 3 lần undo.");
            return;
        }

        moveHistory.Pop();
        UseUndoForColor(record.movedColor);
        RestoreBoardSnapshot(record.previousState);
        undoLockedUntilNextMove = true;

        RenderBoard();
        UpdateUndoButtonLabel();
        UpdateGameStateMessage();
    }

    private void UndoPveMove()
    {
        MoveRecord lastMove = moveHistory.Peek();

        if (lastMove.movedColor == aiColor)
        {
            if (moveHistory.Count < 2)
            {
                UpdateStatusText("Chưa có nước đi của bạn để undo.");
                return;
            }

            MoveRecord aiMove = moveHistory.Pop();
            MoveRecord humanMove = moveHistory.Peek();

            if (humanMove.movedColor != playerColor)
            {
                moveHistory.Push(aiMove);
                UpdateStatusText("Không tìm thấy nước đi gần nhất của bạn để undo.");
                return;
            }

            if (!CanUseUndoForColor(playerColor))
            {
                moveHistory.Push(aiMove);
                UpdateStatusText($"Bạn đã hết 3 lần undo cho quân {GetColorLabel(playerColor)}.");
                return;
            }

            moveHistory.Pop();
            UseUndoForColor(playerColor);
            RestoreBoardSnapshot(humanMove.previousState);
        }
        else if (lastMove.movedColor == playerColor)
        {
            if (!CanUseUndoForColor(playerColor))
            {
                UpdateStatusText($"Bạn đã hết 3 lần undo cho quân {GetColorLabel(playerColor)}.");
                return;
            }

            moveHistory.Pop();
            UseUndoForColor(playerColor);
            RestoreBoardSnapshot(lastMove.previousState);
        }
        else
        {
            UpdateStatusText("Không thể undo trạng thái hiện tại.");
            return;
        }

        undoLockedUntilNextMove = true;

        RenderBoard();
        UpdateUndoButtonLabel();
        UpdateGameStateMessage();
        TryMakeAiMoveIfNeeded();
    }

    private bool CanUseUndoForColor(string color)
    {
        return GetUndoRemaining(color) > 0;
    }

    private void UseUndoForColor(string color)
    {
        if (color == "white")
            whiteUndoRemaining = Mathf.Max(0, whiteUndoRemaining - 1);
        else if (color == "black")
            blackUndoRemaining = Mathf.Max(0, blackUndoRemaining - 1);
    }

    private int GetUndoRemaining(string color)
    {
        return color == "white" ? whiteUndoRemaining : blackUndoRemaining;
    }

    private void ResetUndoState()
    {
        whiteUndoRemaining = maxUndoCountPerSide;
        blackUndoRemaining = maxUndoCountPerSide;
        undoLockedUntilNextMove = false;
    }

    private void ClearMoveHistory()
    {
        moveHistory.Clear();
    }

    private void UpdateUndoButtonLabel()
    {
        if (undoButton == null)
            return;

        string undoText = IsPveMode()
            ? $"Undo ({GetUndoRemaining(playerColor)})"
            : $"Undo W:{whiteUndoRemaining} B:{blackUndoRemaining}";

        undoButton.interactable = CanPressUndoButtonNow();

        TMP_Text tmpLabel = undoButton.GetComponentInChildren<TMP_Text>();

        if (tmpLabel != null)
        {
            tmpLabel.text = undoText;
            return;
        }

        Text legacyLabel = undoButton.GetComponentInChildren<Text>();

        if (legacyLabel != null)
            legacyLabel.text = undoText;
    }

    private bool CanPressUndoButtonNow()
    {
        if (waitingForPromotion)
            return false;

        if (undoLockedUntilNextMove)
            return false;

        if (moveHistory.Count == 0)
            return false;

        if (IsPveMode())
            return CanUseUndoForColor(playerColor);

        return CanUseUndoForColor(moveHistory.Peek().movedColor);
    }
    private void UpdateGameStateMessage()
    {
        bool inCheck = IsKingInCheck(currentTurn);
        bool hasLegalMove = HasAnyLegalMove(currentTurn);

        if (inCheck && !hasLegalMove)
        {
            string winner = currentTurn == "white" ? "black" : "white";
            gameOver = true;
            UpdateStatusText($"Checkmate. {winner} wins.");
            return;
        }

        if (!inCheck && !hasLegalMove)
        {
            gameOver = true;
            UpdateStatusText("Stalemate. Draw.");
            return;
        }

        if (inCheck)
        {
            UpdateStatusText($"{currentTurn} is in check.");
            return;
        }

        if (IsPveMode())
        {
            if (currentTurn == playerColor)
            {
                UpdateStatusText($"{GetModeLabel()} - Bạn là {GetColorLabel(playerColor)}. Lượt của bạn. Undo còn {GetUndoRemaining(playerColor)}.");
            }
            else
            {
                UpdateStatusText($"{GetModeLabel()} - AI là {GetColorLabel(aiColor)} đang nghĩ...");
            }

            return;
        }

        UpdateStatusText($"PvP - {GetColorLabel(currentTurn)} to move. Undo W:{whiteUndoRemaining} B:{blackUndoRemaining}.");
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }
    private string GetOppositeColor(string color)
    {
        return color == "white" ? "black" : "white";
    }

    private string GetColorLabel(string color)
    {
        return color == "white" ? "White" : "Black";
    }
    private int GetStep(int value)
    {
        if (value > 0)
            return 1;

        if (value < 0)
            return -1;

        return 0;
    }

    private bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }

    private void SwitchTurn()
    {
        currentTurn = currentTurn == "white" ? "black" : "white";
    }

    private void ClearSelection()
    {
        selectedRow = -1;
        selectedCol = -1;
    }

    private bool IsPieceColor(string pieceName, string color)
    {
        return pieceName.StartsWith(color + "_");
    }

    private string GetPieceColor(string pieceName)
    {
        if (pieceName.StartsWith("white_"))
            return "white";

        if (pieceName.StartsWith("black_"))
            return "black";

        return "";
    }

    private string GetPieceType(string pieceName)
    {
        if (pieceName.Contains("_king"))
            return "king";

        if (pieceName.Contains("_queen"))
            return "queen";

        if (pieceName.Contains("_rook"))
            return "rook";

        if (pieceName.Contains("_bishop"))
            return "bishop";

        if (pieceName.Contains("_knight"))
            return "knight";

        if (pieceName.Contains("_pawn"))
            return "pawn";

        return "";
    }

    private Sprite GetPieceSprite(string pieceName)
    {
        return pieceName switch
        {
            "white_king" => whiteKing,
            "white_queen" => whiteQueen,
            "white_rook" => whiteRook,
            "white_bishop" => whiteBishop,
            "white_knight" => whiteKnight,
            "white_pawn" => whitePawn,

            "black_king" => blackKing,
            "black_queen" => blackQueen,
            "black_rook" => blackRook,
            "black_bishop" => blackBishop,
            "black_knight" => blackKnight,
            "black_pawn" => blackPawn,

            _ => null
        };
    }

    private void ClearOldSquares()
    {
        if (boardParent == null)
            return;

        for (int i = boardParent.childCount - 1; i >= 0; i--)
        {
            Destroy(boardParent.GetChild(i).gameObject);
        }
    }

    private Color GetSquareColor(int row, int col)
    {
        return (row + col) % 2 == 0
            ? lightSquare
            : darkSquare;
    }
    private void ExitGame()
    {
        Debug.Log("Exit Game pressed.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
