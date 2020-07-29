using System;
using System.Collections.Generic;
using System.Linq;


/*
TODO:
 1. *FEN output, input
 2. *test positions FEN
 3. *Pawn captures
 4. *Check
 5. *Checkmate
 6. *Castling
 7. *En Passant
 8. *Stalemate (implied by draw)
 9. *win loss draw
10. *Draw by 50 move
11. 75 move rule? (arbiter enforced, new Jul 2014)
12. Draw by repetition (same castling, ep, same everything state..., 3rd time, same to move)
13. Draw by pieces? (KvK, KvK+B, KvK+N, ?)
14. *help on commands
15. pretty print moves
16. *load fen from screen
17. *set computer side(s)
18. *show move list
19. save/load move PGN
20. * set computer move timer
21. move gen aware of draws, 50 move, etc..
22. *have computer make one move
23. bug - inf loops on cannot find move from enemy.. Pick rand and print error?

*/

// simple chess program
class Chess
{
    // run interactive game
    public void Interactive()
    {
        State state = new State();

        var quit = false;
        // moves executed in game
        while (!quit)
        {
            state.Reset();
            machineWhite = machineBlack = false;
            var inGame = true;
            while (inGame && !quit)
            {
                Draw(state, debugContinuousScroll, debugShowFen);
                var moves = MoveGen.GenMoves(state);
                if (debugShowMoves)
                    Console.WriteLine(MoveGen.MoveListToText(moves));

                if (state.halfMoveClock >= 50) // 50 move rule
                    moves.Clear();
                var gameOver = !moves.Any();
                if (gameOver)
                {
                    // check result
                    var result = CheckEnd(state);
                    Console.WriteLine($"Result: {result}");
                }

                // get move to do
                Move move = null;

                int score;
                if (!gameOver && state.MoveColor == State.White && machineWhite)
                    (move, score) = computerMover.GenMove(state, -1, maxMs);
                else if (!gameOver && state.MoveColor == State.Black && machineBlack)
                    (move, score) = computerMover.GenMove(state, -1, maxMs);
                else
                    move = HumanInput(ref state, moves, ref quit, ref inGame);

                if (move != null)
                    state.DoMove(move);
            }
        }
    }

    // can also fix seed for testing
    Random rand = new Random(); 
    
    // computer best move generator
    ComputerMove computerMover = new ComputerMove();
    // side(s) computer plays
    bool machineWhite = false;
    bool machineBlack = false;


    // show FEN with each position
    bool debugShowFen = false;
    
    // clear screen each move
    bool debugContinuousScroll = false;
    // show possible moves
    bool debugShowMoves = false;

    // max time spent for computer move in ms
    int maxMs = -1; // not set

    // get input from human
    Move HumanInput(ref State state, List<Move> moves, ref bool done, ref bool inGame)
    {
        Move move = null;
        var getMove = true;
        while (getMove)
        {
            getMove = false; // assume valid move obtained

            Console.Write("Enter move (? for help): ");
            var rawText = Console.ReadLine();
            var movetext = rawText.ToLower();

            if (movetext == "") // handle empty case first
            {   
                // random move - todo - print move?
                if (moves.Any())
                    move = moves[rand.Next(moves.Count)];
            }
            else if (movetext == "?")
                ShowHelp();
            else if (rawText.Length > 1 && rawText[0] == 'D')
            {
                if (rawText[1] == 'f')
                    debugShowFen = !debugShowFen;
                if (rawText[1] == 'm')
                    debugShowMoves = !debugShowMoves;
                if (rawText[1] == 's')
                    debugContinuousScroll = !debugContinuousScroll;
            }
            else if (movetext[0] == 't' && Int32.TryParse(movetext.Substring(1), out var newTime))
            {
                maxMs = newTime;
            }
            else if (movetext == "q")
                done = true;
            else if (movetext == "n")
                inGame = false;
            else if (State.TryParseFEN(rawText, out var s))
                state = s;
            else if (movetext.StartsWith("test") && movetext.Length > 4)
            {
                if (movetext[4] == '1')
                    Testing.Test1(state);
                else if (movetext[4] == '2')
                    Testing.Test2(ref state);
                else
                    Testing.Test3(out state, Int32.Parse(movetext.Substring(4)));
                Console.ReadLine();
            }
            else if ((rawText.StartsWith("s")||rawText.StartsWith("d")) && movetext.Length > 1 && Int32.TryParse(movetext.Substring(1),out var depth))
            { // search space
                // score moves
                var makeTree = rawText.StartsWith("d");
                var (bestMove, score) = computerMover.GenMove(state, depth, maxMs,makeTree);
                //foreach (var (m,s) in computerMover.moveScores)
                //    Console.Write($"{m} {s}, ");
                //Console.WriteLine();
                computerMover.DumpTree();
                getMove = false;
                Console.WriteLine($"Best move: {bestMove} with score {score}");
            }
            else if (movetext[0] == 'm')
            {
                int score;
                // b,w,1,
                if (movetext == "m")
                    machineWhite = machineBlack = false;
                else if (movetext == "mw")
                    machineWhite = true;
                else if (movetext == "mb")
                    machineBlack = true;
                else if (movetext == "m1")
                    (move,score) = computerMover.GenMove(state,-1,maxMs);
            }
            else if (movetext == "x")
            {
                state.UndoMove();
                if (state.MoveColor == State.White && machineWhite)
                    state.UndoMove();
                if (state.MoveColor == State.Black && machineBlack)
                    state.UndoMove();
            }
            else if (movetext.StartsWith(Editor.Prefix))
                Editor.Edit(state, movetext);
            else if (ParseMove(movetext, moves, out move))
            { // move obtained

            }
            else
                getMove = true; // try again
        }
        return move;
    }

