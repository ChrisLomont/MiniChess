using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// represent the state of a game of chess
class State
{
    public const byte Piece = 127;
    public const byte White = 128;
    public const byte Black = 0;
    public const byte Blank = 0;
    public const byte Pawn = 1;
    public const byte Knight = 2;
    public const byte Bishop = 3;
    public const byte Rook = 4;
    public const byte Queen = 5;
    public const byte King = 6;

    public static byte[] IndexedPieces = new[] { Pawn, Knight, Bishop, Rook, Queen, King };

    public static string[] PieceLetters = new[] { "P", "N", "B", "R", "Q", "K" };

    // return opposite color
    public static byte OtherColor(byte color)
    {
        return (byte)(color ^ White);
    }
   
    public State()
    {
        Reset();
    }

    // apply move to state
    public void DoMove(Move move)
    {
        var (r1, c1) = (move.r1, move.c1);
        var (r2, c2) = (move.r2, move.c2);

        var (p1, color1) = this[c1, r1];   // moving piece
        var (p2, color2) = this[c2, r2];   // landing square

        castlingFlags.Add(CastlingFlags);
        enPassantSquares.Add(enPassantSquare);

        // castling flags
        if (p1 == King)
        {
            if (color1 == White)
                WhiteCastleK = WhiteCastleQ = false;
            if (color1 == Black)
                BlackCastleK = BlackCastleQ = false;
        }
        if (p1 == Rook)
        {
            if (color1 == White && r1 == 0 && c1 == 0)
                WhiteCastleQ = false;
            if (color1 == White && r1 == 0 && c1 == 7)
                WhiteCastleK = false;
            if (color1 == Black && r1 == 7 && c1 == 0)
                BlackCastleQ = false;
            if (color1 == Black && r1 == 7 && c1 == 7)
                BlackCastleK = false;
        }

        var captured = p2 != Blank;

        // move piece(s)

        // normal src to dest move, empty source square
        this[c2, r2] = this[c1, r1];
        this[c1, r1] = Square.Empty;

        // special cases
        if (p1 == King && Math.Abs(c2 - c1) == 2)
        { // castling - king moved, move rook
            var dc = (c2 - c1) / 2;
            this[(c2 >> 2) * 7, r2] = Square.Empty; // erase rook
            this[c2 - dc, r2] = (Rook, color1); // set new rook
        }
        else if (p1 == Pawn && (r2 == 0 || r2 == 7))
        { // promotion
            this[c2, r2] = (move.promoted, color1); // set piece
        }
        else if (p1 == Pawn && move.enPassantCapture)
        { // enpassant capture
            captured = true;
            this[c2, r1] = Square.Empty; // erase captured square
        }

        gameMoves.Add(move);
        EnemyColor = MoveColor;
        MoveColor = OtherColor(MoveColor);

        clocks.Add(halfMoveClock);

        if (captured || p1 == Pawn)
            halfMoveClock = 0;
        else
            ++halfMoveClock;

        if (MoveColor == White)
            ++fullMoveCount;

        if (p1 == Pawn && Math.Abs(r1-r2)==2)
        { // en passant sq
            enPassantSquare = Square.Pack((r1+r2)/2,c1);
        }
        else 
            enPassantSquare = -1;
    }
    public void UndoMove()
    {
        // todo - simplify Do/Undo if possible
        if (!gameMoves.Any())
            return;
        var pos = gameMoves.Count;
        var move = gameMoves.Last();
        gameMoves.RemoveAt(pos - 1);
        halfMoveClock = clocks.Last();
        clocks.RemoveAt(pos - 1);
        enPassantSquare = enPassantSquares.Last();
        enPassantSquares.RemoveAt(pos-1);

        CastlingFlags = castlingFlags.Last();
        castlingFlags.RemoveAt(castlingFlags.Count - 1);


        var (r1, c1) = (move.r1, move.c1);
        var (r2, c2) = (move.r2, move.c2);
        var (p, c) = this[c2, r2]; // moving piece

        board[c1, r1] = board[c2, r2];
        if (move.promoted != Blank)
            this[c1, r1] = (Pawn, EnemyColor);
        this[c2, r2] = (move.removed, MoveColor);

        if (move.castle)
        { // castling - king moved, move rook
            var dc = (c2 - c1) / 2;
            this[(c2 >> 2) * 7, r2] = (Rook, c); // restore rook
            this[c2 - dc, r2] = Square.Empty;  // erase rook
        }
        else if (move.enPassantCapture)
        { // enpassant capture, undo it        
            this[c2, r2] = Square.Empty; // blank square on target
            this[c2, r1] = (Pawn, MoveColor); // pawn here
            // enPassantSquare = Square.Pack(r2, c2);
        }

        if (MoveColor == White)
            --fullMoveCount;
        MoveColor = EnemyColor;
        EnemyColor = OtherColor(EnemyColor);
    }



