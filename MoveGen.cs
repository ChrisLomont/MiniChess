
//#define  CHECK_INVARIANTS // define for some sanity checks
using System;
using System.Collections.Generic;
using System.Linq;

//todo - test if can castle out of check

class MoveGen
{
    // see perft samples at http://www.rocechess.ch/perft.html
    // http://cinnamonchess.altervista.org/perft.html
    // https://stackoverflow.com/questions/11365221/perft-test-results-cant-find-bug
    // todo implement perft on known positions, test those published

    // testing of move generator
    // from given position, computes nodes at depth d
    // return count of nodes
    // divide outputs nodes under each top level move
    public static ulong Perft(State state, int depth, bool divide = false)
    {
        if (depth == 0)
            return 1U;
        ulong nodes = 0;

        var moves = GenMoves(state);
        Sort(moves);
        if (depth == 1)
            return (ulong)moves.Count;
        foreach (var move in moves)
        {
            state.DoMove(move);
            var count = Perft(state, depth - 1, false);;
            nodes += count;
            state.UndoMove();
            
            if (divide)
                Console.WriteLine($"{Square.Name(move.r1,move.c1)}{Square.Name(move.r2,move.c2)}: {count}");
        }
        if (divide)
            Console.WriteLine($"Nodes searched: {nodes}");

        return nodes;
    }

    // lexi sort moves
    // todo - add some other options, filter by capture, check, checkmate, promotion, etc
    public static void Sort(List<Move> moves)
    {
        moves.Sort(
            (a,b)=>
            {
                if (a.c1 < b.c1) return -1;
                if (a.c1 > b.c1) return +1;
                if (a.r1 < b.r1) return -1;
                if (a.r1 > b.r1) return +1;
                if (a.c2 < b.c2) return -1;
                if (a.c2 > b.c2) return +1;
                if (a.r2 < b.r2) return -1;
                if (a.r2 > b.r2) return +1;
                return 0;

            }

        );
    }

    /*

depth	nodes	totalnodes
1	20	20
2	400	420
3	8902	9322
4	197281	206603
5	4865609	5072212
6	119060324	124132536
7	3195901860	3320034396


    stockfish: go perft 5
        a2a3: 181046
        b2b3: 215255
        c2c3: 222861
        d2d3: 328511
        e2e3: 402988
        f2f3: 178889
        g2g3: 217210
        h2h3: 181044
        a2a4: 217832
        b2b4: 216145
        c2c4: 240082
        d2d4: 361790
        e2e4: 405385
        f2f4: 198473
        g2g4: 214048
        h2h4: 218829
        b1a3: 198572
        b1c3: 234656
        g1f3: 233491
        g1h3: 198502
    Nodes searched: 4865609    
    */

