// todo - testing
// run random games, store moves, FEN, etc. run back wards and check each, 
// tests DoMove and UndoMove and FEN and ParseFEN
// see test positions https://www.chessprogramming.org/Test-Positions
// decent fen editor https://www.chess.com/analysis

/* TODO
This to test
- pawn promotion
- pawn capture with promotion
- en passant allows discovered check
- capture rook removes castling
- putting back restores castling
- en passant
- draw by repetition, stalemate, 3-fold
- win and loss
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Testing
{

    // test random games
    public static bool Test1(State state = null)
    {
        if (state == null)
            state = new State();

        var msg = TestRandomGames(state);
        Chess.Draw(state);
        var success = true;
        if (!String.IsNullOrEmpty(msg))
        {
            System.Console.WriteLine("ERROR: " + msg);
            success = false;
        }
        System.Console.WriteLine($"Testing complete {success}");
        return success;
    }

    static string TestRandomGames(State state)
    {
        // run random game, check FEN
        var r = new Random(1234); // make repeatable
        for (int game = 0; game < 100; ++game)
        {
            state.Reset();
            for (int moveCount = 1; moveCount < 300; ++moveCount)
            {
                var moves1 = MoveGen.GenMoves(state);
                if (!moves1.Any())
                    break;

                // check can do/undo all
                var fs = state.ToFEN();
                foreach (var m in moves1)
                {
                    state.DoMove(m);
                    state.UndoMove();
                    var fe = state.ToFEN();
                    if (fe != fs)
                        return $"FEN mismatch trying move {m} \n{fs} != \n{fe}";
                }

                var move = moves1[r.Next(moves1.Count)];

                System.Console.WriteLine($"{(moveCount + 1) / 2}: {move:5}");

                // move do/undo checks
                var f1 = state.ToFEN();
                state.DoMove(move);
                var f2 = state.ToFEN();
                System.Console.WriteLine(f2);
                state.UndoMove();
                var f3 = state.ToFEN();
                state.DoMove(move);
                var f4 = state.ToFEN();

                if (f1 != f3)
                    return $"FEN mismatch move {moveCount} : {move} \n{f1} != \n{f3}";
                if (f2 != f4)
                    return $"FEN mismatch move {moveCount} : {move} \n{f2} != \n{f4}";

#if true
                // test FEN parsing
                State s2;
                if (!State.TryParseFEN(f1, out s2))
                    return $"FEN parse failed {f1}";
                var f5 = s2.ToFEN();
                if (f1 != f5)
                    return $"FEN parse mismatch \n{f1} != \n{f5}";
                var moves2 = MoveGen.GenMoves(s2);
                var mt1 = MoveGen.MoveListToText(moves1);
                var mt2 = MoveGen.MoveListToText(moves2);

                if (mt1 != mt2)
                    return $"Move compare failed \n{mt1} != \n{mt2}\n{f1}==\n{f5}";
#endif              
                if (move.checkmate)
                    Chess.Draw(state);
            }
        }
        return "";
    }

    // test perf counts from various positions
    public static bool PerfTests()
    {
#if false        
        // for chasing down bugs. Compare to stockfish command line is useful
        State state2;
        State.TryParseFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", out state2);
        State.TryParseFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/5Q1p/PPPBBPPP/RN2K2R b KQkq - 0 1", out state2);
        State.TryParseFEN("r3k2r/p1ppqpb1/1n2pnp1/1b1PN3/1p2P3/5Q1p/PPPBBPPP/RN2K2R w KQkq - 0 1", out state2);        
        //var mm = MoveGen.GenMoves(state2);
        
        MoveGen.Perft(state2,2,true);
        return true;
#endif

        var regex = new Regex(@"^D(\d+) (\d+)");
        var (errors, successes) = (0, 0);
        var filename = "data/perft.txt";
        foreach (var line1 in File.ReadLines(filename))
        {

            var line = line1;
            if (string.IsNullOrEmpty(line))
                continue;
            var index = line.IndexOf('#');
            if (index != -1)
                line = line.Substring(0, index);
            if (string.IsNullOrEmpty(line))
                continue;
            var words = line.Split(';', Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1 && State.TryParseFEN(words[0], out var state))
            {
                for (var i = 1; i < words.Length; ++i)
                {
                    var m = regex.Match(words[i].Trim());
                    if (
                        m.Success &&
                        Int32.TryParse(m.Groups[1].Value, out var depth) &&
                        UInt64.TryParse(m.Groups[2].Value, out var count)
                        )
                    {

                        if (depth > 5) break; // todo - allow


                        var pinfo = PerftCount(state, depth);
                        var tested = pinfo.count;
                        if (tested != count)
                        {
                            Console.WriteLine($"ERROR: Me {tested} != Truth {count} depth {depth} FEN {words[0]}");
                            ++errors;
                        }
                        else
                        {
                            Console.WriteLine($"OK: {tested} == {count} depth {depth} FEN {words[0]}");
                            ++successes;
                        }
                        State.TryParseFEN(words[0], out state);
                    }
                }
            }
        }
        Console.WriteLine($"Perft testing {successes} successes, {errors} errors");
        return errors == 0;
    }

    // ERROR: 258 != 264 depth 2 FEN r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1



    // testing FEN
    public static string[] testFEN = {

    "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", // castling
    "4k3/R6R/8/8/8/8/8/4K3 w - - 0 1", // white about to mate
    "4k3/R6R/8/8/8/2n5/8/K1bn4 w - - 0 1", // black (then white) about to mate
    "4k3/8/8/r2pP3/7K/8/8/8 w - d6 0 1", // en passant
    "4k3/8/8/r2pP2K/8/8/8/8 w - d6 0 1", // en passant allowing check
    "7k/3q4/8/8/3R4/8/8/7K w - - 0 1", // good for computer movegen testing
    "8/4k1r1/8/2R2b2/8/6N1/8/7K w - - 0 1", // simple tactics, engine testing
    "6k1/8/8/5p2/6p1/6KP/8/8 w - - 0 1", // pawn tactics
    "8/8/8/5p2/6p1/7P/8/8 w - - 0 1", // 3 pawns
    "8/P7/8/8/8/6p1/5b2/7N w - - 0 1", // tactics
    "8/8/8/8/8/6p1/5p2/7N w - - 0 1", // tactics
    "r7/6p1/6pk/4Q1N1/6pK/5N2/8/1b6 w - - 0 1", // mate in 3
    "r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w KQkq - 1 0", // mate in 2, Nf6 first move
    "7k/8/5r2/8/8/2p2R2/8/7K w - - 0 1", // good testing of search depth stability
     };

    // 400k+ chess puzzles https://www.yacpdb.org/#static/home

    // some test positions - todo - finish
    public static void Test2(ref State state)
    {
        foreach (var fen in testFEN)
        {
            if (State.TryParseFEN(fen, out state))
            {
                var m = MoveGen.GenMoves(state);
                Chess.Draw(state);
                System.Console.WriteLine(MoveGen.MoveListToText(m));
                return;
            }
            else
                System.Console.WriteLine($"Error with FEN {fen}");
        }
    }

    // load puzzle from Reinfeld's book 1001 winning combinations
    public static void Test3(out State state, int puzzle)
    {
        state = null;
        var fn = "data/1001 Winning Chess Sacrifices & Combinations.pgn";
        var lines = File.ReadAllLines(fn);
        foreach (var ln in lines)
        {
            if (ln.StartsWith("[FEN \""))
            {
                puzzle--;
                if (puzzle == 0)
                {
                    var fen = ln.Substring(6, ln.Length - 6 - 2);
                    State.TryParseFEN(fen, out var s);
                    state = s;
                }
            }
        }
    }

    public class PerftInfo
    {
        public ulong count;
        public List<(Move, ulong)> divide = new List<(Move, ulong)>();
        public int depth;
    }

    public static void Perft(State state, int depth)
    {
        DumpPerftInfo(PerftCount(state,depth));
    }
    public static void DumpPerftInfo(PerftInfo info)
    {
        Console.WriteLine($"perft {info.depth}");
        foreach (var (m, c) in info.divide)
            Console.WriteLine($"{Square.Name(m.r1, m.c1)}{Square.Name(m.r1, m.c1)}: {c}");
        Console.WriteLine($"Nodes searched: {info.count}");
    }

    // testing of move generator
    // from given position, computes nodes at depth d
    // return count of nodes
    // divide outputs nodes under each top level move
    public static PerftInfo PerftCount(State state, int depth)
    {
        var info = new PerftInfo(){depth = depth};
        info.count = PerftInner(state,depth,true);
        return info;

        ulong PerftInner(State state1, int depth, bool divide)
        {
            if (depth == 0)
                return 1U;
            ulong nodes = 0;

            var moves = MoveGen.GenMoves(state);
            if (divide) MoveGen.Sort(moves);
            if (depth == 1)
                return (ulong)moves.Count;
            foreach (var move in moves)
            {
                state.DoMove(move);
                var count = PerftInner(state, depth - 1, false);
                nodes += count;
                state.UndoMove();

                if (divide)
                    info.divide.Add((move,count));
            }
            if (divide)
                Console.WriteLine($"Nodes searched: {nodes}");

            return nodes;
        }
    }



}