    // track state in flags = todo - unnecessary - expand?
    // bit 0-5: en pass sq
    // bit 6 : 0 = no en pass, 1 = en pass ok
    // bit 7,8      : white can castle K,Q side
    // bit 9,10     : black can castle K,Q side
    // -1 if none, else 0-63 = row*8+col of target square
    public ushort extraState;
    bool Get(int bit)
    {
        return ((extraState >> bit) & 1) == 1;
    }
    void Set(int bit, bool value)
    {
        var v = 1U << bit;
        if (value)
            extraState |= (ushort)v;
        else
            extraState &= (ushort)(~v);
    }


    public int enPassantSquare
    {
        get { if (Get(6)) return (extraState & 63); return -1; }
        set
        {
            extraState = (ushort)(extraState & ~63); // zero square
            if (value == -1)
                Set(6, false); // no enpassant
            else
            {
                Set(6, true); // enpassant
                extraState = (ushort)(extraState | (value & 63));
            }
        }
    }

    // white or black can castle kingside or queenside
    public bool WhiteCastleK
    {
        get { return Get(7); }
        set { Set(7, value); }
    }
    public bool WhiteCastleQ
    {
        get { return Get(8); }
        set { Set(8, value); }
    }
    public bool BlackCastleK
    {
        get { return Get(9); }
        set { Set(9, value); }
    }
    public bool BlackCastleQ
    {
        get { return Get(10); }
        set { Set(10, value); }
    }


    // find first occurrence of piece, return true if found
    public bool FindPiece(byte piece, byte color, out int row, out int col)
    {
        col = 0;
        for (row = 0; row < 8; ++row)
            for (col = 0; col < 8; ++col)
            {
                var (p, c) = this[col, row];
                if (p == piece && c == color)
                    return true;
            }
        row = col = -1;
        return false;
    }

    public byte MoveColor = White;
    public byte EnemyColor = Black;


    byte[,] board;

    // get/set square contents
    public (byte piece, byte color) this[int col, int row]
    {
        get
        {
            var q = board[col, row];
            return ((byte)(q & Piece), (byte)(q & White));
        }
        set
        {
            var a = value; // needs two stages?
            var (p, c) = a;
            board[col, row] = (byte)(p | c);
        }
    }

    // half moves since last capture or pawn advance
    public int halfMoveClock=0;

    // number of full moves, starting at 1, incremented after black move
    public int fullMoveCount=0;

    
    // format position as FEN
    public string ToFEN()
    {
        var sb = new StringBuilder();

        for (var row = 7; row >= 0; --row)
        {
            var spaceCount = 0;
            for (var col = 0; col < 8; ++col)
            {
                var (p, c) = this[col, row];
                if (p == Blank)
                    ++spaceCount;
                else
                {
                    if (spaceCount > 0)
                        sb.Append(spaceCount);
                    spaceCount = 0;
                    var symb = lp[p - 1];
                    if (c == White)
                        symb = Char.ToUpper(symb);
                    sb.Append(symb);
                }
            }
            if (spaceCount > 0)
                sb.Append(spaceCount);
            if (row != 0)
                sb.Append("/");
        }
        sb.Append(MoveColor == White ? " w " : " b ");
        var castle = "";
        castle += WhiteCastleK ? "K" : "";
        castle += WhiteCastleQ ? "Q" : "";
        castle += BlackCastleK ? "k" : "";
        castle += BlackCastleQ ? "q" : "";
        if (castle.Length == 0) castle = "-";
        sb.Append(castle);
        var ep = enPassantSquare;
        if (ep == -1)
            sb.Append(" -");
        else
        {
            Square.Unpack(ep, out var epr, out var epc);
            sb.Append($" {Square.Name(epr, epc)}");
        }
        sb.Append($" {halfMoveClock} {fullMoveCount}");
        return sb.ToString();
    }

