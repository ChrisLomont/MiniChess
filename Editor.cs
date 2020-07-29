using System;

class Editor
{
    public const string Prefix = "-";
    // return true if legal editing command and was executed

    public static byte AddColor = State.White;
    public static bool Edit(State state, string command)
    {
        /*
        commands:
        c - clear board
        {-pnbrqk}[a-h][1-8] - adds piece of current color (removes if '-')
        t - toggle color
        x - reset board
        o{0-15} set castling bits
        TODO - esq - set en passant sq
        TODO - h n - set halfmove #
        TODO - m n - set move number
        */
        command = command.Substring(Prefix.Length);
        if (String.IsNullOrEmpty(command))
            return false;
        if ("-pnbrqk".Contains(command[0]))
        {
            if (command.Length < 3)
                return false;
            // add/remove piece
            var col = command[1]-'a';
            var row = command[2]-'1';
            if (!Square.InBounds(row,col))
                return false;
            var c = command[0];
            if (c == '-') state[col,row] = Square.Empty;
            if (c == 'p') state[col,row] = (State.Pawn,AddColor);
            if (c == 'n') state[col,row] = (State.Knight,AddColor);
            if (c == 'b') state[col,row] = (State.Bishop,AddColor);
            if (c == 'r') state[col,row] = (State.Rook,AddColor);
            if (c == 'q') state[col,row] = (State.Queen,AddColor);
            if (c == 'k') state[col,row] = (State.King,AddColor);
        }
        else
        {
            switch (command[0])
            {
                case 'c' : // clear
                    state.ClearPieces();
                    break;
                case 't' : // toggle add color
                    AddColor = State.OtherColor(AddColor);
                    break;
                case 'x' : // reset
                    state.Reset();
                    break;
                case 'o' : // castling
                    if (Int32.TryParse(command.Substring(1),out var flags))
                        state.CastlingFlags = flags;
                    break;
            }
        }
        return true;
    }
        public static void ShowHelp()
        {
            Console.WriteLine($"Editor--------------");
            Console.WriteLine($"  Prefix commands with {Prefix}");
            Console.WriteLine($"  c - clear board");
            Console.WriteLine($"  x - reset board");
            Console.WriteLine($"  oN - set castling bits, N in 0-15");
            Console.WriteLine($"  piece and square, white uppercase PNBRQK, black lowercase, e.g., pe4");
            Console.WriteLine($"  -sq clears square");            
        }


}