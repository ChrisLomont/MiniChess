
// simple alpha-beta tree search, negamax framework https://www.chessprogramming.org/Alpha-Beta
// good source Knuth alpha beta paper 1975 https://pdfs.semanticscholar.org/dce2/6118156e5bc287bca2465a62e75af39c7e85.pdf

// todo - implement move ordering, killer hueristic
// todo - better scoring function
// todo - must pick random move from all equal scores to make interesting
// todo - clean and unify
// todo - still some bugs in here somewhere.... :|

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

class ComputerMove
{
    static Random rand = new Random();
    // get a machine move

    
    // get best move and score
    // fixed depth forces certain depth only search
    public (Move, int score) GenMove(State state, int fixedDepth = -1, int maxMs = -1, bool makeTree = false)
    {
        //var moves = MoveGen.GenMoves(state);
        //if (moves.Any())
        //    return moves[rand.Next(moves.Count)];
        //return null;

        // stats
        nodesInspected = 0;
        pruneCount = 0;

        // cause scoring to be relative to root player
        var playerSign = state.MoveColor == State.White ? 1 : -1;

        maxDepth = fixedDepth == -1 ? 2 : fixedDepth;

        var alpha = -50000;
        var beta  = -alpha;

        int  bestScore = Int32.MinValue;
        Move bestMove = null;

        moveScores.Clear();

        this.maxMs = maxMs==-1?2000:maxMs;

        root = null;

        timer.Restart();

        do {
            int score1=0;
            Move res = null;
#if true
            if (makeTree)
            {
                root = new Node();
                (res,score1) = AlphaBetaSearchTree(root, state, 0, alpha, beta, playerSign);
            }
            else
                (res,score1) = AlphaBetaSearch(state, 0, alpha, beta, playerSign);
#else                       
            root = new Node();
            var (res, score1) = Negamax(root, state, 0, playerSign);
#endif            
            if (res != null && score1 > bestScore)
            {
                bestMove = res;
                bestScore = score1;
            }
            ++maxDepth;
        } while (!OutOfTime() && fixedDepth == -1);
        timer.Stop();

        if (root != null)
        {
            root.score = bestScore;
            root.move = bestMove;
        }

        // stats
        Console.WriteLine($"Nodes inspected {nodesInspected}, depth {maxDepth - 1}, score {bestScore}, {pruneCount} pruned in {timer.ElapsedMilliseconds} ms");
        return (bestMove,playerSign*bestScore);
    }

    Stopwatch timer = new Stopwatch();
    int maxMs = 2000;
    bool OutOfTime()
    {
        var oo = timer.ElapsedMilliseconds > maxMs;
        return oo;
    }
    int pruneCount = 0;
    long nodesInspected = 0;
    int maxDepth=2;
    
#region Search tree
    public List<(Move, int)> moveScores = new List<(Move, int)>();

    public class Node
    {
        public Move move = null; // move to get here, or null
        public List<Node> Children = new List<Node>();
        public int score = Int32.MinValue; // best score
        public int alpha, beta;
        public string prunedMoves = "";
        public override string ToString() => $"{move} [{score}] {prunedMoves} [{alpha}-{beta}]";
    }
#endregion

    Node root=null;
    public void DumpTree()
    {
        PrintTree(root);
    }
    public static void PrintTree(Node tree, string indent = "", bool last = true)
    {
        if (tree == null)
            return;
        Console.WriteLine($"{indent}+- {tree}");
        indent += last ? "   " : "|  ";
        for (int i = 0; i < tree.Children.Count; i++)
            PrintTree(tree.Children[i], indent, i == tree.Children.Count - 1);
    }