    void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Help ------");
        Console.WriteLine($"Enter a move as 4 character e2e4. Castling is move king 2 squares");
        Console.WriteLine($"x     - back up a move");
        Console.WriteLine($"q     - quit");
        Console.WriteLine($"n     - new game");
        Console.WriteLine($"tN    - set max ms for computer move to N ms");
        Console.WriteLine($"sN    - score moves, N levels deep, N=1..9.. Enable continuous scroll first");
        Console.WriteLine($"dN    - score moves, show tree, N levels deep, N=1..9.. Enable continuous scroll first");
        Console.WriteLine($"?     - this help");
        Console.WriteLine($"Df    - toggle showing FEN each board");
        Console.WriteLine($"Dm    - toggle showing moves each board");
        Console.WriteLine($"Ds    - toggle continuous scroll");
        Console.WriteLine($"testn - run test #n");
        Console.WriteLine($"FEN   - any FEN position");
        Console.WriteLine($"      - (blank), play random move");
        Console.WriteLine($"mX    - X=w to have computer play white, b for black, 1 for one move, blank for neither");
        Editor.ShowHelp();
        Console.WriteLine("Enter to continue");
        Console.ReadLine();
    }


    public enum EndState
    {
        Draw,
        WhiteWins,
        BlackWins
    }
    // call when no moves left
    public static EndState CheckEnd(State state)
    {
        // if to move, no moves, and in check....
        if (MoveGen.InCheck(state))
        {
            if (state.MoveColor == State.White)
                return EndState.BlackWins;
            if (state.MoveColor == State.Black)
                return EndState.WhiteWins;
        }
        return EndState.Draw;
    }


    bool ParseMove(string movetext, List<Move> moves, out Move move)
    {
        if (!String.IsNullOrEmpty(movetext) &&
            movetext.Length == 4 &&
            Square.TryParse(movetext, out var r1, out var c1) &&
            Square.TryParse(movetext.Substring(2), out var r2, out var c2)
        )
        {
            move = moves.FirstOrDefault(m => m.r1 == r1 && m.r2 == r2 && m.c1 == c1 && m.c2 == c2);
            return move != null;
        }
        move = null;
        return false;
    }

    enum Result
    {
        None,
        Check,
        Draw
    }


    public static void Draw(State state, bool scroll = true, bool showFen = true)
    {
        string[] symb = new[]{
                " ","♙","♘","♗","♖","♕","♔",
                " ","♟︎","♞","♝","♜","♛","♚"
            };

        int movePos = 0; // start move output here
        int maxMove = state.gameMoves.Count;

        if (!scroll)
            Console.Clear();
        // unicode: white KQRBNP , black KQRBNP = U+2654 to U+265F
        Console.WriteLine();
        for (var row = 7; row >= 0; --row)
        {
            Console.Write($"{row + 1} ");
            var b = Console.BackgroundColor;
            var f = Console.ForegroundColor;
            for (var col = 0; col < 8; ++col)
            {
                var (p, c) = state[col, row];

                Console.ForegroundColor = ConsoleColor.Black;

                var blackSquare = ((row + col) & 1) != 0;
                if (blackSquare)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                }
                else
                {
                    //Console.ForegroundColor=ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }

                var index = p; // 0 index into string
                if (c == State.Black)
                    index += 7;
                string s = symb[index] + " ";
                //System.Console.WriteLine($"{col},{row}->{ind} = {p} = {ind}={s}");

                Console.Write(s);
            }
            Console.BackgroundColor = b;
            Console.ForegroundColor = f;

            // draw moves
            var whiteMove = movePos < maxMove ? state.gameMoves[movePos++] : null;
            var blackMove = movePos < maxMove ? state.gameMoves[movePos++] : null;
            if (whiteMove != null || blackMove != null)
                Console.Write($" {((movePos+1)/2),4}. {whiteMove,-9} {blackMove,-9}");
            //else
            //  Console.Write($" {(movePos/2),4}. {whiteMove,-8} {blackMove,-8}");

            Console.WriteLine();
        }
        Console.WriteLine("  a b c d e f g h ");
        if (showFen)
            Console.WriteLine($"FEN: [ {state.ToFEN()} ]");
    }
}



