// Square helper functions
using System;

static class Square {
    
    // represent blank square as tuple
    public static (byte,byte) Empty => (State.Blank, State.Black);
    public static bool InBounds(int row, int col) => 0<=col && col <= 7 && 0 <= row && row <= 7;

    public static string Name(int row, int col) => "abcdefgh"[col].ToString() + (row + 1).ToString();


    // read prefix of string
    public static bool TryParse(string text, out int row, out int col)
    {
        row=col=-1;
        if (String.IsNullOrEmpty(text) || text.Length < 2)
            return false;
        var rt = text[1];
        var ct = text[0];
        var c = Char.ToLower(ct)-'a';
        var r = rt-'1';
        if (!InBounds(r,c))
            return false;
        
        col=c;
        row=r;        
        return true;
    }
    public static byte Pack(int row, int col)
    {
        return (byte)(row*8+col);
    }
    public static void Unpack(int sq, out int row, out int col)
    {
        row = sq/8;
        col = sq&7;
    }

}