    // alpha and beta, which represent the minimum score that the maximizing player is assured of and the maximum score that the minimizing player is assured of respectively    
    // https://homepage.iis.sinica.edu.tw/~tshsu/tcg/2012/slides/slide7.pdf
    // return score from view of current player
    // todo - make version that doesn't do any move tracking, then special one at highest level for move
    // builds tree
    (Move, int) AlphaBetaSearchTree(Node node, State state, int depth, int alpha, int beta, int playerSign)
    {
        // stats
        ++nodesInspected;

        // determine successor positions
        var moves = MoveGen.GenMoves(state);
        OrderMoves(moves); 

        // terminal node, or other end conditions
        // todo - or out of time, or other knowledge
        // todo - adjust score on depth if win/loss/draw to prefer shorter wins, longer losses
        if (depth >= maxDepth || !moves.Any())
            // scoring from viewpoint of root player, positive favors root player
            return (null, playerSign*Score(moves, state, maxDepth - depth)); // todo - quiesence search

        Move bestMove = null;
        var m = Int32.MinValue; // this is Fail-Soft move, to have Fail-Hard move, use m = alpha here
        for (var moveIndex = 0; moveIndex < moves.Count; ++moveIndex)
        {
            var move = moves[moveIndex];

            // construct move tree
            var child = new Node() { move = move };
            node.Children.Add(child);
            child.alpha = alpha;
            child.beta = beta;

            // recurse over child            
            state.DoMove(move);
            // reverse window for other viewpoint
            var (tempMove, tempScore) = AlphaBetaSearchTree(child, state, depth + 1, -beta, -m, -playerSign);
            tempScore = -tempScore; // correct for negamax
            state.UndoMove();

            // save score in tree
            child.score = tempScore;

            if (tempScore > m) 
            { // update score and move
                m = tempScore; 
                bestMove = move; 
            }
            // if (score > alpha) alpha = tempScore; // adjust window
            // cutoff
            if (m >= beta)
            {
                ++pruneCount;
                var sb = new StringBuilder();
                for (var j = moveIndex+1; j < moves.Count; ++j)
                    sb.Append($"{moves[j]}, ");
                child.prunedMoves = sb.ToString();
                

                break; // ensure move set
            }
            //if (OutOfTime())
            //    break;
        }
        return (bestMove, m);
    }

    // return score from view of current player
    // todo - make version that doesn't do any move tracking, then special one at highest level for move
    (Move, int) AlphaBetaSearch(State state, int depth, int alpha, int beta, int playerSign)
    {
        // for randomizing ties
        //var resevoirSampleCount = 1; // assume one to begin with
        
        // stats
        ++nodesInspected;

        // determine successor positions
        var moves = MoveGen.GenMoves(state); // todo- move order
        OrderMoves(moves); 

        // terminal node, or other end conditions
        // todo - or out of time, or other knowledge
        // todo - adjust score on depth if win/loss/draw to prefer shorter wins, longer losses
        if (depth >= maxDepth || !moves.Any())
            // scoring from viewpoint of root player, positive favors root player
            return (null, playerSign*Score(moves, state, maxDepth - depth)); // todo - quiesence search

        Move bestMove = null;
        var m = alpha;
        foreach (var move in moves)
        {
            // recurse over child            
            state.DoMove(move);
            // reverse window for other viewpoint
            var tempScore = AlphaBetaSearchFast(state, depth + 1, -beta, -m, -playerSign);
            tempScore = -tempScore; // correct for negamax
            state.UndoMove();

            if (tempScore >= m) 
            { // update score and move
                if (tempScore > m)
                {
                    m = tempScore; 
                    bestMove = move; 
                    //System.Console.WriteLine($"B: {move} {tempScore}");
                    //resevoirSampleCount = 1;
                }
                //else // tied, resevoir sample
                //{
                //    System.Console.WriteLine($"B: {move} {tempScore}");
                //    resevoirSampleCount++;
                //    // todo - if (rand.Next(resevoirSampleCount)==0)
                //        //bestMove = move;  // selected uniformly (if rnd is)
                // fails to sample here - gets wrong moves since moves out of bounds get scored in bounds...
                //}
            }; 
            if (m >= beta)
            {
                System.Console.WriteLine($"P {move}");
                ++pruneCount;
                break; // ensure move set
            }
            if (OutOfTime())
                break;
        }
        return (bestMove, m);
    }

