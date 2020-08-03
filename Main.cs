// Chess program
// Chris Lomont 2020
// run the chess program
using System;

class Program
{
    static void Main(string[] args)
    {
        // install DejaVu Sans Mono as console font for windows
        // or Arial Unicode for MS
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        //System.Console.WriteLine(Console.OutputEncoding.ToString());
        //System.Text.Encoding 

        var chess = new Chess();
        chess.Interactive();
    }
}
