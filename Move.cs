// immutable move, contains enough to UndoMove on it
// todo - shrink footprint?!
class Move
{
    // track from, to, square, 
    // captured piece (and square, for en passant), 
    // promoted to piece (always on landing square)
    // board extra state before move
    public Move(int r1, int c1, int r2, int c2, byte removed)
    {
        this.r1 = (byte)r1;
        this.c1 = (byte)c1;
        this.r2 = (byte)r2;
        this.c2 = (byte)c2;

        this.removed = removed;
    }

    public Move(Move move, byte piece) : this(move.r1, move.c1, move.r2, move.c2, move.removed)
    {
        promoted = piece;
    }

    // row, col from and to
    public byte r1, c1, r2, c2;   

    // this move is an en passant capture
    public bool enPassantCapture = false;

    // puts opponent in check
    public bool check = false;
    // opponent in checkmate
    public bool checkmate = false;
    
    public byte removed = State.Blank;// piece removed or blank
    public byte promoted=State.Blank; // promoted to piece, or blank            


    // is castling - todo - can check in DoMove? - but need for formatting
    public bool castle = false;

    // formatting of move
    // todo - add a few methods: a1a4, a1-a4, Ra4
    public override string ToString()
    {
        var end = ""; // capture, e.p., check, promote
        if (removed != State.Blank)
            end += "x" + State.PieceLetters[removed - 1];
        if (enPassantCapture)
            end += " e.p.";
        if (promoted != State.Blank)
            end += "=" + State.PieceLetters[promoted - 1];
        if (check)
            end += "+";
        if (checkmate)
            end += "+";
        string prefix = "";
        if (castle)
            prefix = c2 == 6 ? "O-O" : "O-O-O";
        else
            prefix = $"{Square.Name(r1, c1)}-{Square.Name(r2, c2)}";
        return prefix + end;
    }

}