        static string  lp = "pnbrqk";
        static string  up = "PNBRQK";


    // try to parse a FEN string
    public static bool TryParseFEN(string text, out State state)
    {
        var s = new State();
        s.ClearPieces();
        state = null;

        var words = text.Trim().Split(' ');
        if (words.Length != 6)
            return false;

        // try parsing pieces
        int row = 7, col = 0;
        foreach (var c in words[0])
        {
            if (lp.Contains(c))
            {
                // checks
                if (!Square.InBounds(row,col))
                    return false;
                s[col++, row] = (IndexedPieces[lp.IndexOf(c)], Black);
            }
            else if (up.Contains(c))
            {
                // checks
                if (!Square.InBounds(row,col))
                    return false;
                s[col++, row] = (IndexedPieces[up.IndexOf(c)], White);
            }
            else if (c == '/')
            {
                col = 0;
                row--;
            }
            else if ("12345678".Contains(c))
                col += (c - '1' + 1);
        }

        // to move
        if (words[1] == "b")
            s.MoveColor = Black;
        else if (words[1] == "w")
            s.MoveColor = White;
        else
            return false;
        s.EnemyColor = (byte)(s.MoveColor ^ White);

        // castling
        s.CastlingFlags = 0;
        if (words[2] != "-")
        {
            foreach (var c in words[2])
            {
                if (c == 'k')
                    s.BlackCastleK = true;
                else if (c == 'q')
                    s.BlackCastleQ = true;
                else if (c == 'K')
                    s.WhiteCastleK = true;
                else if (c == 'Q')
                    s.WhiteCastleQ = true;
                else
                    return false;
            }
        }

        if (words[3] == "-")
            s.enPassantSquare = -1;
        else
        {
            // en passant
            if (!Square.TryParse(words[3], out var epr, out var epc))
                return false;
            s.enPassantSquare = Square.Pack(epr, epc);
        }

        if (Int32.TryParse(words[4], out var half))
            s.halfMoveClock = half;
        else
            return false;

        if (Int32.TryParse(words[5], out var full))
            s.fullMoveCount = full;
        else
            return false;

        state = s;
        return true;
    }

    // todo - merge state stacks?
    public List<Move> gameMoves = new List<Move>();
    // track clock values for undo move
    List<int> clocks = new List<int>();
    List<int> castlingFlags = new List<int>();
    List<int> enPassantSquares = new List<int>();

    public int CastlingFlags
    {
        get
        {
            return
                (WhiteCastleK ? 1 : 0) +
                (WhiteCastleQ ? 2 : 0) +
                (BlackCastleK ? 4 : 0) +
                (BlackCastleQ ? 8 : 0)
                ;
        }
        set
        {
            WhiteCastleK = (value & 1) != 0;
            WhiteCastleQ = (value & 2) != 0;
            BlackCastleK = (value & 4) != 0;
            BlackCastleQ = (value & 8) != 0;
        }
    }

    // setup piece order
    static byte[] order = new byte[]
    {
        Rook,Knight,Bishop,Queen,King,Bishop,Knight,Rook
    };

    public void ClearPieces()
    {
        for (var col = 0; col < 8; ++col)
            for (var row = 0; row < 8; ++row)
                this[col, row] = Square.Empty;
    }

    // Set board to start state
    public void Reset()
    {
        gameMoves.Clear();
        enPassantSquares.Clear();
        clocks.Clear();
        castlingFlags.Clear();
        MoveColor = White;
        EnemyColor = Black;
        enPassantSquare = -1;
        fullMoveCount = 1;
        halfMoveClock = 0;
        WhiteCastleK = WhiteCastleQ = true;
        BlackCastleK = BlackCastleQ = true;
        board = new byte[8, 8];
        for (var col = 0; col < 8; ++col)
            for (var row = 0; row < 8; ++row)
            {
                var color = row < 2 ? White : Black;
                byte piece = 0;
                if (row == 1 || row == 6)
                    piece = Pawn;
                if (row == 0 || row == 7)
                    piece = order[col];
                this[col,row] = (piece,color);
            }
    }
}