    // return score from view of current player
    // todo - make version that doesn't do any move tracking, then special one at highest level for move
    int AlphaBetaSearchFast(State state, int depth, int alpha, int beta, int playerSign)
    {
        // stats
        ++nodesInspected;

        // determine successor positions
        var moves = MoveGen.GenMoves(state);
        OrderMoves(moves); 

        // terminal node, or other end conditions
        // todo - or out of time, or other knowledge
        // todo - adjust score on depth if win/loss/draw to prefer shorter wins, longer losses
        if (depth >= maxDepth || !moves.Any())
            // scoring from viewpoint of root player, positive favors root player
            return playerSign*Score(moves, state, maxDepth - depth); // todo - quiesence search

        var m = alpha;
        foreach (var move in moves)
        {
            // recurse over child            
            state.DoMove(move);
            // reverse window for other viewpoint
            var tempScore = AlphaBetaSearchFast(state, depth + 1, -beta, -m, -playerSign);
            tempScore = -tempScore; // correct for negamax
            state.UndoMove();

            if (tempScore > m) 
            { // update score and move
                m = tempScore; 
            }
            if (m >= beta)
            {
                ++pruneCount;
                break; // ensure move set
            }
            if (OutOfTime())
                break;
        }
        return m;
    }



    // todo - negamax - not working :(
    (Move, int) Negamax(Node node, State state, int depth, int playerSign)
    {
        var moves = MoveGen.GenMoves(state); // todo- move order
        OrderMoves(moves); 
        ++nodesInspected;

        if (depth >= maxDepth || !moves.Any()  /* todo: or state is terminal */)
            return (null, playerSign * Score(moves, state, 0)); // todo - quiesence search

        Move bestMove = null;
        var score = Int32.MinValue;
        foreach (var move in moves)
        {
            // if (bm == null) bm = move;
            var child = new Node() { move = move };
            node.Children.Add(child);
            state.DoMove(move);
            var (bm2, cur) = Negamax(child, state, depth + 1, -playerSign);
            cur = -cur;
            state.UndoMove();
            child.score = cur;

            if (cur > score) { score = cur; bestMove = move; }; // update score
            //if (OutOfTime())
            //    break;
        }
        return (bestMove, score);
    }

    static void Shuffle<T>(IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rand.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }    

    // order pieces for better pruning
    // randomize where possible
    static void OrderMoves(List<Move> moves)
    {
        // shuffle to make play less predictable
        Shuffle(moves);

        var c1 = new List<Move>();
        var c2 = new List<Move>();
        var c3 = new List<Move>();
        var c4 = new List<Move>();
        var c5 = new List<Move>();

        // split into 1) checkmates, 2) captures, 3) checks, 4) promotions, 5) rest
        foreach (var m in moves)
        {
            if (m.checkmate)
                c1.Add(m);
            else if (m.removed != State.Blank)
                c2.Add(m);
            else if (m.check)
                c3.Add(m);
            else if (m.promoted != State.Blank)
                c4.Add(m);
            else 
                c5.Add(m);
        }
        moves.Clear();
        moves.AddRange(c1);
        moves.AddRange(c2);
        moves.AddRange(c3);
        moves.AddRange(c4);
        moves.AddRange(c5);        
    }


    // score from viewpoint of white
    static int Score(List<Move> moves, State state, int depthBonus)
    {
        int score = 0;
        if (!moves.Any())
        {
            var endState = Chess.CheckEnd(state);
            if (endState == Chess.EndState.WhiteWins)
                score = 20000 + depthBonus;
            else if (endState == Chess.EndState.BlackWins)
                score = -20000 - depthBonus;
            else
                score = 0 + depthBonus;
        }
        else
        {
            // https://en.wikipedia.org/wiki/Chess_piece_relative_value
            for (var row = 0; row < 8; ++row)
                for (var col = 0; col < 8; ++col)
                {
                    var (p, c) = state[col, row];
                    if (p==State.Blank)
                        continue;

                    var temp = 0; // score from whites view

                    if (p == State.Pawn)
                        temp += 100;
                    if (p == State.Knight)
                        temp += 300;
                    if (p == State.Bishop)
                        temp += 350;
                    if (p == State.Rook)
                        temp += 500;
                    if (p == State.Queen)
                        temp += 900;
                    

                    if (temp != 0)
                    {
                        // add weight to occupying center
                        var dc = Math.Abs(3.5-col);
                        var dr = Math.Abs(3.5-row);
                        var centerScore = 20.0*(2*3.5*3.5-(dr*dr+dc*dc))/(2*3.5*3.5);
                        //System.Console.WriteLine($"cs: {centerScore}");
                        //centerScore = 0; // todo 

                        temp += (int)centerScore;
                    }


                    score += c==State.White ? temp : -temp;
               }
        }

        return score;

        //return (state.MoveColor == State.White) ? score : -score;
    }

    
}