    // Generate a list of legal moves
    public static List<Move> GenMoves(State state, bool checkTests = true)
    {
        // debugging 
        SetInvariant(state);

        // en passant target if it exists
        int epr = -1, epc = -1;
        if (state.enPassantSquare != -1)
            Square.Unpack(state.enPassantSquare, out epr, out epc);

        var moves = new List<Move>();
        for (var row = 0; row < 8; ++row)
            for (var col = 0; col < 8; ++col)
            {
                var (piece, color) = state[col, row];
                if (piece == State.Blank || state.MoveColor != color)
                    continue;

                var targetColor = State.OtherColor(color);

                // gen moves
                switch (piece)
                {
                    case State.Pawn:
                        var pawnDir = color == State.White ? 1 : -1;     // row direction to move
                        var depth = color == State.White ? row : 7 - row; // row from pawn viewpoint, 1-6, then promote. 

                        // check single forward move
                        var single = AddMoveIfOk(state, moves, row, col, pawnDir, 0, targetColor, false);

                        // check promotions - todo - maybe faster to do this after later filters?
                        if (single == AppendResult.NoCapture && depth == 6)
                            AddPromotions(moves);

                        // check double move, only allowed if single was allowed
                        // en passant square handled in State.DoMove
                        if (single == AppendResult.NoCapture && depth == 1)
                            AddMoveIfOk(state, moves, row, col, 2 * pawnDir, 0, targetColor, false);

                        // captures (EXcluding enPassant, including promotions)
                        for (var d = -1; d <= 1; d += 2)
                        {
                            var result = AddMoveIfOk(state, moves, row, col, pawnDir, d, targetColor, true, true);
                            if (result == AppendResult.Capture && depth == 6)
                                AddPromotions(moves); // todo - maybe better to do after move filters?
                        }
                        // en passant captures
                        if (state.enPassantSquare != -1 && epr == row + pawnDir && Math.Abs(epc - col) == 1)
                            moves.Add(new Move(row, col, row + pawnDir, epc, state[epc, row].piece) { enPassantCapture = true });
                        break;

                    case State.Knight:
                        ProcessDirections(state, knights, moves, row, col, targetColor, 1);
                        break;

                    case State.Bishop:
                        ProcessDirections(state, diag, moves, row, col, targetColor);
                        break;

                    case State.Rook:
                        ProcessDirections(state, axis, moves, row, col, targetColor);
                        break;

                    case State.Queen:
                        ProcessDirections(state, diag, moves, row, col, targetColor);
                        ProcessDirections(state, axis, moves, row, col, targetColor);
                        break;

                    case State.King:
                        ProcessDirections(state, diag, moves, row, col, targetColor, 1);
                        ProcessDirections(state, axis, moves, row, col, targetColor, 1);

                        // check castling
                        if (state.MoveColor == State.White)
                        {
                            if (state.WhiteCastleK)
                                AddCastleIfOk(state, moves, 0, 1);
                            if (state.WhiteCastleQ)
                                AddCastleIfOk(state, moves, 0, -1);
                        }
                        else
                        {
                            if (state.BlackCastleK)
                                AddCastleIfOk(state, moves, 7, 1);
                            if (state.BlackCastleQ)
                                AddCastleIfOk(state, moves, 7, -1);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

        //CheckInvariant(state);

        // expensive, but apply each move, check if king ok, filter....
        // filter out moves that leave king in check
        // also handles castling through check removal
        moves = moves.Where(move => !InCheck(state, move)).ToList();

        //CheckInvariant(state);

        if (checkTests) // stops possible deep recursion
        {
            // see which moves check opponent and mark them
            if (state.FindPiece(State.King, state.EnemyColor, out var row1, out var col1))
            {
                var attackerColor = state.MoveColor;
                foreach (var move in moves)
                {
                    state.DoMove(move);
                    move.check = IsAttacked(state, row1, col1, attackerColor);
                    state.UndoMove();
                    if (move.check)
                    {
                        // checkmate test
                        var replyMoves = GenMoves(state, false);
                        move.checkmate = !replyMoves.Any();
                    }
                }
            }
        }

        CheckInvariant(state, true);


        return moves;
    }

    // some functions to help catch errors
#if CHECK_INVARIANTS
    static Stack<string> fenStack = new Stack<string>();
    static void SetInvariant(State state, bool clear = false)
    {
        if (clear)
            fenStack.Clear();

        fenStack.Push(state.ToFEN());
    }
    static void CheckInvariant(State state, bool popAfter = false)
    {
        var fen2 = state.ToFEN();
        var ff = fenStack.Peek();
        if (popAfter)
            fenStack.Pop();
        if (ff != fen2)
            Console.WriteLine($"FEN error in gen moves \n{ff}!=\n{fen2}");
    }
#else    
    static void SetInvariant(State state, bool clear = false)
    { }
    static void CheckInvariant(State state, bool popAfter = false)
    { }
#endif            

    // Given state, move list, king row, and column direction +-1, add castling move if valid
    // assumes the direction is legal to castle
    static void AddCastleIfOk(State state, List<Move> moves, int row, int dc)
    {
        // rook column
        var colRook = dc == 1 ? 7 : 0;
        var color = state.MoveColor;
        // better be king here...
        var (pk, ck) = state[4, row];
        // .. and rook here...
        var (pr, cr) = state[colRook, row];
        if (pk != State.King || pr != State.Rook || ck != color || cr != color)
            return;
        // king needs two unoccupied squares on O-O, three on O-O-O
        if (
            state[4 + 1 * dc, row].piece != State.Blank ||
            state[4 + 2 * dc, row].piece != State.Blank || 
            (dc == -1 && state[4 + 3 * dc, row].piece != State.Blank)
        )
            return;
        // king needs two unattacked squares, not in check (todo - in check can be moved higher)
        if (
            IsAttacked(state, row, 4 + dc * 0, state.EnemyColor) ||
            IsAttacked(state, row, 4 + dc * 1, state.EnemyColor) ||
            IsAttacked(state, row, 4 + dc * 2, state.EnemyColor)
        )
            return;
        // add castling move
        moves.Add(new Move(row, 4, row, 4 + 2 * dc, State.Blank) { castle = true });
    }


    // add promotions to last pawn move
    static void AddPromotions(List<Move> moves)
    {
        var last = moves[moves.Count - 1];
        // set promotions, add more copies
        last.promoted = State.Knight;
        moves.Add(new Move(last, State.Bishop));
        moves.Add(new Move(last, State.Rook));
        moves.Add(new Move(last, State.Queen));
    }


    // handle list of dr,dc directions, with optional max depth
    // adds moves
    static void ProcessDirections(State state, int[] dirs, List<Move> moves, int r1, int c1, byte targetColor, int maxDepth = 8)
    {
        for (var i = 0; i < dirs.Length; i += 2)
        {
            var dr = dirs[i];
            var dc = dirs[i + 1];
            int depth = 0;
            var ddr = dr;
            var ddc = dc;
            while (depth < maxDepth && AddMoveIfOk(state, moves, r1, c1, ddr, ddc, targetColor) == AppendResult.NoCapture)
            {
                ddr += dr;
                ddc += dc;
                ++depth;
            }
        }
    }


    // apply move and see if king still in check
    static bool InCheck(State state, Move move)
    {
        var kingColor = state.MoveColor;
        var enemyColor = state.EnemyColor;

        CheckInvariant(state);

        state.DoMove(move);

        SetInvariant(state);

        var inCheck = false;
        // find king, see which enemies hit it
        if (state.FindPiece(State.King, kingColor, out var row, out var col))
        {
            CheckInvariant(state);
            inCheck = IsAttacked(state, row, col, enemyColor);
        }

        CheckInvariant(state, true);

        state.UndoMove();

        CheckInvariant(state);
        return inCheck;
    }

    // see if to move king is in check
    public static bool InCheck(State state)
    {
        var kingColor = state.MoveColor;
        var enemyColor = state.EnemyColor;

        var inCheck = false;
        // find king, see which enemies hit it
        if (state.FindPiece(State.King, kingColor, out var row, out var col))
        {
            inCheck = IsAttacked(state, row, col, enemyColor);
        }

        return inCheck;

    }

    // try to add move if meets criteria
    // return result
    static AppendResult AddMoveIfOk(State state, List<Move> moves, int r1, int c1, int dr, int dc, byte targetColor, bool canCapture = true, bool mustCapture = false)
    {
        var r2 = r1 + dr;
        var c2 = c1 + dc;

        if (!Square.InBounds(r2, c2))
            return AppendResult.Blocked; // out of bounds
        var (p, c) = state[c2, r2];
        if (p == State.Blank && mustCapture == false)
        {
            moves.Add(new Move(r1, c1, r2, c2, p));
            return AppendResult.NoCapture;
        }
        if (p != State.Blank && c == targetColor && canCapture)
        {
            moves.Add(new Move(r1, c1, r2, c2, p));
            return AppendResult.Capture;
        }
        return AppendResult.Blocked;
    }





    // cast ray in direction, see what hits
    // return (blank, black?) if off board
    static (byte piece, byte color, int dist) CastRay(State state, int row, int col, int dr, int dc)
    {
        //CheckInvariant(state);

        var dist = 0;
        while (true)
        {
            row += dr;
            col += dc;
            dist++;
            if (!Square.InBounds(row, col))
                break;
            var (p, c) = state[col, row];
            if (p != State.Blank)
                return (p, c, dist);
        }
        //CheckInvariant(state);

        return (State.Blank, State.Black, dist);
    }



    // see if square attacked by given color
    static bool IsAttacked(State state, int row, int col, int attackerColor)
    {
        // check each dir for relevant pieces
        return CheckDir(axis) || CheckDir(diag) || CheckDir(knights);

        bool CheckDir(int[] dirs)
        {
            //CheckInvariant(state);
            for (var i = 0; i < dirs.Length; i += 2)
            {
                var dr = dirs[i];
                var dc = dirs[i + 1];
                //CheckInvariant(state);
                var (p2, c2, dist) = CastRay(state, row, col, dr, dc);
                //CheckInvariant(state);


                if (p2 != State.Blank && c2 == attackerColor)
                {
                    var dd = Math.Abs(dr * dc); // 0 axis, 1 diag, 2 knight

                    // pawn
                    if (dd == 1 && p2 == State.Pawn && dist == 1)
                    {   // check direction to pawn
                        if (attackerColor == State.Black && dr == 1)
                            return true;
                        if (attackerColor == State.White && dr == -1)
                            return true;
                    }

                    // knight
                    if (dd == 2 && p2 == State.Knight && dist == 1)
                        return true;

                    // bishop
                    if (dd == 1 && p2 == State.Bishop)
                        return true;

                    // rook
                    if (dd == 0 && p2 == State.Rook)
                        return true;

                    // queen
                    if (dd != 2 && p2 == State.Queen)
                        return true;

                    // king
                    if (dd != 2 && p2 == State.King && dist == 1)
                        return true;
                }
            }
            return false;
        };

    }
    static int[] diag = new int[] { +1, +1, +1, -1, -1, -1, -1, +1 };
    static int[] axis = new int[] { +1, 0, -1, 0, 0, +1, 0, -1 };
    static int[] knights = new int[] { +2, +1, -2, +1, -2, -1, +2, -1, +1, +2, +1, -2, -1, -2, -1, +2 };


    enum AppendResult
    {
        NoCapture, // move onto blank square
        Capture, // move and capture
        Blocked // out of bounds or same color piece on target
    }


    // format move list
    public static string MoveListToText(List<Move> movelist) =>
        movelist.Aggregate("", (a, m) => a + (String.IsNullOrEmpty(a) ? "" : ", ") + m.ToString());

}